// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Text;
using System.Threading.Tasks;
using Fabrikam.IoT.Devices.SupplyChain.Actors.Security.Models;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;

namespace Fabrikam.IoT.Devices.SupplyChain.Actors
{
    /// <summary>
    /// represent the IoT Device with an HSM silicon chip installed on it
    /// </summary>
    public class IoTDevice : IIoTDevice
    {
        private string deviceId;
        private string assignedIoTHub;
        private bool ready = false;

        public IoTDevice()
        {
            this.HardwareSecurityModel = new DeviceHSM();
        }

        public IDeviceHSM HardwareSecurityModel { get; }

        public string DeviceID => this.deviceId;

        public string AssignedIoTHub => this.assignedIoTHub;

        public ProvisioningRegistrationStatusType RegistrationStatus
                                                    => this.registrationStatus;

        private ProvisioningRegistrationStatusType registrationStatus { get; set; }

        public async Task AuthNAndSendTelemetryAsync()
        {
            if (!this.ready)
            {
                throw new InvalidOperationException($"The device {this.DeviceID} with status {this.RegistrationStatus} is not ready yet. Try to register it again.");
            }
            var auth = this.HardwareSecurityModel
                           .ExecPerformingCryptoOps(
                                (certWithKey) 
                                => new DeviceAuthenticationWithX509Certificate(
                                            this.DeviceID,
                                            certWithKey));
                using (DeviceClient iotClient = DeviceClient.Create(
                                                        this.AssignedIoTHub,
                                                        auth))
                {
                    await iotClient.OpenAsync().ConfigureAwait(false);

                    await iotClient.SendEventAsync(
                                        new Message(Encoding
                                                    .UTF8.GetBytes("TestMessage")))
                                    .ConfigureAwait(false);

                    await iotClient.CloseAsync().ConfigureAwait(false);
                }
        }

        public async Task ProvisionAsync(string dpsGlobalDeviceEndpoint,
                                            string dpsIdScope)
        {
            DeviceRegistrationResult result =
                                            await this.RegisterDeviceAsync(
                                                dpsGlobalDeviceEndpoint,
                                                dpsIdScope)
                                                      .ConfigureAwait(false);
            this.InitializeDevice(result);
        }

        protected void InitializeDevice(DeviceRegistrationResult result)
        {
            this.deviceId = result.DeviceId;
            this.assignedIoTHub = result.AssignedHub;
            this.registrationStatus = result.Status;
            this.ready = this.registrationStatus == ProvisioningRegistrationStatusType.Assigned;
        }

        protected virtual async Task<DeviceRegistrationResult> RegisterDeviceAsync(
                                                string dpsGlobalDeviceEndpoint,
                                                string dpsIdScope)
        {
            DeviceRegistrationResult result = null;

            using (var security =
                        new SecurityProviderX509Certificate(
                                    this.HardwareSecurityModel.DeviceLeafCert))
            using (var transport =
                        new ProvisioningTransportHandlerMqtt(
                                                TransportFallbackType.TcpOnly))
            {
                ProvisioningDeviceClient provClient =
                        ProvisioningDeviceClient.Create(
                                                    dpsGlobalDeviceEndpoint,
                                                    dpsIdScope,
                                                    security,
                                                    transport);

                result = await provClient.RegisterAsync().ConfigureAwait(false);
            }

            return result;
        }
    }
}