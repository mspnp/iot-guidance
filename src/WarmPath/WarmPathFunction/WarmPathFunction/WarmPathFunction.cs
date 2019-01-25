// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Spatial;
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

        [FunctionName("WarmPathFunction")]
        public static async Task RunAsync(
            [EventHubTrigger("%EventHubName%", Connection = "EventHubConnectionString", ConsumerGroup = "%ConsumerGroup%")]
            EventData[] messages,
            [DocumentDB("%CosmosDBDataBase%", "%CosmosDBCollection%", ConnectionStringSetting = "CosmosDBConnectionString", CreateIfNotExists = false)]
            IAsyncCollector<dynamic> documents,
            ExecutionContext context,
            TraceWriter log)
        {
            CustomTelemetry.TrackMetric(context, "IoTHubMessagesReceived", messages.Length);

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

                        await documents.AddAsync(new
                        {
                            id = telemetry.deviceId,
                            deviceId = telemetry.deviceId,
                            Location = new Point(longitude, latitude),
                            Timestamp = message.EnqueuedTimeUtc
                        });
                        count++;
                    }
                }
                catch (Exception ex)
                {
                    log.Error("Error processing message", ex);
                }
            }

            CustomTelemetry.TrackMetric(
                               context,
                               "IoTHubMessagesDropped",
                               droppedMessages);
            CustomTelemetry.TrackMetric(
                               context, 
                               "CosmosDbDocumentsCreated", 
                               count);
        }
    }
}
