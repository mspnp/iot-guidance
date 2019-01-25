// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Fabrikam.IoT.Devices.SupplyChain.Actors;
using Fabrikam.IoT.Devices.SupplyChain.Actors.Security;
using Fabrikam.IoT.Devices.SupplyChain.Runtime;
using Fabrikam.IoT.Devices.SupplyChain.Security.X509Certificates;
using Fabrikam.IoT.Devices.SupplyChain.Tests.helpers;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Fabrikam.IoT.Devices.SupplyChain.Tests
{
    public class IoTHardwareIntegratorTests : IDisposable
    {
        private readonly ITestOutputHelper log;

        private readonly X509Certificate2Collection chain;

        private readonly Mock<IConfig> config;

        private readonly Mock<ICertificateAuthority> mockCA;

        public IoTHardwareIntegratorTests(ITestOutputHelper log)
        {
            this.log = log;

            this.config = new Mock<IConfig>();
            this.config.Setup(c=>c.IoTDeployerName).Returns("technician-z");
            
            this.mockCA = new Mock<ICertificateAuthority>();

            DateTimeOffset now = DateTimeOffset.UtcNow;

            this.chain = new X509Certificate2Collection(X509CertificateOperations
                                .CreateChainRequest(
                                    "CN=Fabrikam CA Root (Unit Test Use Only)", 
                                    RSA.Create(3072), 
                                    HashAlgorithmName.SHA512, 
                                    true, null)
                                .CreateX509SelfSignedCert(
                                    10000));
            
            this.mockCA
                .Setup(ca=> ca.CreateSignedCrt(It.IsAny<CertificateRequest>()))
                .Returns( (CertificateRequest csr) => 
                                       csr.CreateX509Cert(
                                               this.chain[0], 
                                               4000));
            this.mockCA.Setup(ca=>ca.personalSignedX509Certificate)
                       .Returns(this.chain[0]);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            this.chain[0]?.Dispose();
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void IoTHardwareIntegratorCanManufactureANewIoTDevice()
        {
            // Arrange
            var mockIoTDeployer = new Mock<IIoTDeployer>();
            var factory = new IoTHardwareIntegrator(this.config.Object,
                                                this.mockCA.Object,
                                                mockIoTDeployer.Object);
            
            // Act
            var device = factory.ManufactureAsync("testDPSGlobalEndpoint", 
                                              "testDPSScopeId")
                                              .GetAwaiter()
                                              .GetResult();

            // Assert
            Assert.NotNull(device);
            mockIoTDeployer.Verify(
                d => d.InstallAsync(
                        It.IsAny<IIoTDevice>(), 
                        It.IsAny<string>(), 
                        It.IsAny<string>()));
        }
    }
}
