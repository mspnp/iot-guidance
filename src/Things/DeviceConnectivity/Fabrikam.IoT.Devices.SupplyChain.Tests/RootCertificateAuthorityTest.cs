// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Security.Cryptography.X509Certificates;
using Fabrikam.IoT.Devices.SupplyChain.Actors;
using Fabrikam.IoT.Devices.SupplyChain.Actors.Security;
using Fabrikam.IoT.Devices.SupplyChain.Runtime;
using Fabrikam.IoT.Devices.SupplyChain.Services;
using Fabrikam.IoT.Devices.SupplyChain.Tests.helpers;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Fabrikam.IoT.Devices.SupplyChain.Tests
{
    public class RootCertificateAuthorityTest
    {
        /// <summary>The test logger</summary>
        private readonly ITestOutputHelper log;
        private readonly Mock<IConfig> config;

        public RootCertificateAuthorityTest(ITestOutputHelper log)
        {
            this.log = log;
            this.config = new Mock<IConfig>();
            this.config.Setup(c=>c.IoTCompanyName).Returns("company-x");
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void CAIsAbleToAcquireAValidSelfSignedCert()
        {
            // Arrange
            var provisioningService = new Mock<IProvisioningService>().Object;

            IRootCertificateAuthority rootCA = 
                                            new IoTCompany(this.config.Object,
                                                           provisioningService);   
            X509Certificate2 justAnotherSelfSignedCert = null;

            // Act
            justAnotherSelfSignedCert = rootCA.AcquireSelfSignedCrt();      
            
            // Assert
            Assert.NotNull(justAnotherSelfSignedCert);
            // TODO: future PR if team says "go", improve assertions here
        }

        [Fact, Trait(Constants.TYPE, Constants.UNIT_TEST)]
        public void RootCADoesntHaveParentCAAndIsProperlyInitialized()
        {
            // Arrange
            IRootCertificateAuthority rootCA = null;   
            var provisioningService = new Mock<IProvisioningService>().Object;

            // Act
            rootCA = new IoTCompany(this.config.Object,
                                    provisioningService);   

            // Assert
            Assert.Null(rootCA.ParentCertificateAuthority);
            Assert.NotNull(rootCA.personalSignedX509Certificate);
        }
    }
}