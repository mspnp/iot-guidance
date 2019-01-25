// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Fabrikam.IoT.Devices.SupplyChain.Security.X509Certificates;

namespace Fabrikam.IoT.Devices.SupplyChain.Actors.Security.Models
{
    /// <summary>
    /// represents secure silicon chip in the form of Hardware Secure Modules (HSM)
    /// </summary>
    /// <remarks>
    /// While real workd HSM devices are way too different from this 
    /// respresentation, this model gives the idea that HSM are capable of 
    /// internally generating private keys that will never see the light of day.
    ///
    /// In general, a HSM should be compliance with security
    /// requirements (e.g. FIPS 140-2 standard, PCI, etc.), could be aligned with 
    /// DICE+RIoT security architectures, and can be utilized through 
    /// CryptoAPI interfaces. 
    /// </remarks>
    /// <see cref="http://download.microsoft.com/download/C/0/5/C05276D6-E602-4BB1-98A4-C29C88E57566/The_right_secure_hardware_for_your_IoT_deployment_EN_US.pdf">The Right Secure Hardware for your IoT Deployment</see>
    /// <see cref="https://www.microsoft.com/en-us/research/publication/device-identity-dice-riot-keys-certificates/">Device Identity with DICE and RIoT: Keys and Certificates</see>
    /// <see cref="https://www.microsoft.com/en-us/research/wp-content/uploads/2016/06/RIoT20Paper-1.1-1.pdf">RIoT</see>
    public class DeviceHSM: IDeviceHSM
    {
        private readonly RSA deviceLeafPrivKey;
        private readonly RSA deviceLeafPubKey;

        private X509Certificate2 deviceLeafCertWithKey = null;
        private X509Certificate2 deviceLeafCert = null;
        private X509Certificate2Collection chain = null;

        public DeviceHSM()
        {
            this.deviceLeafPrivKey = RSA.Create(1536);
            this.deviceLeafPubKey = RSA.Create(
                                    deviceLeafPrivKey.ExportParameters(false));
        }

        public RSA GetPublicKey => this.deviceLeafPubKey;

        public X509Certificate2 DeviceLeafCert => this.deviceLeafCert;

        public string GetUniqueDeviceId => "simulated-drone-device01";

        public void StoreX509Cert(X509Certificate2 leafCrt, 
                                  X509Certificate2Collection chain)
        {
            // Guards
            this.deviceLeafCert = leafCrt ?? throw new ArgumentNullException(
                                                            nameof(leafCrt));
            this.chain = chain ?? throw new ArgumentNullException(nameof(chain));
            if (this.chain.Count == 0)
            {
                throw new ArgumentException("at least one CA cert is required",
                                            nameof(chain));
            }

            // get device leaf cert with key
            this.deviceLeafCertWithKey = this.deviceLeafCert
                                                .CloneWithPrivateKey(
                                                    this.deviceLeafPrivKey);
            // root
            chain[chain.Count - 1]?.StoreCertIfNotExist(StoreName.Root, 
                                              StoreLocation.CurrentUser);

            // intermed
            chain.OfType<X509Certificate2>()
                 .SkipLast(1).StoreCertsIfNotExist(
                                    StoreName.CertificateAuthority, 
                                    StoreLocation.CurrentUser, 
                                    true);

            // leaf
            this.deviceLeafCertWithKey.StoreCertIfNotExist( 
                                    StoreName.My, 
                                    StoreLocation.CurrentUser, 
                                    true);
        }

        public T ExecPerformingCryptoOps<T>(Func<X509Certificate2, T> func)
        {
            // external functions would never gain access to the private key
            // handle nor be executed within an HSM like in here
            return func(this.deviceLeafCertWithKey);
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
                this.deviceLeafPrivKey?.Dispose(); 
                this.deviceLeafCert?.Dispose();
                this.deviceLeafCertWithKey?.Dispose();
                this.deviceLeafPubKey?.Dispose();
            }  
        }
    }
}