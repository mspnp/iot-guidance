// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Security.Cryptography.X509Certificates;

namespace Fabrikam.IoT.Devices.SupplyChain.Actors.Security
{
    /// <summary>
    /// represents a Root Certificate Authority that self sing its own certificates
    /// </summary>
    public interface IRootCertificateAuthority: ICertificateAuthority
    {
        /// <summary>
        /// as a Root CA one possible option is to sign its own X509 certificate
        /// </summary>
        /// <remarks>
        /// In production it is strongly recommended to acquire CA certificate
        /// signed by well known public Certificate Authority.
        /// </remarks>
        /// <returns>a self signed X509 Certificate</returns>
        X509Certificate2 AcquireSelfSignedCrt();
    }
}