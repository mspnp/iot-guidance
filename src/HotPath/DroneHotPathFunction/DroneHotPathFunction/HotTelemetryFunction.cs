// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Net;
using System;
using Microsoft.Extensions.Logging;

namespace Fabrikam.DroneManagement.DroneHotPathFunction
{
    public static class HotTelemetryFunction
    {

        public static INotificationService notificationService;
        private readonly static int batchSize;

        static HotTelemetryFunction()
        {
            notificationService = new NotificationService();
            batchSize = int.Parse(Environment.GetEnvironmentVariable("ASA_BATCH"));
        }

        [FunctionName("HotTelemetry")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, 
            "get", "post", Route = null)]HttpRequest request, ILogger logger)
        {
           // Get the request body
            string requestBody;
            using (StreamReader streamReader = new StreamReader(request.Body))
            { 
               requestBody = await streamReader.ReadToEndAsync();
            }

            // an array of telemetry data
            HotTelemetry[] hotTelemetries;
            try
            {
                hotTelemetries = JsonConvert
                        .DeserializeObject<HotTelemetry[]>(requestBody);
            }
            // if ASA sends bad json format
            // it logs and returns bad request
            catch(JsonSerializationException JsonException)
            {
                logger.LogError(JsonException.Message);
                return new BadRequestObjectResult(HttpStatusCode.BadRequest);
            }

            //call notification service 
            //per device telemetry
            // 
            logger.LogInformation("payload {payload}", requestBody);

            foreach (HotTelemetry hotTelemetry in hotTelemetries)
            {            
                logger.LogInformation("message {message} deliveryId {deliveryid}, deviceId {deviceid}, " +
                    "Event Time {eventtime}, arrivaltime {queuetime}, processedtime {processedtime}",
                    hotTelemetry.message,
                    hotTelemetry.Id, 
                    hotTelemetry.deviceid,
                    hotTelemetry.EventUtcTime, 
                    hotTelemetry.EventArrivalUtcTime,
                    hotTelemetry.EventProcessedUtcTime);

               
                await notificationService
                    .SendNotificationAsync(hotTelemetry)
                    .ConfigureAwait(false);

            

            }

            //this will tell ASA to return a smaller
            //batch. this scenario is possible if
            //many devices send 1 hot telemetry on 
            //same tumbling window.
            //scale ASA and/or scale function to avoid latency 

            logger.LogInformation("Batch size {batchsize}", hotTelemetries.Length);

            if (hotTelemetries.Length > batchSize)
            {
                logger.LogWarning("Max batch size allowed {maxvalue} exceeded {value} ",
                    batchSize, hotTelemetries.Length);
               
                return new BadRequestObjectResult(HttpStatusCode.RequestEntityTooLarge);
            }
            return new OkObjectResult(HttpStatusCode.OK);
                   
        }
    }
}
