// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.CredentialsClient;

namespace InHouseOidc.Example.CredentialsClient
{
    internal class CredentialsStore : ICredentialsStore
    {
        public Task<CredentialsClientOptions?> GetCredentialsClientOptions(string clientName)
        {
            if (clientName != "exampleapistored")
            {
                return Task.FromResult<CredentialsClientOptions?>(null);
            }
            const string providerAddress = "http://localhost:5100";
            const string clientId = "clientcredentialsexample";
            const string clientSecret = "topsecret";
            const string scope = "exampleapiscope";
            return Task.FromResult<CredentialsClientOptions?>(
                new CredentialsClientOptions
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret,
                    Scope = scope,
                    OidcProviderAddress = providerAddress,
                }
            );
        }
    }
}
