// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Provider.Exception;
using InHouseOidc.Provider.Type;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;

namespace InHouseOidc.Provider.Extension
{
    internal static class ListSigningKeyExtension
    {
        public static List<SigningKey> Resolve(this List<SigningKey> listSigningKey, IServiceProvider serviceProvider)
        {
            if (!listSigningKey.Any())
            {
                lock (listSigningKey)
                {
                    if (!listSigningKey.Any())
                    {
                        var certificateStore = serviceProvider.GetService<ICertificateStore>();
                        if (certificateStore != null)
                        {
                            listSigningKey.StoreSigningKeys(
                                certificateStore.GetSigningCertificates().GetAwaiter().GetResult()
                            );
                        }
                        if (!listSigningKey.Any())
                        {
                            throw new InternalErrorException(
                                "No signing keys available.  Set via ProviderBuilder.SetSigningCertificates"
                                    + " or implement ICertificateStore.GetSigningCertificates"
                            );
                        }
                    }
                }
            }
            return listSigningKey;
        }

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
