// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using Newtonsoft.Json;

namespace Fabrikam.DroneManagement.DroneHotPathFunction
{
    public class HotTelemetry 
    {
        [JsonProperty(PropertyName = "message")]
        public string message { get; set; }

        [JsonProperty(PropertyName = "partitionid")]
        public int partition { get; set; }

        [JsonProperty(PropertyName = "deliveryid")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "deviceid")]
        public string deviceid { get; set; }

        [JsonProperty(PropertyName = "avgtemperature")]
        public double AvgTemperature { get; set; }

        [JsonProperty(PropertyName = "lastoccurrenceutctime")]
        public DateTime EventUtcTime { get; set; }

        [JsonProperty(PropertyName = "lastenqueuedutctime")]
        public DateTime EventArrivalUtcTime { get; set; }

        [JsonProperty(PropertyName = "lastprocessedutctime")]
        public DateTime EventProcessedUtcTime { get; set; }

    }
}
