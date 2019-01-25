// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Fabrikam.IoT.Devices.SupplyChain.Actors.Security;
using Fabrikam.IoT.Devices.SupplyChain.Actors.Security.Models;
using Fabrikam.IoT.Devices.SupplyChain.Security.X509Certificates;
using Fabrikam.IoT.Devices.SupplyChain.Tests.helpers;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Fabrikam.IoT.Devices.SupplyChain.Tests
{
    public class DeviceHSMX509StoreTests : IDisposable
    {
        private readonly ITestOutputHelper log;

        private readonly X509Certificate2 leaf;
        
        private readonly X509Certificate2Collection chain;

        private readonly Mock<ICertificateAuthority> ca;

        private readonly IDeviceHSM deviceHSM;

        public DeviceHSMX509StoreTests(ITestOutputHelper log)
        {
            this.log = log;            
            this.deviceHSM = new DeviceHSM();

            this.chain = new X509Certificate2Collection(
                               X509CertificateOperations
                                .CreateChainRequest(
                                   "CN=Fabrikam CA Root (Integration Test Use Only), O=Fabrikam Drone Delivery", 
                                    RSA.Create(3072), 
                                    HashAlgorithmName.SHA512, 
                                    true, 
                                    null)
                                .CreateX509SelfSignedCert(
                                    2));


            this.leaf = X509CertificateOperations
                                .CreateChainRequest(
                                    "CN=Fabrikam Leaf (Integration Test Use Only), O=Fabrikam Drone Delivery", 
                                    this.deviceHSM.GetPublicKey, 
                                    HashAlgorithmName.SHA256, 
                                    false,
                                    null)
                                .CreateX509Cert(
                                    this.chain[0],
                                    1);
            
            this.ca = new Mock<ICertificateAuthority>();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            this.leaf?.Dispose();
            this.chain[0]?.Dispose();
            X509CertificateOperations.RemoveCertsByOrganizationName(
                            StoreName.My,
                            StoreLocation.CurrentUser,
                            "Fabrikam Drone Delivery");
            X509CertificateOperations.RemoveCertsByOrganizationName(
                                        StoreName.Root,
                                        StoreLocation.CurrentUser,
                                        "Fabrikam Drone Delivery");
            deviceHSM?.Dispose();
        }

        [Fact, Trait(Constants.TYPE, Constants.INTEGRATION_TEST)]
        public void DeviceHSMStoresLeafCertOnceSuccessfully()
        {
            // Arrange
            X509Certificate2Collection colLeaf = null;
            this.deviceHSM.StoreX509Cert(this.leaf, this.chain);

            // Act
            colLeaf = this.leaf.FindCertBySubjectName(
                                  StoreName.My,
                                  StoreLocation.CurrentUser);
            
            // Assert
            Assert.NotNull(colLeaf);
            Assert.Single(colLeaf);
            Assert.NotNull(this.deviceHSM.DeviceLeafCert);
            Assert.False(this.deviceHSM.DeviceLeafCert.HasPrivateKey);
        }

        [Fact, Trait(Constants.TYPE, Constants.INTEGRATION_TEST)]
        public void DeviceHSMStoresRootCACertOnceSuccessfully()
        {
            // Arrange
            X509Certificate2Collection colRootCA = null;

            // Act
            this.deviceHSM.StoreX509Cert(this.leaf, this.chain);
            colRootCA = this.chain[0].FindCertBySubjectName(
                                        StoreName.Root,
                                        StoreLocation.CurrentUser);            
            // Assert
            Assert.NotNull(colRootCA);
            Assert.Single(colRootCA);
        }
    }
}
