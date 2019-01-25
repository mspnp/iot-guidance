// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Fabrikam.IoT.Devices.SupplyChain.Actors.Security.Models
{
    public interface IDeviceHSM: IDisposable
    {
        string GetUniqueDeviceId { get; }

        X509Certificate2 DeviceLeafCert{get;}

        RSA GetPublicKey {get;}

        void StoreX509Cert(X509Certificate2 leafCrt, X509Certificate2Collection chain);

        T ExecPerformingCryptoOps<T>(Func<X509Certificate2, T> func);
    }
}