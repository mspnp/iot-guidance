// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json.Linq;
using Microsoft.SqlServer.Types;
using Newtonsoft.Json;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling.Data;

namespace WarmPathFunction
{
    public sealed class WarmPathFunction
    {
        private const string DroneSensorEventType = "drone-event-sensor;v1";
        private const string EnvSQLDBConnectionString = "SQLDBConnectionString";

        private static readonly string SQLDBConnectionString =
                        Environment
                        .GetEnvironmentVariable(
                            EnvSQLDBConnectionString,
                            EnvironmentVariableTarget.Process);

        static WarmPathFunction()
        {
            if (string.IsNullOrEmpty(SQLDBConnectionString))
            {
                throw new ArgumentException("Azure SQL Database connection string is not valid.", nameof(SQLDBConnectionString));
            }
        }

        private WarmPathFunction()
        {
        }

        [FunctionName("WarmPathFunction")]
        public static async Task RunAsync(
                            [EventHubTrigger(
                              "%EventHubName%",
                              Connection = "EventHubConnectionString",
                              ConsumerGroup = "%ConsumerGroup%")]
                            EventData[] messages,
                            Microsoft.Azure.WebJobs.ExecutionContext context,
                            TraceWriter log)
        {
            DateTimeOffset ticksUTCNow = DateTimeOffset.UtcNow;

            CustomTelemetry.TrackMetric(
                              context,
                              "IoTHubMessagesReceived",
                              messages.Length);

            // Track whether messages are arriving at the function late.
            DateTime? firstMsgEnqueuedTicksUtc = messages[0]?.EnqueuedTimeUtc;

            if (firstMsgEnqueuedTicksUtc.HasValue)
            {
                CustomTelemetry.TrackMetric(
                                  context,
                                  "IoTHubMessagesReceivedFreshnessMsec",
                                  (ticksUTCNow - firstMsgEnqueuedTicksUtc.Value)
                                  .TotalMilliseconds);
            }

            var length = (double)messages.Length;

            log.Info($"Starting load | Docs to bulk load {messages.Length} | Docs to bulk load per task {length}");

            // Bulk load events
            long sqlDbTotalMilliseconds = await BulkLoadEvents(messages, log);

            CustomTelemetry.TrackMetric(
                               context,
                               "IoTHubMessagesDropped",
                               messages.Length);
            CustomTelemetry.TrackMetric(
                               context,
                               "SqlDbDocumentsCreated",
                               messages.Length);
            var latency = messages.Length > 0 
                                ? sqlDbTotalMilliseconds / messages.Length 
                                : 0;
            CustomTelemetry.TrackMetric(
                               context,
                               "SqlDbLatencyMsec",
                               latency);
        }

        private static async Task<long> BulkLoadEvents(
                                    IEnumerable<EventData> docsToUpsert,
                                    TraceWriter log)
        {
            // Define retry logic for reliable database operations
            RetryStrategy retryStrategy = new FixedInterval(3, TimeSpan.FromSeconds(10));
            RetryPolicy retryPolicy = new RetryPolicy<SqlDatabaseTransientErrorDetectionStrategy>(retryStrategy);

            // Define data structure that will load events into database
            DataTable dt = new DataTable();
            dt.Columns.Add("deviceid",typeof(string));
            dt.Columns.Add("timestamp",typeof(DateTime));
            dt.Columns.Add("geo",typeof(string));
            dt.Columns.Add("json",typeof(string));

            var sqlDbLatency = new Stopwatch();
            // for each message read from IoTHub
            foreach (var message in docsToUpsert)
            {
                var text = Encoding.UTF8.GetString(message.GetBytes());
                // Create a new row
                DataRow dr = dt.NewRow();
                // Parse telemetry message
                dynamic telemetry = JObject.Parse(text);
                if (telemetry.sensorType == DroneSensorEventType)
                {
                    // Convert position
                    string position = telemetry.position;
                    var (latitude, longitude) = DroneTelemetryConverter.ConvertPosition(position);
                    // Conver to WKT format
                    string geo = string.Format($"POINT ({longitude} {latitude})");

                    dr["deviceid"]=telemetry.deviceId;
                    dr["timestamp"]=message.EnqueuedTimeUtc;
                    dr["geo"]=geo;
                    dr["json"]=text;

                    dt.Rows.Add(dr);                  
                }
            }    

            try
            {
                sqlDbLatency.Start();

                await retryPolicy.ExecuteAsync(async ()=>
                { 
                    using(SqlConnection cnn = new SqlConnection(SQLDBConnectionString))
                    {
                        cnn.Open();
                        SqlBulkCopy bc = new SqlBulkCopy(cnn);
                        bc.BatchSize=10000;
                        bc.DestinationTableName="events";
                        await bc.WriteToServerAsync(dt);
                    }
                });

                sqlDbLatency.Stop();
            }
            catch (SqlException sqlEx)
            {
                log.Error($"Error processing message with err number {sqlEx.Number}. Exception was {sqlEx.ToString()}");
            }
            catch(Exception e)
            {
                log.Error($"Error processing message. Exception was {e.ToString()}");
            }

            return (long)sqlDbLatency
                .Elapsed
                .TotalMilliseconds;
        }
    }
}
