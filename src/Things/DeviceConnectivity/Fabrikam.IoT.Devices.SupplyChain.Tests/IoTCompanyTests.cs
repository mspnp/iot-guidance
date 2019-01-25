// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Fabrikam.IoT.Devices.SupplyChain.Actors;
using Fabrikam.IoT.Devices.SupplyChain.Runtime;
using Fabrikam.IoT.Devices.SupplyChain.Services;
using Fabrikam.IoT.Devices.SupplyChain.Tests.helpers;
using Microsoft.Azure.Devices.Provisioning.Service;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Fabrikam.IoT.Devices.SupplyChain.Tests
{
    public class IoTCompanyTests
    {
        /// <summary>The test logger</summary>
        private readonly ITestOutputHelper log;

        private readonly Mock<IConfig> config;

        public IoTCompanyTests(ITestOutputHelper log)
        {
            this.log = log;

            this.config = new Mock<IConfig>();
            this.config.Setup(c => c.IoTCompanyName).Returns("company-x");
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void IoTCompanyMakeAnIoTDeviceSucessfully()
        {
            // Arrange
            var mockProvisioningService = new Mock<IProvisioningService>();
            mockProvisioningService
                .Setup(
                    pS => pS.CleanUpAndCreateEnrollmentGroupAsync(
                              It.IsAny<Attestation>()))
                .Returns(Task.FromResult(true));
            var mockFactory = new Mock<IIoTHardwareIntegrator>();
            mockFactory
             .Setup(
                f => f.ManufactureAsync(It.IsAny<string>(), It.IsAny<string>()))
             .Returns(
                Task.FromResult<IIoTDevice>(new Mock<IIoTDevice>().Object));
            IIoTCompany company = new IoTCompany(
                                        this.config.Object,
                                        mockProvisioningService.Object,
                                        mockFactory.Object);

            IIoTDevice brandNewIoTDevice = null;

            // Act
            company.CleanUpAndCreateEnrollmentGroupAsync().GetAwaiter().GetResult();
            brandNewIoTDevice = company.MakeDeviceAsync().GetAwaiter().GetResult();

            // Assert
            Assert.NotNull(brandNewIoTDevice);
            mockFactory.Verify(f => f.ManufactureAsync(It.IsAny<string>(), It.IsAny<string>()));
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void IoTCompanyMakeIoTDeviceThrowsExceptionCauseNeverCreatedEnrollmentGroup()
        {
            // Arrange
            var mockProvisioningService = new Mock<IProvisioningService>();
            var mockFactory = new Mock<IIoTHardwareIntegrator>();
            mockFactory
             .Setup(
                f => f.ManufactureAsync(It.IsAny<string>(), It.IsAny<string>()))
             .Returns(
                Task.FromResult<IIoTDevice>(new Mock<IIoTDevice>().Object));
            IIoTCompany company = new IoTCompany(
                                        this.config.Object,
                                        mockProvisioningService.Object,
                                        mockFactory.Object);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(
                    () => company.MakeDeviceAsync().GetAwaiter().GetResult());
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void IoTCompanyMakeIoTDeviceThrowsExceptionCauseFailedEnrollmentGroupCreation()
        {
            // Arrange
            var mockProvisioningService = new Mock<IProvisioningService>();
            mockProvisioningService
                .Setup(
                    pS => pS.CleanUpAndCreateEnrollmentGroupAsync(
                              It.IsAny<Attestation>()))
                .Returns(Task.FromResult(false));
            var mockFactory = new Mock<IIoTHardwareIntegrator>();
            mockFactory
             .Setup(
                f => f.ManufactureAsync(It.IsAny<string>(), It.IsAny<string>()))
             .Returns(
                Task.FromResult<IIoTDevice>(new Mock<IIoTDevice>().Object));
            IIoTCompany company = new IoTCompany(
                                        this.config.Object,
                                        mockProvisioningService.Object,
                                        mockFactory.Object);

            // Act & Assert
            company.CleanUpAndCreateEnrollmentGroupAsync().GetAwaiter().GetResult();
            Assert.Throws<InvalidOperationException>(
                    () => company.MakeDeviceAsync().GetAwaiter().GetResult());
        }
    }
}
