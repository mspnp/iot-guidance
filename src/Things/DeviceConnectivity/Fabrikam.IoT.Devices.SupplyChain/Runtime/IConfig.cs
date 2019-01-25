// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Fabrikam.IoT.Devices.SupplyChain.Runtime
{
    public interface IConfig
    {
        #region Supply Chain
        string IoTCompanyName {get; set;}
        string IoTHardwareIntegratorName {get; set;}
        string IoTDeployerName {get; set;}
        #endregion

        #region Azure IoT Device Provisioning Service
        string AzureDPSConnectionString {get; set;}
        string AzureDPSGlobalEndpoint {get; set;}
        string AzureDPSScopeId {get; set;}
        string AzureDPSEnrollmentGroup {get; set;}
        #endregion
    }
}