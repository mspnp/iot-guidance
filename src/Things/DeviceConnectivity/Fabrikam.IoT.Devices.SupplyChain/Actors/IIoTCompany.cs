// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Threading.Tasks;

namespace Fabrikam.IoT.Devices.SupplyChain.Actors
{
    public interface IIoTCompany
    {
        Task GenerateProofOfVerficationAsync(string dn,
                                             string fileName = null);

        Task CleanUpAndCreateEnrollmentGroupAsync();

        Task<IIoTDevice> MakeDeviceAsync();
    }
}