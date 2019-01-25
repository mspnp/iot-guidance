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
using Fabrikam.IoT.Devices.SupplyChain.Services;
using Fabrikam.IoT.Devices.SupplyChain.Tests.helpers;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Fabrikam.IoT.Devices.SupplyChain.Tests
{
    public class CertificateAuthorityTests : IDisposable
    {
        /// <summary>The test logger</summary>
        private readonly ITestOutputHelper log;

        private readonly Mock<IConfig> config;

        private readonly X509Certificate2Collection Chain;

        private readonly IDeviceHSM deviceHSM;

        private readonly CertificateRequest leafCsr;

        public CertificateAuthorityTests(ITestOutputHelper log)
        {
            this.log = log;

            this.config = new Mock<IConfig>();
            this.config.Setup(c=>c.IoTCompanyName).Returns("company-x");
            this.config.Setup(c=>c.IoTHardwareIntegratorName)
                       .Returns("factory-y");

            this.deviceHSM = new DeviceHSM();

            DateTimeOffset now = DateTimeOffset.UtcNow;

            this.Chain = new X509Certificate2Collection(X509CertificateOperations
                                .CreateChainRequest("CN=rootCATests", 
                                                    RSA.Create(3072), 
                                                    HashAlgorithmName.SHA512, 
                                                    true, null)
                                        .CreateSelfSigned(now,
                                        now.AddDays(2)));

            this.leafCsr = X509CertificateOperations
                    .CreateChainRequest("CN=leafCATests", 
                                        this.deviceHSM.GetPublicKey, 
                                        HashAlgorithmName.SHA256, 
                                        false, null);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void CAIsAbleToAcquireAValidCertAndIsProperlyInitialized()
        {
            // Arrange
            IConfig config = this.config.Object;
            var provisioningService = new Mock<IProvisioningService>().Object;
            IRootCertificateAuthority rootCA = new IoTCompany(
                                                    config,
                                                    provisioningService);
            ICertificateAuthority intermedCA = null;

            // Act
            intermedCA = new IoTHardwareIntegrator(config, rootCA);
            
            // Assert
            Assert.NotNull(intermedCA.personalSignedX509Certificate);
            Assert.NotNull(intermedCA.ParentCertificateAuthority);
            Assert.Equal(rootCA, intermedCA.ParentCertificateAuthority);
            Assert.False(intermedCA.personalSignedX509Certificate.HasPrivateKey);
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void CAIsAbleToSignCerts()
        {
            // Arrange
            IConfig config = this.config.Object;
            var provisioningService = new Mock<IProvisioningService>().Object;

            IRootCertificateAuthority rootCA = new IoTCompany(
                                                        config, 
                                                        provisioningService);
            ICertificateAuthority intermedCA = new IoTHardwareIntegrator(config, rootCA);
            X509Certificate2 signedCrt = null;

            // Act
            signedCrt = intermedCA.CreateSignedCrt(this.leafCsr);

            // Assert
            Assert.NotNull(signedCrt);
            // TODO: future PR if team says "go", improve assertions here: isCa false, subject, etc. 
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            this.deviceHSM?.Dispose();
        }
    }
}
