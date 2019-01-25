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
    /// represents the IoT Deployer role and also act as an intermediate CA 
    /// </summary>
    public class IoTDeployer: IIoTDeployer, ICertificateAuthority
    {  
        private readonly ICertificateAuthority parentIntermedCertificateAuthority;
        private readonly X509Certificate2 intermedCACertWithKey = null;
        private readonly X509Certificate2 intermedCACert = null;
        private readonly IConfig config = null;

        // TODO: future PR if team says "go", add this as single instance DependencyResolution 
        public IoTDeployer(
                        IConfig config, 
                        ICertificateAuthority parentIntermedCertificateAuthority)
        {
            this.config = config ?? 
                                throw new ArgumentNullException(nameof(config));
            this.parentIntermedCertificateAuthority = parentIntermedCertificateAuthority;
            this.intermedCACertWithKey = this.AcquireCASignedIntermediateCertWithKey();
            this.intermedCACert = this.intermedCACertWithKey.CloneWithoutKey();
        }

        public string Name => this.config.IoTDeployerName;

        public X509Certificate2 personalSignedX509Certificate => this.intermedCACert;

        public ICertificateAuthority ParentCertificateAuthority
                                    => this.parentIntermedCertificateAuthority;

        // e.g. "CN=Fabrikam Drone Factory Technician Intermediate Certificate, O=Fabrikam_DD"
        private string IntermedDistinguishedName 
                            => $"CN={this.Name} Intermediate CA Certificate (Test Use Only), O=Fabrikam Drone Delivery";

        public string GenerateLeafCertDistinguishedName(string uniqueDeviceId) => $"CN={uniqueDeviceId}, O=Fabrikam Drone Delivery";

        public X509Certificate2 CreateSignedCrt(CertificateRequest csr)
        {
            X509Certificate2 leafCert = csr.CreateX509Cert(
                                               this.intermedCACertWithKey, 
                                               30);
            return leafCert;
        }

        public async Task InstallAsync(IIoTDevice device, string dpsGlobalEndpoint, string dpsScopeId)
        {
            if (device == null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            var chain = new X509Certificate2Collection();
            this.CollectCerts(this, chain);
            this.InjectChainOfTrust(device, chain);
            // provisioning here
            await device.ProvisionAsync(dpsGlobalEndpoint, dpsScopeId).ConfigureAwait(false);
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

        private void CollectCerts(ICertificateAuthority ca, X509Certificate2Collection chain)
        {
            chain.Add(ca.personalSignedX509Certificate);
            
            if(ca.ParentCertificateAuthority == null)
                return;

            this.CollectCerts(ca.ParentCertificateAuthority, chain);
        }

        private X509Certificate2 AcquireCASignedIntermediateCertWithKey()
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
                X509Certificate2 intermedCert = this.parentIntermedCertificateAuthority
                                                .CreateSignedCrt(intermedRequest);

                // Save its private key for future issuing as Itermediate CA 
                intermedCertWithKey = intermedCert
                                        .CloneWithPrivateKey(intermedPrivKey);

                intermedCert.Dispose();
            }

            return intermedCertWithKey;
        }

        // Install a signed leaft X509 Cert in a Device HSM-based
        private IIoTDevice InjectChainOfTrust(IIoTDevice device,
                                       X509Certificate2Collection chain)
        {
            if (device == null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            X509Certificate2 deviceLeafCert = null;

            // Create csr for a Device
            CertificateRequest deviceLeafRequest = 
                                  X509CertificateOperations.CreateChainRequest(
                                    this.GenerateLeafCertDistinguishedName(
                                            device
                                            .HardwareSecurityModel
                                            .GetUniqueDeviceId),
                                    device.HardwareSecurityModel.GetPublicKey,
                                    HashAlgorithmName.SHA256,
                                    false, null);

            // issue a signed leaf crt
            deviceLeafCert = this.CreateSignedCrt(deviceLeafRequest);

            device.HardwareSecurityModel.StoreX509Cert(deviceLeafCert, chain);

            return device;
        }
    }
}