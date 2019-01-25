// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Fabrikam.IoT.Devices.SupplyChain.Security.X509Certificates
{
    /// <summary>
    /// a helper that interacts with System.Security.Cryptography to generate csr(s), 
    /// create, clone and store certs
    /// </summary>
    public static class X509CertificateOperations
    {
        #region X509 Certificate Request Operations
        private static CertificateRequest OpenCertRequest(
            string dn,
            AsymmetricAlgorithm key,
            HashAlgorithmName hashAlgorithm)
        {

            if (key is RSA rsa)
                return new CertificateRequest(dn, rsa, hashAlgorithm, RSASignaturePadding.Pkcs1);

            if (key is ECDsa ecdsa)
                return new CertificateRequest(dn, ecdsa, hashAlgorithm);

            throw new InvalidOperationException(
                $"Had no handler for key of type {key?.GetType().FullName ?? "null"}");
        }

        public static CertificateRequest CreateChainRequest(
            string dn,
            AsymmetricAlgorithm key,
            HashAlgorithmName hashAlgorithm,
            bool isCa,
            int? pathLen)
        {
            const X509KeyUsageFlags CAFlags = X509KeyUsageFlags.CrlSign |
                                              X509KeyUsageFlags.KeyCertSign;
            const X509KeyUsageFlags EEFlags =
                X509KeyUsageFlags.DataEncipherment |
                X509KeyUsageFlags.KeyEncipherment |
                X509KeyUsageFlags.DigitalSignature |
                X509KeyUsageFlags.NonRepudiation;

            CertificateRequest request = OpenCertRequest(dn, key, hashAlgorithm);

            request.CertificateExtensions.Add(
               new X509EnhancedKeyUsageExtension(
                       // client authN
                       new OidCollection {new Oid("1.3.6.1.5.5.7.3.2")},
                       false));

            request.CertificateExtensions.Add(
                new X509SubjectKeyIdentifierExtension(
                    request.PublicKey,
                    X509SubjectKeyIdentifierHashAlgorithm.Sha1,
                    false));

            // whether it is leaf or not
            request.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    isCa ? CAFlags : EEFlags,
                    true));

            request.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(
                    isCa,
                    pathLen.HasValue,
                    pathLen.GetValueOrDefault(),
                    true));

            return request;
        }
        #endregion

        #region X509 Certificate2 Extensions
        public static X509Certificate2 CreateX509Cert(this CertificateRequest csr,
                                                    X509Certificate2 caSigner,
                                                    int validityInDays)
        {
            DateTimeOffset now = caSigner.NotBefore;
            DateTimeOffset validity = now.AddDays(validityInDays);

            byte[] serial = new byte[10];
            serial[1] = 1;

            // Generate Serial Number
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(serial, 2, serial.Length - 2);
            }

            // Create and return the Certificate
            var cert = csr.Create(issuerCertificate: caSigner,
                              notBefore: now,
                              notAfter: validity,
                              serialNumber: serial);
            return cert;
        }

        public static X509Certificate2 CreateX509SelfSignedCert(
                                            this CertificateRequest csr,
                                            int validityInDays)
        {

            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset rootEnd = now.AddDays(validityInDays);

            var certSelfSigned = csr.CreateSelfSigned(
                                                    notBefore: now,
                                                    notAfter: rootEnd);

            return certSelfSigned;
        }

        public static X509Certificate2 CloneWithPrivateKey(
                                            this X509Certificate2 cert,
                                            AsymmetricAlgorithm key)
        {
            if (key is RSA rsa)
                return cert.CopyWithPrivateKey(rsa);

            if (key is ECDsa ecdsa)
                return cert.CopyWithPrivateKey(ecdsa);

            throw new InvalidOperationException(
    $"Had no handler for key of type {key?.GetType().FullName ?? "null"}");
        }

        public static X509Certificate2 CloneWithoutKey(
                                            this X509Certificate2 certWithKey)
        {
            var cert = new X509Certificate2(
                                    certWithKey.Export(X509ContentType.Cert));
            
            return cert;
        }

        public async static Task ExportToCerAsync(this X509Certificate2 cert,
                                       string fileName)
        {
            var builderCer = new StringBuilder();

            builderCer.AppendLine("-----BEGIN CERTIFICATE-----");
            builderCer.AppendLine(
                Convert.ToBase64String(cert.Export(X509ContentType.Cert),
                                       Base64FormattingOptions.InsertLineBreaks));
            builderCer.AppendLine("-----END CERTIFICATE-----");

            await File.WriteAllTextAsync(fileName, builderCer.ToString())
                                                        .ConfigureAwait(false);
        }
        #endregion

        #region X509 Store
        public static void RemoveCertsByOrganizationName(StoreName storeName,
                                                    StoreLocation storeLocation,
                                                    string organizationName)
        {
            string o = $"O={organizationName}";
            using (var store = new X509Store(storeName, storeLocation))
            {
                store.Open(OpenFlags.ReadWrite);

                var col = store
                          .Certificates
                          .Find(
                              X509FindType.FindByApplicationPolicy,
                              "1.3.6.1.5.5.7.3.2", 
                              false);

                foreach (var cert in col)
                {
                    if (cert.Subject.Contains(o))
                        store.Remove(cert);
                }

                store.Close();
            }
        }

        public static X509Certificate2Collection FindCertBySubjectName(
                                                    this X509Certificate2 cert,
                                                    StoreName storeName,
                                                    StoreLocation storeLocation)
        {
            return FindCertBySubjectName(storeName,
                                        storeLocation,
                                        cert.Subject);
        }

        public static X509Certificate2Collection FindCertBySubjectName(
                                                    StoreName storeName,
                                                    StoreLocation storeLocation,
                                                    string dnSubjectName)
        {
            X509Certificate2Collection col = null;

            using (var store = new X509Store(storeName, storeLocation))
            {
                store.Open(OpenFlags.ReadOnly);

                col = store.Certificates.Find(
                X509FindType.FindBySubjectDistinguishedName,
                dnSubjectName, false);

                store.Close();
            }

            return col;
        }

        public static X509Certificate2 PrepareForStoringWithKeys(this X509Certificate2 cert)
        {

            return new X509Certificate2(
                            cert.Export(X509ContentType.Pkcs12),
                            (string)null,
                            X509KeyStorageFlags.MachineKeySet |
                            X509KeyStorageFlags.PersistKeySet);
        }

        public static void StoreCertIfNotExist(this X509Certificate2 cert,
                                    StoreName storeName,
                                    StoreLocation storeLocation,
                                    bool storeWithKeys = false)
        {
            StoreCertsIfNotExist(new X509Certificate2Collection(cert),
                                  storeName, storeLocation, storeWithKeys);
        }

        public static void StoreCertsIfNotExist(this IEnumerable<X509Certificate2> certs,
                                    StoreName storeName,
                                    StoreLocation storeLocation,
                                    bool storeWithKeys = false)
        {
            StoreCertsIfNotExist(new X509Certificate2Collection(certs.ToArray()),
                                                      storeName,
                                                      storeLocation,
                                                      storeWithKeys);
        }

        public static void StoreCertsIfNotExist(this X509Certificate2Collection certs,
                                    StoreName storeName,
                                    StoreLocation storeLocation,
                                    bool storeWithKeys = false)
        {
            using (var store = new X509Store(storeName, storeLocation))
            {
                store.Open(OpenFlags.ReadWrite);

                foreach (var cert in certs)
                {
                    var col = store.Certificates.Find(
                        X509FindType.FindBySubjectDistinguishedName,
                        cert.Subject, false);

                    if (col?.Count == 0)
                    {
                        X509Certificate2 certReadyToStore = null;
                        if (storeWithKeys)
                        {
                            certReadyToStore = cert.PrepareForStoringWithKeys();
                        }

                        certReadyToStore = certReadyToStore ?? cert;

                        store.Add(certReadyToStore);
                    }
                }

                store.Close();
            }
        }
        #endregion
    }
}