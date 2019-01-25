// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Fabrikam.IoT.Devices.SupplyChain.Actors;
using Fabrikam.IoT.Devices.SupplyChain.Actors.Security;
using Fabrikam.IoT.Devices.SupplyChain.Actors.Security.Models;
using Fabrikam.IoT.Devices.SupplyChain.Runtime;
using Fabrikam.IoT.Devices.SupplyChain.Security.X509Certificates;
using Fabrikam.IoT.Devices.SupplyChain.Tests.helpers;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Fabrikam.IoT.Devices.SupplyChain.Tests
{
    public class IoTDeployerTests: IDisposable
    {
        private readonly ITestOutputHelper log;

        private readonly X509Certificate2Collection chain;

        private readonly Mock<IConfig> config;

        private readonly Mock<ICertificateAuthority> mockCA;

        public IoTDeployerTests(ITestOutputHelper log)
        {
            this.log = log;

            this.config = new Mock<IConfig>();
            this.config.Setup(c=>c.IoTDeployerName).Returns("techinician-z");
            
            this.mockCA = new Mock<ICertificateAuthority>();

            this.chain = new X509Certificate2Collection(X509CertificateOperations
                                .CreateChainRequest(
                                    "CN=Fabrikam CA Root (Unit Test Use Only)", 
                                    RSA.Create(3072), 
                                    HashAlgorithmName.SHA512, 
                                    true, 
                                    null)
                                .CreateX509SelfSignedCert(
                                    60));
            
            this.mockCA.Setup(ca 
                                => ca.CreateSignedCrt(
                                         It.IsAny<CertificateRequest>()))
                       .Returns( (CertificateRequest csr) => 
                                   csr.CreateX509Cert(
                                          this.chain[0], 
                                          45));
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
        public void IoTDeployersKnowHowToGenerateAValidDistiguishedName()
        {
            // Arrange
            IIoTDeployer deployer = new IoTDeployer(
                                            this.config.Object,
                                            this.mockCA.Object);
            string leafDN = string.Empty;
            var pattern = "^CN=[a-z0-9-]+, O=Fabrikam Drone Delivery$";
		    
            // Act
            leafDN = deployer.GenerateLeafCertDistinguishedName("test");
            
            // Assert
            Assert.False(string.IsNullOrEmpty(leafDN));
            Assert.Matches(pattern, leafDN);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void IoTDeployersCanInstallDevices()
        {
            // Arrange
            IIoTDeployer deployer = new IoTDeployer(
                                            this.config.Object,
                                            this.mockCA.Object);
            
            var mockIoTDevice = new Mock<IIoTDevice>();		
            var mockDeviceHSM = new Mock<IDeviceHSM>();		
            mockDeviceHSM.Setup(hsm=>hsm.GetPublicKey)
                         .Returns(RSA.Create(1536));
            mockIoTDevice.Setup(d => d.HardwareSecurityModel)
                         .Returns(mockDeviceHSM.Object);

            // Act
            deployer.InstallAsync(mockIoTDevice.Object,
                                       "testDPSGlobalEndpoint",
                                       "testDPSScopeId").
                    GetAwaiter().GetResult();

            // Assert
            mockDeviceHSM.Verify(hsm => 
                            hsm.StoreX509Cert(
                                    It.IsAny<X509Certificate2>(),
                                    It.IsAny<X509Certificate2Collection>()));
            
            mockIoTDevice.Verify(d => 
                            d.ProvisionAsync(
                                 "testDPSGlobalEndpoint",
                                 "testDPSScopeId"));
        }
    }
}
