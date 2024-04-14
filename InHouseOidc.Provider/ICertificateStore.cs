// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Provider
{
    /// <summary>
    /// Interface to be implemented by the provider host API to provide access to signing certificates at runtime.<br />
    /// Required unless signing certificates have been added via <see cref="ProviderBuilder.SetSigningCertificates"/>.
    /// </summary>
    public interface ICertificateStore
    {
        /// <summary>
        /// Provides access to the signing certificate(s) included in the Json Web Key Set.<br />
        /// See <see href="https://openid.net/specs/openid-connect-core-1_0.html#SigEnc"></see>.
        /// Certificate selection respects NotBefore and NotAfter certificate properties,<br />
        /// and always selects the certificate with the longest time to expiry when issuing new tokens.<br />
        /// Use multiple certificates to rollover keys by loading a replacement certificate (that has at least 24 hours
        /// of overlap with your current certificate) at least 24 hours before your current certificate expires.<br />
        /// </summary>
        /// <returns>The list of <see cref="X509Certificate2"/> to use for signing JWTs.</returns>
        Task<IEnumerable<X509Certificate2>> GetSigningCertificates();
    }
}
