// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Fabrikam.IoT.Devices.SupplyChain.Runtime
{
    public class Config : IConfig
    {
        public string IoTCompanyName { get; set; }
        public string IoTHardwareIntegratorName { get; set; }
        public string IoTDeployerName { get; set; }
        public string AzureDPSConnectionString { get; set; }
        public string AzureDPSGlobalEndpoint { get; set; }
        public string AzureDPSScopeId { get; set; }
        public string AzureDPSEnrollmentGroup {get;  set; }
    }
}