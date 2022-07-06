// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Provider.Constant;
using InHouseOidc.Provider.Type;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace InHouseOidc.Provider.Extension
{
    internal static class SigningCredentialsExtension
    {
        public static SigningKey ToSigningKey(this SigningCredentials signingCredentials)
        {
            if (signingCredentials.Key is not X509SecurityKey x509SecurityKey)
            {
                throw new ArgumentException("Signing credentials must use X509SecurityKey", nameof(signingCredentials));
            }
            // Extract key properties required to generate a JSON Web Key
            if (x509SecurityKey.PublicKey is not RSA rsaSecurityKey)
            {
                throw new ArgumentException("Signing credentials must use RSA public key");
            }
            var parameters = rsaSecurityKey.ExportParameters(false);
            var jsonWebKey = new JsonWebKey
            {
                Alg = signingCredentials.Algorithm,
                E = Base64UrlEncoder.Encode(parameters.Exponent),
                Kid = signingCredentials.Kid,
                Kty = JsonWebKeySetConstant.RSA,
                N = Base64UrlEncoder.Encode(parameters.Modulus),
                Use = JsonWebKeyUseNames.Sig,
            };
            var certificate = x509SecurityKey.Certificate;
            jsonWebKey.X5c.Add(Convert.ToBase64String(certificate.RawData));
            var certificateHash = certificate.GetCertHash();
            jsonWebKey.X5t = Base64UrlEncoder.Encode(certificateHash);
            return new SigningKey(
                certificate,
                certificate.GetCertHash(),
                jsonWebKey,
                certificate.NotAfter.ToUniversalTime(),
                certificate.NotBefore.ToUniversalTime(),
                signingCredentials,
                x509SecurityKey
            );
        }
    }
}
