// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Fabrikam.IoT.Devices.SupplyChain.Runtime;
using Microsoft.Azure.Devices.Provisioning.Service;

namespace Fabrikam.IoT.Devices.SupplyChain.Services
{
    public class ProvisioningService : IProvisioningService
    {
        private readonly IConfig config = null;

        public ProvisioningService(IConfig config)
        {
            this.config = config ??
                    throw new ArgumentNullException(nameof(config));
        }

        private string DPSConnStr => this.config.AzureDPSConnectionString;

        private string DPSEnrollmentGroup => this.config.AzureDPSEnrollmentGroup;

        public async Task<bool> CleanUpAndCreateEnrollmentGroupAsync(
                                                Attestation attestation)
        {
            EnrollmentGroup enrollmentGroup = await TryGetEnrollmentGroupAsync()
                                                    .ConfigureAwait(false);
            if(enrollmentGroup != null)
                await TryDeleteEnrollmentGroupAsync(enrollmentGroup)
                      .ConfigureAwait(false);

            var result = await CreateEnrollmentGroupAsync(attestation)
                                        .ConfigureAwait(false);

            return CheckReadiness(result);
        }

        private static bool CheckReadiness(EnrollmentGroup result)
        {
            var ready = result?.ProvisioningStatus == ProvisioningStatus.Enabled;
            return ready;
        }

        private async Task<EnrollmentGroup> CreateEnrollmentGroupAsync(Attestation attestation)
        {
            EnrollmentGroup enrollmentGroupResult = null;

            using (ProvisioningServiceClient provisioningServiceClient =
                    ProvisioningServiceClient.CreateFromConnectionString(
                                                 this.DPSConnStr))
            {
                EnrollmentGroup enrollmentGroup = new EnrollmentGroup(
                                                        this.DPSEnrollmentGroup,
                                                        attestation);
                enrollmentGroup.ProvisioningStatus = ProvisioningStatus.Enabled;

                // Create the enrollmentGroup
                enrollmentGroupResult =
                    await provisioningServiceClient
                          .CreateOrUpdateEnrollmentGroupAsync(enrollmentGroup)
                          .ConfigureAwait(false);
            }

            return enrollmentGroupResult;
        }

        private async Task<EnrollmentGroup> TryGetEnrollmentGroupAsync()
        {
            EnrollmentGroup enrollmentGroupResult = null;

            using (ProvisioningServiceClient provisioningServiceClient =
        ProvisioningServiceClient.CreateFromConnectionString(this.DPSConnStr))
            {
                try
                {
                    // Get the enrollmentGroup
                    enrollmentGroupResult =
                        await provisioningServiceClient
                                                  .GetEnrollmentGroupAsync(
                                                        this.DPSEnrollmentGroup)
                                                  .ConfigureAwait(false);
                }
                catch (ProvisioningServiceClientHttpException ex)
                {
                    // not found is ok
                    if (!ex.ErrorMessage.Equals("Not Found",
                                            StringComparison.
                                            InvariantCultureIgnoreCase))
                    {
                        throw;
                    }
                }
            }

            return enrollmentGroupResult;
        }


        private async Task TryDeleteEnrollmentGroupAsync(EnrollmentGroup enrollmentGroup)
        {
            using (ProvisioningServiceClient provisioningServiceClient =
        ProvisioningServiceClient.CreateFromConnectionString(this.DPSConnStr))
            {
                try
                {
                    // Delete the enrollmentGroup
                    await provisioningServiceClient
                            .DeleteEnrollmentGroupAsync(enrollmentGroup)
                            .ConfigureAwait(false);
                }
                catch (ProvisioningServiceClientHttpException ex)
                {
                    // not found is ok
                    if (!ex.ErrorMessage.Equals("Not Found", 
                                            StringComparison.
                                            InvariantCultureIgnoreCase))
                    {
                        throw;
                    }
                }
            }
        }

    }
}
