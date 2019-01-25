// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Security.Cryptography.X509Certificates;

namespace Fabrikam.IoT.Devices.SupplyChain.Actors.Security
{
    /// <summary>
    /// represents a Certificate Authority that request another root CA to sign its certificate
    /// </summary>
    public interface ICertificateAuthority : IDisposable
    {
        #region API
        /// <summary>
        /// as a CA an entity can accept Certificate Signing Requests 
        /// to generate and sign leaf X509 Certificates
        /// </summary>
        /// <param name="csr">the Certificate Signing Request</param>
        /// <returns>a signed X509 certificate by the CA</returns>
        X509Certificate2 CreateSignedCrt(CertificateRequest csr);

        /// <summary>
        /// X509 Certificate an entity would general acquire thought a parent CA
        /// </summary>
        X509Certificate2 personalSignedX509Certificate {get;}
        #endregion

        /// <summary>
        /// the root CA that an intermediate entity could request to sign its 
        /// personal X509 certificates
        /// </summary>
        ICertificateAuthority ParentCertificateAuthority {get;}
    }
}