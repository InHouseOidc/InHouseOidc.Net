﻿// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Provider;

namespace InHouseOidc.Example.Provider
{
    public class ClientStore : IClientStore
    {
        private readonly Dictionary<string, (OidcClient OidcClient, string? ClientSecret)> clients =
            new()
            {
                {
                    "bffexample",
                    (
                        new OidcClient
                        {
                            AccessTokenExpiry = TimeSpan.FromMinutes(15),
                            ClientId = "bffexample",
                            ClientSecretRequired = true,
                            GrantTypes = [GrantType.AuthorizationCode, GrantType.RefreshToken],
                            RedirectUris = ["http://localhost:5105", "http://localhost:5105/api/auth/callback",],
                            RedirectUrisPostLogout =
                            [
                                "http://localhost:5105",
                                "http://localhost:5105/api/auth/signout-callback",
                            ],
                            Scopes =
                            [
                                "openid",
                                "offline_access",
                                "email",
                                "phone",
                                "profile",
                                "role",
                                "exampleapiscope",
                                "exampleproviderapiscope",
                            ],
                        },
                        "topsecret"
                    )
                },
                {
                    "clientcredentialsexample",
                    (
                        new OidcClient
                        {
                            AccessTokenExpiry = TimeSpan.FromMinutes(15),
                            ClientId = "clientcredentialsexample",
                            ClientSecretRequired = true,
                            GrantTypes = [GrantType.ClientCredentials],
                            Scopes = ["exampleapiscope"],
                        },
                        "topsecret"
                    )
                },
                {
                    "mvcexample",
                    (
                        new OidcClient
                        {
                            AccessTokenExpiry = TimeSpan.FromMinutes(15),
                            ClientId = "mvcexample",
                            ClientSecretRequired = true,
                            GrantTypes = [GrantType.AuthorizationCode, GrantType.RefreshToken],
                            RedirectUris =
                            [
                                "http://localhost:5103",
                                "http://localhost:5103/connect/authorize/callback",
                            ],
                            RedirectUrisPostLogout =
                            [
                                "http://localhost:5103",
                                "http://localhost:5103/signout-callback-oidc",
                            ],
                            Scopes =
                            [
                                "openid",
                                "offline_access",
                                "email",
                                "phone",
                                "profile",
                                "role",
                                "exampleapiscope",
                                "exampleproviderapiscope",
                            ],
                        },
                        "topsecret"
                    )
                },
                {
                    "providerexample",
                    (
                        new OidcClient
                        {
                            AccessTokenExpiry = TimeSpan.FromMinutes(15),
                            ClientId = "providerexample",
                            ClientSecretRequired = true,
                            GrantTypes = [GrantType.AuthorizationCode, GrantType.RefreshToken],
                            RedirectUris =
                            [
                                "http://localhost:5100",
                                "http://localhost:5100/connect/authorize/callback",
                            ],
                            RedirectUrisPostLogout =
                            [
                                "http://localhost:5100",
                                "http://localhost:5100/signout-callback-oidc",
                            ],
                            Scopes =
                            [
                                "openid",
                                "offline_access",
                                "email",
                                "phone",
                                "profile",
                                "role",
                                "exampleapiscope"
                            ],
                        },
                        "topsecret"
                    )
                },
                {
                    "razorexample",
                    (
                        new OidcClient
                        {
                            AccessTokenExpiry = TimeSpan.FromMinutes(15),
                            ClientId = "razorexample",
                            ClientSecretRequired = true,
                            GrantTypes = [GrantType.AuthorizationCode, GrantType.RefreshToken],
                            RedirectUris =
                            [
                                "http://localhost:5101",
                                "http://localhost:5101/connect/authorize/callback",
                            ],
                            RedirectUrisPostLogout =
                            [
                                "http://localhost:5101",
                                "http://localhost:5101/signout-callback-oidc",
                            ],
                            Scopes =
                            [
                                "openid",
                                "offline_access",
                                "email",
                                "phone",
                                "profile",
                                "role",
                                "exampleapiscope",
                                "exampleproviderapiscope",
                            ],
                        },
                        "topsecret"
                    )
                },
            };

        public Task<OidcClient?> GetClient(string clientId)
        {
            if (this.clients.TryGetValue(clientId, out var client))
            {
                return Task.FromResult<OidcClient?>(client.OidcClient);
            }
            return Task.FromResult<OidcClient?>(null);
        }

        public Task<bool> IsCorrectClientSecret(string clientId, string checkClientSecretRaw)
        {
            if (!this.clients.TryGetValue(clientId, out var client))
            {
                return Task.FromResult(false);
            }
            return Task.FromResult(client.ClientSecret == checkClientSecretRaw);
        }

        public Task<bool> IsKnownPostLogoutRedirectUri(string postLogoutRedirectUri)
        {
            foreach (var client in this.clients.Values)
            {
                if (
                    client.OidcClient.RedirectUrisPostLogout != null
                    && client.OidcClient.RedirectUrisPostLogout.Contains(postLogoutRedirectUri)
                )
                {
                    return Task.FromResult(true);
                }
            }
            return Task.FromResult(false);
        }
    }
}
