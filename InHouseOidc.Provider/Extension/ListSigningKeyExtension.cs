// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Provider.Exception;
using InHouseOidc.Provider.Type;
using Microsoft.IdentityModel.Tokens;

namespace InHouseOidc.Provider.Extension
{
    internal static class ListSigningKeyExtension
    {
        public static void StoreSigningKeys(
            this List<SigningKey> listSigningKey,
            IEnumerable<X509Certificate2> x509Certificate2s
        )
        {
            foreach (var x509Certificate2 in x509Certificate2s)
            {
                if (!x509Certificate2.HasPrivateKey)
                {
                    throw new InternalErrorException("X509Certificate2 must include a private key");
                }
                var x509SecurityKey = new X509SecurityKey(x509Certificate2);
                if (!x509SecurityKey.IsSupportedAlgorithm(SecurityAlgorithms.RsaSha256))
                {
                    throw new InternalErrorException("X509Certificate2 must support RS256 algorithm");
                }
                listSigningKey.Add(
                    new SigningCredentials(x509SecurityKey, SecurityAlgorithms.RsaSha256).ToSigningKey()
                );
            }
        }
    }
}
