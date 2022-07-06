// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;

namespace InHouseOidc.Provider.Type
{
    internal class SigningKey
    {
        public X509Certificate2 Certificate { get; init; }
        public byte[] CertificateHash { get; init; }
        public JsonWebKey JsonWebKey { get; init; }
        public DateTimeOffset NotAfter { get; init; }
        public DateTimeOffset NotBefore { get; init; }
        public SigningCredentials SigningCredentials { get; init; }
        public X509SecurityKey X509SecurityKey { get; init; }

        public SigningKey(
            X509Certificate2 certificate,
            byte[] certificateHash,
            JsonWebKey jsonWebKey,
            DateTimeOffset notAfter,
            DateTimeOffset notBefore,
            SigningCredentials signingCredentials,
            X509SecurityKey x509SecurityKey
        )
        {
            this.Certificate = certificate;
            this.CertificateHash = certificateHash;
            this.JsonWebKey = jsonWebKey;
            this.NotAfter = notAfter;
            this.NotBefore = notBefore;
            this.SigningCredentials = signingCredentials;
            this.X509SecurityKey = x509SecurityKey;
        }
    }
}
