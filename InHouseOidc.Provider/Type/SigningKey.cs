// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using Microsoft.IdentityModel.Tokens;

namespace InHouseOidc.Provider.Type
{
    internal class SigningKey(
        X509Certificate2 certificate,
        byte[] certificateHash,
        JsonWebKey jsonWebKey,
        DateTimeOffset notAfter,
        DateTimeOffset notBefore,
        SigningCredentials signingCredentials,
        X509SecurityKey x509SecurityKey
    )
    {
        public X509Certificate2 Certificate { get; init; } = certificate;
        public byte[] CertificateHash { get; init; } = certificateHash;
        public JsonWebKey JsonWebKey { get; init; } = jsonWebKey;
        public DateTimeOffset NotAfter { get; init; } = notAfter;
        public DateTimeOffset NotBefore { get; init; } = notBefore;
        public SigningCredentials SigningCredentials { get; init; } = signingCredentials;
        public X509SecurityKey X509SecurityKey { get; init; } = x509SecurityKey;
    }
}
