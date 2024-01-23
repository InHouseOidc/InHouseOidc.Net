// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common;
using InHouseOidc.Provider;

namespace InHouseOidc.Certify.Provider
{
    public class ClientStore : IClientStore
    {
        private readonly Dictionary<string, (OidcClient OidcClient, string? ClientSecret)> clients = new();

        public ClientStore(IConfiguration configuration)
        {
            var clients = configuration.GetSection("ClientStore").GetChildren();
            foreach (var client in clients)
            {
                var clientConfig = client.Get<ClientConfig>();
                this.clients.Add(
                    client.Key,
                    (
                        new OidcClient
                        {
                            ClientId = clientConfig?.ClientId ?? string.Empty,
                            ClientSecretRequired = !string.IsNullOrEmpty(clientConfig?.ClientSecret ?? string.Empty),
                            AccessTokenExpiry = TimeSpan.FromMinutes(clientConfig?.AccessTokenExpiryMinutes ?? 0),
                            GrantTypes =
                                clientConfig?.GrantTypes == null
                                    ? new List<GrantType>()
                                    : clientConfig.GrantTypes
                                        .Select(gt => EnumHelper.ParseEnumMember<GrantType>(gt))
                                        .ToList(),
                            IdentityTokenExpiry = TimeSpan.FromMinutes(clientConfig?.IdentityTokenExpiryMinutes ?? 0),
                            RedirectUris = clientConfig?.RedirectUris,
                            RedirectUrisPostLogout = clientConfig?.RedirectUrisPostLogout,
                            Scopes = clientConfig?.Scopes,
                        },
                        clientConfig?.ClientSecret
                    )
                );
            }
        }

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

        private class ClientConfig
        {
            public int? AccessTokenExpiryMinutes { get; set; }
            public string? ClientId { get; set; }
            public string? ClientSecret { get; set; }
            public List<string>? GrantTypes { get; set; }
            public int? IdentityTokenExpiryMinutes { get; set; }
            public List<string>? RedirectUrisPostLogout { get; set; }
            public List<string>? RedirectUris { get; set; }
            public List<string>? Scopes { get; set; }
        }
    }
}
