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
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Spatial;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json.Linq;

namespace WarmPathFunction
{
    public sealed class WarmPathFunction
    {
        private const string DroneSensorEventType = "drone-event-sensor;v1";
        private const string EnvNameCosmosDBConnectionString = "CosmosDBConnectionString";
        private const string EnvNameCosmosDbDatabaseId = "CosmosDBDataBase";
        private const string EnvNameCosmosDbCollectionId = "CosmosDBCollection";
        // using 100 threads (10 messages/thread) in an ASP with 32 workers, 
        // the CPU usage is at about 50% (1x core).
        private const int MinThreadPoolSize = 100; 

        private static readonly string DatabaseId =
                        Environment
                        .GetEnvironmentVariable(
                            EnvNameCosmosDbDatabaseId,
                            EnvironmentVariableTarget.Process);
        private static readonly string CollectionId =
                        Environment
                        .GetEnvironmentVariable(
                            EnvNameCosmosDbCollectionId,
                            EnvironmentVariableTarget.Process);
        private static readonly string CosmosDBConnectionString =
                        Environment
                        .GetEnvironmentVariable(
                            EnvNameCosmosDBConnectionString,
                            EnvironmentVariableTarget.Process);
        private static readonly Uri CollectionLink;
        private static readonly DocumentClient Client;

        private long UpsertedDocuments,
                     DroppedMessages,
                     CosmosDbTotalMilliseconds;

        static WarmPathFunction()
        {
            if (string.IsNullOrEmpty(CosmosDBConnectionString))
            {
                throw new ArgumentException("Azure Cosmos DB connection string is not valid.", nameof(CosmosDBConnectionString));
            }

            if (string.IsNullOrEmpty(DatabaseId))
            {
                throw new ArgumentException("Azure Cosmos DB Database id is not valid.", nameof(DatabaseId));
            }

            if (string.IsNullOrEmpty(CollectionId))
            {
                throw new ArgumentException("Azure Cosmos DB Collection id is not valid.", nameof(CollectionId));
            }

            var cosmosdbConnStrBuilder = 
                  new DbConnectionStringBuilder
                  {
                     ConnectionString = CosmosDBConnectionString
                  };
            var endpointUri = 
                new Uri(cosmosdbConnStrBuilder["accountendpoint"].ToString());
            var authorizationKey = 
                cosmosdbConnStrBuilder["accountkey"].ToString();

            Client = new DocumentClient(
                            endpointUri,
                            authorizationKey,
                            new ConnectionPolicy
                            {
                                // Direct connectivity for best performance, 
                                // since client connects to a range of port 
                                // numbers in Cosmos db.
                                ConnectionMode = ConnectionMode.Direct,
                                // Better for performance.
                                ConnectionProtocol = Protocol.Tcp,
                                RequestTimeout = new TimeSpan(1, 0, 0),
                                // [Security] [DDoS] limit the resource 
                                // consumption from Azure Fuction to 
                                // Azure Cosmos db
                                RetryOptions = new RetryOptions
                                {
                                    // backoff: provided the client SDK detects 
                                    // the Azure Function sends many requests, 
                                    // it will stop sending to cosmos db for 
                                    // some period of time.
                                    MaxRetryAttemptsOnThrottledRequests = 10,
                                    // retryafter: it tries to recover for a 
                                    // peoriod of time due to a rate limiting 
                                    // server side. After the time has elapsed, 
                                    // it just give up and returns an error to 
                                    // the function.
                                    MaxRetryWaitTimeInSeconds = 60
                                }
                            });

            Client.OpenAsync().GetAwaiter().GetResult();

            CollectionLink = UriFactory.CreateDocumentCollectionUri(
                                          DatabaseId,
                                          CollectionId);
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
            ThreadPool.SetMinThreads(MinThreadPoolSize, MinThreadPoolSize);

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
            var numberOfDocumentsToUpsertPerTask = (int)Math.Ceiling(length / MinThreadPoolSize);
            var taskCount = (int)Math.Ceiling(length / numberOfDocumentsToUpsertPerTask);

            log.Info($"Starting upserts with {taskCount} tasks | Docs to upsert {messages.Length} | Docs to upsert per task {numberOfDocumentsToUpsertPerTask}");

            var (documentsUpserted, droppedMessages, cosmosDbTotalMilliseconds) =
                await new WarmPathFunction().ProcessMessagesFromEventHub(
                        taskCount,
                        numberOfDocumentsToUpsertPerTask,
                        messages,
                        log);

            CustomTelemetry.TrackMetric(
                               context,
                               "IoTHubMessagesDropped",
                               droppedMessages);
            CustomTelemetry.TrackMetric(
                               context,
                               "CosmosDbDocumentsCreated",
                               documentsUpserted);
            // some telemetry could be not stored in WarmPath, so it gets 
            // filtered and documents are not upserted.
            var latency = documentsUpserted > 0 
                                ? cosmosDbTotalMilliseconds / documentsUpserted 
                                : 0;
            CustomTelemetry.TrackMetric(
                               context,
                               "CosmosDbLatencyMsec",
                               latency);
        }

        private async Task<(long documentsUpserted,
                            long droppedMessages,
                            long cosmosDbTotalMilliseconds)>
                      ProcessMessagesFromEventHub(
                            int taskCount,
                            int numberOfDocumentsToUpsertPerTask,
                            EventData[] messages,
                            TraceWriter log)
        {
            DateTimeOffset cutoffTime = DateTimeOffset.UtcNow.AddMinutes(-5);

            var tasks = new List<Task>();

            for (var i = 0; i < taskCount; i++)
            {
                var docsToUpsert = messages
                                    .Skip(i * numberOfDocumentsToUpsertPerTask)
                                    .Take(numberOfDocumentsToUpsertPerTask);
                // client will attempt to create connections to the data 
                // nodes on Cosmos db's clusters on a range of port numbers
                tasks.Add(UpsertDocuments(i, docsToUpsert, cutoffTime, log));
            }

            await Task.WhenAll(tasks);

            return (this.UpsertedDocuments,
                    this.DroppedMessages,
                    this.CosmosDbTotalMilliseconds);
        }

        private async Task UpsertDocuments(
                                    int taskId,
                                    IEnumerable<EventData> docsToUpsert,
                                    DateTimeOffset cutoffTime,
                                    TraceWriter log)
        {
            var cosmosDbLatency = new Stopwatch();
            int count = 0;
            int droppedMessages = 0;
            foreach (var message in docsToUpsert)
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
                        var (latitude, longitude) = DroneTelemetryConverter
                                                      .ConvertPosition(position);

                        cosmosDbLatency.Start();
                        await Client.UpsertDocumentAsync(
                          CollectionLink,
                          new
                          {
                              id = telemetry.deviceId,
                              deviceId = telemetry.deviceId,
                              Location = new Point(longitude, latitude),
                              Timestamp = message.EnqueuedTimeUtc
                          });
                        cosmosDbLatency.Stop();

                        count++;
                    }
                }
                catch (Exception e)
                {
                    if (e is DocumentClientException documentClientEx)
                    {
                        log.Error($"Error processing message with status code {documentClientEx.StatusCode}. Exception was {documentClientEx.Message}");
                    }
                    else
                    {
                        log.Error($"Error processing message. Exception was {e.Message}");
                    }
                }
                finally
                {
                    Interlocked.Add(ref this.UpsertedDocuments, count);
                    Interlocked.Add(ref this.DroppedMessages, droppedMessages);
                    Interlocked.Add(ref this.CosmosDbTotalMilliseconds,
                                (long)cosmosDbLatency
                                .Elapsed
                                .TotalMilliseconds);
                }
            }
        }
    }
}
