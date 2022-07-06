// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Provider;

namespace InHouseOidc.Example.Provider
{
    public class ClientStore : IClientStore
    {
        private readonly Dictionary<string, OidcClient> clients =
            new()
            {
                {
                    "clientcredentialsexample",
                    new OidcClient
                    {
                        AccessTokenExpiry = TimeSpan.FromMinutes(15),
                        ClientId = "clientcredentialsexample",
                        ClientSecret = "topsecret",
                        GrantTypes = new() { GrantType.ClientCredentials },
                        Scopes = new() { "exampleapiscope" },
                    }
                },
                {
                    "mvcexample",
                    new OidcClient
                    {
                        AccessTokenExpiry = TimeSpan.FromMinutes(15),
                        ClientId = "mvcexample",
                        GrantTypes = new() { GrantType.AuthorizationCode, GrantType.RefreshToken },
                        IdentityTokenExpiry = TimeSpan.FromMinutes(60),
                        RedirectUris = new()
                        {
                            "http://localhost:5103",
                            "http://localhost:5103/connect/authorize/callback",
                        },
                        RedirectUrisPostLogout = new()
                        {
                            "http://localhost:5103",
                            "http://localhost:5103/signout-callback-oidc",
                        },
                        Scopes = new()
                        {
                            "openid",
                            "offline_access",
                            "email",
                            "phone",
                            "profile",
                            "role",
                            "exampleapiscope",
                            "exampleproviderapiscope",
                        },
                    }
                },
                {
                    "providerexample",
                    new OidcClient
                    {
                        AccessTokenExpiry = TimeSpan.FromMinutes(15),
                        ClientId = "providerexample",
                        GrantTypes = new() { GrantType.AuthorizationCode },
                        IdentityTokenExpiry = TimeSpan.FromMinutes(60),
                        RedirectUris = new()
                        {
                            "http://localhost:5100",
                            "http://localhost:5100/connect/authorize/callback",
                        },
                        RedirectUrisPostLogout = new()
                        {
                            "http://localhost:5100",
                            "http://localhost:5100/signout-callback-oidc",
                        },
                        Scopes = new() { "openid", "email", "phone", "profile", "role", "exampleapiscope" },
                    }
                },
                {
                    "razorexample",
                    new OidcClient
                    {
                        AccessTokenExpiry = TimeSpan.FromMinutes(15),
                        ClientId = "razorexample",
                        GrantTypes = new() { GrantType.AuthorizationCode, GrantType.RefreshToken },
                        IdentityTokenExpiry = TimeSpan.FromMinutes(60),
                        RedirectUris = new()
                        {
                            "http://localhost:5101",
                            "http://localhost:5101/connect/authorize/callback",
                        },
                        RedirectUrisPostLogout = new()
                        {
                            "http://localhost:5101",
                            "http://localhost:5101/signout-callback-oidc",
                        },
                        Scopes = new()
                        {
                            "openid",
                            "offline_access",
                            "email",
                            "phone",
                            "profile",
                            "role",
                            "exampleapiscope",
                            "exampleproviderapiscope",
                        },
                    }
                },
            };

        public Task<OidcClient?> GetClient(string clientId)
        {
            if (this.clients.TryGetValue(clientId, out var client))
            {
                return Task.FromResult<OidcClient?>(client);
            }
            return Task.FromResult<OidcClient?>(null);
        }

        public Task<bool> IsCorrectClientSecret(string clientSecretHashed, string checkClientSecretRaw)
        {
            return Task.FromResult(clientSecretHashed == checkClientSecretRaw);
        }

        public Task<bool> IsKnownPostLogoutRedirectUri(string postLogoutRedirectUri)
        {
            foreach (var client in this.clients.Values)
            {
                if (
                    client.RedirectUrisPostLogout != null
                    && client.RedirectUrisPostLogout.Contains(postLogoutRedirectUri)
                )
                {
                    return Task.FromResult(true);
                }
            }
            return Task.FromResult(false);
        }
    }
}
