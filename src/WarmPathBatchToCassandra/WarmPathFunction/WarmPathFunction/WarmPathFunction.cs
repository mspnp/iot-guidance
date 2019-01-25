// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Text;
using System.Threading.Tasks;
using Dse;
using Dse.Auth;
using Dse.Geometry;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json.Linq;

namespace WarmPathFunction
{
    public static class WarmPathFunction
    {
        const string DroneSensorEventType = "drone-event-sensor;v1";

        static IDseSession session;
        static readonly string tableName;

        static WarmPathFunction()
        {
            var contactPoints = Environment.GetEnvironmentVariable("CassandraContactPoints", EnvironmentVariableTarget.Process);
            var username = Environment.GetEnvironmentVariable("CassandraUsername", EnvironmentVariableTarget.Process);
            var password = Environment.GetEnvironmentVariable("CassandraPassword", EnvironmentVariableTarget.Process);
            tableName = Environment.GetEnvironmentVariable("CassandraTableName", EnvironmentVariableTarget.Process);

            IDseCluster cluster = DseCluster.Builder()
                    .AddContactPoints(contactPoints.Split(','))
                    .WithAuthProvider(new DsePlainTextAuthProvider(username, password))
                    .Build();
            session = cluster.Connect();
        }

        [FunctionName("WarmPathFunction")]
        public static async Task RunAsync(
            [EventHubTrigger("%EventHubName%", Connection = "EventHubConnectionString", ConsumerGroup = "%ConsumerGroup%")]
            EventData[] messages,
            ExecutionContext context,
            TraceWriter log)
        {
            CustomTelemetry.TrackMetric(context, "IoTHubMessagesReceived", messages.Length);
            await Task.Delay(0);

            var ticksUTCNow = DateTimeOffset.UtcNow;
            var cutoffTime = DateTimeOffset.UtcNow.AddMinutes(-5);

            // Track whether messages are arriving at the function late.
            DateTime? firstMsgEnqueuedTicksUtc = messages[0]?.EnqueuedTimeUtc;
            if (firstMsgEnqueuedTicksUtc.HasValue)
            {
                CustomTelemetry.TrackMetric(
                                   context,
                                   "IoTHubMessagesReceivedFreshnessMsec",
                                   (ticksUTCNow - firstMsgEnqueuedTicksUtc.Value).TotalMilliseconds);
            }

            int count = 0;
            int droppedMessages = 0;

            var batchStatement = new BatchStatement();
            batchStatement.SetBatchType(BatchType.Unlogged);

            foreach (var message in messages)
            {
                // Drop stale messages,
                if (message.EnqueuedTimeUtc < cutoffTime)
                {
                    log.Info($"Dropping late message batch. Enqueued time = {message.EnqueuedTimeUtc}, Cutoff = {cutoffTime}");
                    droppedMessages++;
                    continue;
                }

                var text = Encoding.UTF8.GetString(message.GetBytes());
                log.Info($"Process message: {text}");

                try
                {
                    dynamic telemetry = JObject.Parse(text);
                    if (telemetry.sensorType == DroneSensorEventType)
                    {
                        string position = telemetry.position;
                        var (latitude, longitude) = DroneTelemetryConverter.ConvertPosition(position);

                        string deviceId = telemetry.deviceId;

                        var statementAdd = new SimpleStatement($"INSERT INTO {tableName} (device_id, location, event_time) VALUES (?, ?, ?) USING TTL 259200",
                                        deviceId, new Point(longitude, latitude), new DateTimeOffset(message.EnqueuedTimeUtc));
                        batchStatement.Add(statementAdd);

                        count++;
                    }
                }
                catch (Exception ex)
                {
                    log.Error("Error processing message", ex);
                }
            }

            try
            {
                await session.ExecuteAsync(batchStatement);
                log.Info("Successfully written batch to cassandra");

                CustomTelemetry.TrackMetric(
                               context,
                               "IoTHubMessagesDropped",
                               droppedMessages);
                CustomTelemetry.TrackMetric(
                                   context,
                                   "CassandraDocumentsCreated",
                                   count);
            }
            catch (Exception ex)
            {
                log.Error("Error processing batch of messages", ex);
            }
        }
    }
}
