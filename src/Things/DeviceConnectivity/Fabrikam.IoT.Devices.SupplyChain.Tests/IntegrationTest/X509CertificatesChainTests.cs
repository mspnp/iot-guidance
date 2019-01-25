// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Fabrikam.IoT.Devices.SupplyChain.Actors;
using Fabrikam.IoT.Devices.SupplyChain.Actors.Security;
using Fabrikam.IoT.Devices.SupplyChain.Runtime;
using Fabrikam.IoT.Devices.SupplyChain.Security.X509Certificates;
using Fabrikam.IoT.Devices.SupplyChain.Services;
using Fabrikam.IoT.Devices.SupplyChain.Tests.helpers;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Fabrikam.IoT.Devices.SupplyChain.Tests
{
    public class X509CertificatesChainTests : IDisposable
    {
        /// <summary>The test logger</summary>
        private readonly ITestOutputHelper log;

        private readonly Mock<IConfig> config;

        // IoT Contract Manufactoring Company as Root CA
        private readonly IRootCertificateAuthority rootCA;
        // IoT Hardware Integrator as Intermediate CA
        private readonly ICertificateAuthority intermed1CA;
        // IoT Deployer as Intermediate CA
        private readonly ICertificateAuthority intermed2CA;

        public X509CertificatesChainTests(ITestOutputHelper log)
        {
            this.log = log;

            this.config = new Mock<IConfig>();
            this.config.Setup(c=>c.IoTCompanyName)
                       .Returns("company-x");
            this.config.Setup(c=>c.IoTHardwareIntegratorName)
                       .Returns("factory-y");
            this.config.Setup(c=>c.IoTDeployerName)
                       .Returns("technician-z");

            var config = this.config.Object;
            var provisioningService = new Mock<IProvisioningService>().Object;

            //TODO: future PR if team says "go", this needs to be tested with the Dependency Resolution
            this.rootCA = new IoTCompany(config, provisioningService);
            this.intermed1CA = new IoTHardwareIntegrator(
                                      config, 
                                      this.rootCA);
            this.intermed2CA = new IoTDeployer(
                                      config, 
                                      this.intermed1CA);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            this.intermed2CA?.Dispose();
            this.intermed1CA?.Dispose();
            this.rootCA?.Dispose();
        }

        private static void DisposeChainCerts(X509Chain chain)
        {
            foreach (X509ChainElement element in chain.ChainElements)
            {
                element.Certificate.Dispose();
            }
        }

        private static string RunChain(
            X509Chain chain,
            X509Certificate2 cert,
            string msg)
        {
            bool success = chain.Build(cert);

            FormattableString errMsg = null;

            if (!success)
            {
                for (int i = 0; i < chain.ChainElements.Count; i++)
                {
                    X509ChainElement element = chain.ChainElements[i];

                    if (element.ChainElementStatus.Length != 0)
                    {
                        X509ChainStatusFlags flags =
                            element.ChainElementStatus.Select(ces => ces.Status).Aggregate((a, b) => a | b);

                        errMsg = $"{msg}: chain error at depth {i}: {flags}";
                        break;
                    }
                }
            }
            
            return errMsg?.ToString(formatProvider: CultureInfo.CurrentCulture);
        }

        [Fact, Trait(Constants.TYPE, Constants.INTEGRATION_TEST)]
        public void CanRunBasicPolicyChainForRootCAX509Certificate()
        {
            // Arrange
            X509Certificate2 rootCACert = this.rootCA.personalSignedX509Certificate;
            string errMsg = null;
            var chain = new X509Chain();
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
                        
            // Act
            try
            {
                errMsg = RunChain(
                            chain, 
                            rootCACert, 
                            "Verify basic policy chain");
            }
            finally
            {
                DisposeChainCerts(chain);
            }

            // Assert
            Assert.True(string.IsNullOrEmpty(errMsg), errMsg);
            Assert.NotNull(rootCACert);
            Assert.False(rootCACert.HasPrivateKey);
            Assert.NotNull(rootCACert.GetRSAPublicKey());
            Assert.True(rootCACert.NotAfter <= (DateTimeOffset.UtcNow.AddDays(10000)));
            Assert.False(string.IsNullOrEmpty(rootCACert.Thumbprint));
            Assert.True(rootCACert.Version >= 3);
        }

        [Fact, Trait(Constants.TYPE, Constants.INTEGRATION_TEST)]
        public void CanRunChainOfTrustForIntermedX509Certificate()
        {
            // Arrange
            X509Certificate2 rootCACertWithKey = this.rootCA.personalSignedX509Certificate;
            X509Certificate2 intermedCertWithKey = this.intermed1CA.personalSignedX509Certificate;
            string errMsg = null;
            var chain = new X509Chain();
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority; 
            chain.ChainPolicy.ExtraStore.Add(rootCACertWithKey);

            // Act
            try
            {
                errMsg = RunChain(
                            chain, 
                            intermedCertWithKey, 
                            "Verify chain of trust for Intermediate X509 Cert");
            }
            finally
            {
                DisposeChainCerts(chain);
            }

            // Assert
            Assert.True(string.IsNullOrEmpty(errMsg), errMsg);
            Assert.NotNull(intermedCertWithKey);
            Assert.False(intermedCertWithKey.HasPrivateKey);
            Assert.NotNull(intermedCertWithKey.GetRSAPublicKey());
            Assert.True(intermedCertWithKey.NotAfter <= (DateTimeOffset.UtcNow.AddDays(1504)));
            Assert.False(string.IsNullOrEmpty(intermedCertWithKey.Thumbprint));
            Assert.True(intermedCertWithKey.Version >= 3);
        }

        [Fact, Trait(Constants.TYPE, Constants.INTEGRATION_TEST)]
        public void CanRunChainOfTrustForEndEnitiyLeafX509Certificates()
        {
            // Arrange
            const string endEntityLeafDistinguishedName = "CN=End Entity Leaf Certificate, O=Fabrikam_DD";

            X509Certificate2 rootCACert, 
                             intermed1Cert, 
                             intermed2Cert, 
                             endEntityLeafCertificate = null;

            rootCACert = this.rootCA.personalSignedX509Certificate;
            intermed1Cert = this.intermed1CA.personalSignedX509Certificate;
            intermed2Cert = this.intermed2CA.personalSignedX509Certificate;

            CertificateRequest endEntityLeafCSR = null;

            string errMsg = null;
            var chain = new X509Chain();
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
            chain.ChainPolicy.ExtraStore.Add(intermed2Cert);
            chain.ChainPolicy.ExtraStore.Add(intermed1Cert);
            chain.ChainPolicy.ExtraStore.Add(rootCACert);

            // Act
            try
            {
                endEntityLeafCSR = X509CertificateOperations
                                                .CreateChainRequest(
                                                    endEntityLeafDistinguishedName, 
                                                    RSA.Create(1536), 
                                                    HashAlgorithmName.SHA256, 
                                                    false, null);

                endEntityLeafCertificate = this.intermed2CA
                                                .CreateSignedCrt(
                                                            endEntityLeafCSR);
                errMsg = RunChain(
                            chain, 
                            endEntityLeafCertificate, 
                            "Verify chain of trust for End Entity Leaf X509 Cert");
            }
            finally
            {
                DisposeChainCerts(chain);
            }

            // Assert
            Assert.True(string.IsNullOrEmpty(errMsg), errMsg);
            Assert.NotNull(endEntityLeafCertificate);
            Assert.False(endEntityLeafCertificate.HasPrivateKey);
            Assert.NotNull(endEntityLeafCertificate.GetRSAPublicKey());
            Assert.True(endEntityLeafCertificate.NotAfter <= (DateTimeOffset.UtcNow.AddDays(30)));
            Assert.False(string.IsNullOrEmpty(endEntityLeafCertificate.Thumbprint));
            Assert.True(endEntityLeafCertificate.Version >= 3);
        }
    }
}
