// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.​
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.​
// ------------------------------------------------------------

using System;
using System.Spatial;

namespace WarmPathUI.Models
{
    public class DeviceEvent
    {
        public string id { get; set; }
        public GeometryPoint Location { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
