// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Fabrikam.IoT.Devices.SupplyChain.Actors.Security.Models;
using Fabrikam.IoT.Devices.SupplyChain.Tests.helpers;
using Xunit;
using Xunit.Abstractions;

namespace Fabrikam.IoT.Devices.SupplyChain.Tests
{
    public class DeviceHSMTests
    {
        /// <summary>The test logger</summary>
        private readonly ITestOutputHelper log;

        public DeviceHSMTests(ITestOutputHelper log)
        {
            this.log = log;
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
#pragma warning disable CA1822 // Mark members as static
        public void DeviceHSMIsProperlyIntialized()
#pragma warning restore CA1822 // Mark members as static
        {
            // Arrange
            DeviceHSM deviceHSM = null;
        
            // Act
            deviceHSM = new DeviceHSM();
            
            // Assert
            Assert.NotNull(deviceHSM);
            Assert.Null(deviceHSM.DeviceLeafCert);
            Assert.NotNull(deviceHSM.GetPublicKey);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
#pragma warning disable CA1822 // Mark members as static
        public void DeviceHSMsKnowHowToGenerateAValidUniqueDeviceId()
#pragma warning restore CA1822 // Mark members as static
        {
            // Arrange
            DeviceHSM deviceHSM = new DeviceHSM();

            string leafDN = string.Empty;
            var pattern = "^[a-z0-9-]+$";

            // Act
            var unitqueDeviceId = deviceHSM.GetUniqueDeviceId;

            // Assert
            Assert.False(string.IsNullOrEmpty(unitqueDeviceId));
            Assert.Matches(pattern, unitqueDeviceId);
        }
    }
}