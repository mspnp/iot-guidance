// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Threading.Tasks;

namespace Fabrikam.DroneManagement.DroneHotPathFunction
{
    class NotificationService : INotificationService
    {
        public async Task SendNotificationAsync(HotTelemetry hotTelemetry)
        {
            //this is the stub implementation for notification service
            //implement notification service call below
            // for twilio sms message implementation 
            //https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-twilio
            // for notificatio hub implementation
            //https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-notification-hubs


            await Task.Delay(10);
          
        }
    }
}
