// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Fabrikam.IoT.Devices.SupplyChain.Actors.Security;
using Fabrikam.IoT.Devices.SupplyChain.Runtime;
using Fabrikam.IoT.Devices.SupplyChain.Security.X509Certificates;

namespace Fabrikam.IoT.Devices.SupplyChain.Actors
{
    /// <summary>
    /// represents the IoT Hardware Integrator role and also act as an intermediate CA 
    /// </summary>
    public class IoTHardwareIntegrator: IIoTHardwareIntegrator, ICertificateAuthority
    {   
        private readonly ICertificateAuthority rootCertificateAuthority;
        private readonly X509Certificate2 intermedCACertWithKey = null;
        private readonly X509Certificate2 intermedCACert = null;
        private readonly IConfig config = null;

        // TODO: future PR if team says "go", add this as single instance DependencyResolution 
        public IoTHardwareIntegrator(IConfig config,
                                ICertificateAuthority rootCertificateAuthority,
                                IIoTDeployer deployer = null)
        {
            this.config = config ??
                                throw new ArgumentNullException(nameof(config));
            this.rootCertificateAuthority = rootCertificateAuthority;
            this.intermedCACertWithKey = this.AcquireCASigneIntermediateCertWithKey();
            this.intermedCACert = this.intermedCACertWithKey.CloneWithoutKey();
            this.Deployer = deployer ?? new IoTDeployer(config, this);
        }

        public string Name => this.config.IoTHardwareIntegratorName;

        // e.g. "CN=Fabrikam Drone Factory Intermediate Certificate, O=Fabrikam_DD";
        private string IntermedDistinguishedName
                            => $"CN={this.Name} Intermediate CA Certificate (Test Use Only), O=Fabrikam Drone Delivery";

        public X509Certificate2 personalSignedX509Certificate 
                                        => this.intermedCACert;

        public ICertificateAuthority ParentCertificateAuthority 
                                        => this.rootCertificateAuthority;

        private IIoTDeployer Deployer {get;}

        public X509Certificate2 CreateSignedCrt(CertificateRequest csr)
        {
            X509Certificate2 intermedCert = csr.CreateX509Cert(
                                                   this.intermedCACertWithKey,
                                                   476);

            return intermedCert;
        }

        public async Task<IIoTDevice> ManufactureAsync(string dpsGlobalEndpoint,
                                                       string dpsScopeId)
        {
            var device = new IoTDevice();

            // hand off for installation and first time provisioning
            await this.ShipAsync(device, dpsGlobalEndpoint, dpsScopeId)
                      .ConfigureAwait(false);

            return device;
        }

        public async Task ShipAsync(IIoTDevice device,
                                    string dpsGlobalEndpoint,
                                    string dpsScopeId)
        {
            await this.Deployer.InstallAsync(device,
                                             dpsGlobalEndpoint,
                                             dpsScopeId)
                               .ConfigureAwait(false);
        }

        public void Dispose()
        {  
            Dispose(true);  
            GC.SuppressFinalize(this);  
        }  

        protected virtual void Dispose(bool disposing)
        {  
            if (disposing)
            {
                this.intermedCACertWithKey?.Dispose(); 
            }  
        }

        private X509Certificate2 AcquireCASigneIntermediateCertWithKey()
        {
            X509Certificate2 intermedCertWithKey = null;

            using (AsymmetricAlgorithm intermedPrivKey = RSA.Create(2048))
            {
                // Create csr
                CertificateRequest intermedRequest =
                    X509CertificateOperations.CreateChainRequest(
                                                IntermedDistinguishedName,
                                                intermedPrivKey,
                                                HashAlgorithmName.SHA384,
                                                true, null);

                // Request Root CA to issue a signed intermed crt
                X509Certificate2 intermedCert = this.rootCertificateAuthority
                                                .CreateSignedCrt(intermedRequest);

                // Save its private key for future issuing as Itermediate CA 
                intermedCertWithKey = intermedCert
                                        .CloneWithPrivateKey(intermedPrivKey);

                intermedCert.Dispose();
            }

            return intermedCertWithKey;
        }
    }
}