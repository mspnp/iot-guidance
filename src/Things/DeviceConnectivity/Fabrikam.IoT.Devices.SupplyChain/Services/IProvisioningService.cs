// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.Azure.Devices.Provisioning.Service;
using System.Threading.Tasks;

namespace Fabrikam.IoT.Devices.SupplyChain.Services
{
    public interface IProvisioningService
    {
        Task<bool> CleanUpAndCreateEnrollmentGroupAsync(Attestation attestation);
    }
}
