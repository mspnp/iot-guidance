// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Threading.Tasks;

namespace Fabrikam.IoT.Devices.SupplyChain.Actors
{   
    public interface IIoTDeployer
    {
        string GenerateLeafCertDistinguishedName(string uniqueDeviceId);

        Task InstallAsync(IIoTDevice device, string dpsGlobalEndpoint, string dpsScopeId);
    }
}