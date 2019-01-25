// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Threading.Tasks;
using Fabrikam.IoT.Devices.SupplyChain.Actors.Security.Models;
using Microsoft.Azure.Devices.Provisioning.Client;

namespace Fabrikam.IoT.Devices.SupplyChain.Actors
{
    public interface IIoTDevice
    {
        IDeviceHSM HardwareSecurityModel {get;}

        string DeviceID {get;}

        string AssignedIoTHub {get;}

        ProvisioningRegistrationStatusType RegistrationStatus {get;}

        Task ProvisionAsync(string dpsGlobalDeviceEndpoint, 
                               string dpsIdScope);

        Task AuthNAndSendTelemetryAsync();
    }
}