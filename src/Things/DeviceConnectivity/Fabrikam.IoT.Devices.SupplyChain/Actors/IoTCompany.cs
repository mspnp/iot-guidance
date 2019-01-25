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
using Fabrikam.IoT.Devices.SupplyChain.Services;
using Microsoft.Azure.Devices.Provisioning.Service;

namespace Fabrikam.IoT.Devices.SupplyChain.Actors
{
    /// <summary>
    /// IoT contract-manufacturing company role and also act as a Root CA 
    /// </summary>
    public class IoTCompany: IIoTCompany, IRootCertificateAuthority
    {

        private readonly X509Certificate2 rootCASelfSignedCertWithKey = null;
        private readonly X509Certificate2 rootCASelfSignedCert = null;
        private readonly IConfig config = null;
        private readonly IProvisioningService service;
        private bool ready = false;

        // TODO: future PR if team says "go", add this as single instance DependencyResolution
        public IoTCompany(IConfig config,
                          IProvisioningService service,
                          IIoTHardwareIntegrator hardwareIntegrator = null)
        {
            this.config = config ?? 
                                throw new ArgumentNullException(nameof(config));
            this.service = service ??
                                throw new ArgumentNullException(nameof(service));

            this.rootCASelfSignedCertWithKey = CreateRootCASelfSignedCertWithKey();
            this.rootCASelfSignedCert = this.rootCASelfSignedCertWithKey
                                            .CloneWithoutKey();
            this.HardwareIntegrator = hardwareIntegrator ?? 
                                        new IoTHardwareIntegrator(config, this);
        }

        public string Name => this.config.IoTCompanyName;

        public ICertificateAuthority ParentCertificateAuthority => null;

        public X509Certificate2 personalSignedX509Certificate 
                                    => this.rootCASelfSignedCert;

        // e.g. "CN=Fabrikam Contract-Manufacting Company Root CA Certificate"
        private string RootCADistinguishedName => $"CN={this.Name} Root CA Certificate (Test Use Only), O=Fabrikam Drone Delivery";

        private  string DPSGlobalEndpoint => this.config.AzureDPSGlobalEndpoint;

        private string DPSScopeId => this.config.AzureDPSScopeId;

        private IIoTHardwareIntegrator HardwareIntegrator { get; set; }

        public X509Certificate2 CreateSignedCrt(CertificateRequest csr)
        {
            if (csr == null)
            {
                throw new ArgumentNullException(nameof(csr));
            }

            X509Certificate2 intermedCert = csr
                                            .CreateX509Cert(
                                                this.rootCASelfSignedCertWithKey, 
                                                1504);

            return intermedCert;
        }

        public X509Certificate2 AcquireSelfSignedCrt() =>
                                    this.AcquireCASignedCrt();

        public X509Certificate2 AcquireCASignedCrt() =>
                                            this.CreateRootCASelfSignedCertWithKey();

        public async Task CleanUpAndCreateEnrollmentGroupAsync()
        {
            Attestation attestation = X509Attestation
                                        .CreateFromRootCertificates(
                                            this.personalSignedX509Certificate);

           this.ready = await this.service
                                    .CleanUpAndCreateEnrollmentGroupAsync(
                                                attestation)
                                    .ConfigureAwait(false);
        }

        public async Task<IIoTDevice> MakeDeviceAsync()
        {
            if (!this.ready)
            {
                throw new InvalidOperationException($"The company {this.Name} is not ready yet. Try to create an Enrollment Group before continuing.");
            }

            var device = await HardwareIntegrator
                                        .ManufactureAsync(
                                            DPSGlobalEndpoint,
                                            DPSScopeId)
                                        .ConfigureAwait(false);

            return device;
        }

        public async Task GenerateProofOfVerficationAsync(
                                    string verificationCode,
                                    string fileName = null)
        {
            fileName = fileName ?? $".\\{verificationCode}.cer";

            using (AsymmetricAlgorithm popPrivKey = RSA.Create(1536))
            {
                // generate csr for Azure DPS PoP
                CertificateRequest proofCsr =
                    X509CertificateOperations.CreateChainRequest(
                                        verificationCode,
                                        popPrivKey,
                                        HashAlgorithmName.SHA256,
                                        false, null);

                // generate the proof
                X509Certificate2 popCert = this.CreateSignedCrt(proofCsr);

                // save to .cer so tje private key owner can proof the possesion by uploading the new signed cert
                await popCert.ExportToCerAsync(fileName).ConfigureAwait(false);
            }
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
                this.rootCASelfSignedCertWithKey?.Dispose(); 
            }  
        }

        private X509Certificate2 CreateRootCASelfSignedCertWithKey()
        {
            X509Certificate2 rootCACertWithKey = null;

            // Well kwnon CA cert. It belongs to the CA API, it should just sign the very first intermediate cert
            using (AsymmetricAlgorithm rootKeyCAPrivKey = RSA.Create(3072))
            {
                // Create CSR
                CertificateRequest rootKeyCARequest = X509CertificateOperations
                                            .CreateChainRequest(
                                                this.RootCADistinguishedName,
                                                rootKeyCAPrivKey,
                                                HashAlgorithmName.SHA512,
                                                true, null);

                rootCACertWithKey = rootKeyCARequest
                                        .CreateX509SelfSignedCert(
                                            10000);
            }

            return rootCACertWithKey;
        }
    }
}