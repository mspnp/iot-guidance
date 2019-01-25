// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.​
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.​
// ------------------------------------------------------------

using Microsoft.Azure.Documents.Spatial;
using System;

namespace WarmPathUI.Models
{
    public class DeviceEvent
    {
        public string id { get; set; }
        public Point Location { get; set; }
        public DateTime Timestamp { get; set; }
        public string _etag { get; set; }
    }
}
