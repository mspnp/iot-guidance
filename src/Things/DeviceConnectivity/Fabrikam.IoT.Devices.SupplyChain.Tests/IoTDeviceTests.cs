// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Threading.Tasks;
using Fabrikam.IoT.Devices.SupplyChain.Actors;
using Fabrikam.IoT.Devices.SupplyChain.Tests.helpers;
using Microsoft.Azure.Devices.Provisioning.Client;
using Xunit;
using Xunit.Abstractions;


namespace Fabrikam.IoT.Devices.SupplyChain.Tests
{
    public class IoTDeviceTests
    {
        /// <summary>The test logger</summary>
        private readonly ITestOutputHelper log;

        public IoTDeviceTests(ITestOutputHelper log)
        {
            this.log = log;
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
#pragma warning disable CA1822 // Mark members as static
        public void IIoTDeviceIsProperlyInitialized()
#pragma warning restore CA1822 // Mark members as static
        {
            // Arrange
            IIoTDevice device = null;

            // Act
            device = new IoTDevice();

            // Assert
            Assert.NotNull(device.HardwareSecurityModel);
            Assert.True(string.IsNullOrEmpty(device.DeviceID));
            Assert.True(string.IsNullOrEmpty(device.AssignedIoTHub));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
#pragma warning disable CA1822 // Mark members as static
        public void IIoTDeviceCanBeProvisioned()
#pragma warning restore CA1822 // Mark members as static
        {
            // Arrange
            IIoTDevice device = new IoTDeviceFake();

            // Act
            device.ProvisionAsync("testGlobalEP","testScopeId")
                                                    .GetAwaiter().GetResult();

            // Assert
            Assert.False(string.IsNullOrEmpty(device.DeviceID));
            Assert.Equal("testRegistrationId", device.DeviceID);
            Assert.False(string.IsNullOrEmpty(device.AssignedIoTHub));
            Assert.Equal("myHub", device.AssignedIoTHub);            
            Assert.Equal(ProvisioningRegistrationStatusType.Assigned, 
                        device.RegistrationStatus);
        }
    }

    public class IoTDeviceFake: IoTDevice
    {
        protected override async Task<DeviceRegistrationResult> RegisterDeviceAsync(                            
                                        string dpsGlobalDeviceEndpoint, 
                                        string dpsIdScope)
        {
            var result = new DeviceRegistrationResult(
                                            "testRegistrationId", 
                                            null, 
                                            "myHub", 
                                            "testRegistrationId", 
                                            ProvisioningRegistrationStatusType
                                                .Assigned, 
                                            "testGenerationId", 
                                            null,
                                            0, 
                                            string.Empty, 
                                            "testetag");

            return await Task.FromResult(result).ConfigureAwait(false);
        }
    }
}
