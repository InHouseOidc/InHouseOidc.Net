// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Provider;
using System.Security.Cryptography.X509Certificates;

namespace InHouseOidc.Example.Provider
{
    public class CertificateStore : ICertificateStore
    {
        public Task<IEnumerable<X509Certificate2>> GetSigningCertificates()
        {
            var signingCertificate = new X509Certificate2("InHouseOidcExample.pfx", "Internal");
            return Task.FromResult<IEnumerable<X509Certificate2>>(new List<X509Certificate2> { signingCertificate });
        }
    }
}
