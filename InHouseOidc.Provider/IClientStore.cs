// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Provider
{
    /// <summary>
    /// Interface to be implemented by the provider host API to provide access to client information.<br />
    /// Required for all flows.
    /// </summary>
    public interface IClientStore
    {
        /// <summary>
        /// Get the OIDC client details for a client.
        /// </summary>
        /// <param name="clientId">The client identifier.</param>
        /// <returns><see cref="OidcClient?"/> or null to indicate an inactive or unknown client.</returns>
        Task<OidcClient?> GetClient(string clientId);

        /// <summary>
        /// Validate a passed check client secret is correct based on the current client secret.
        /// </summary>
        /// <param name="clientId">The client identifier.</param>
        /// <param name="checkClientSecretRaw">The raw (clear text) client secret on the token request that needs to be verified.</param>
        /// <returns>True for client secret is correct.</returns>
        Task<bool> IsCorrectClientSecret(string clientId, string checkClientSecretRaw);

        /// <summary>
        /// Validate a passed post logout redirect URI.<br />
        /// The URI must be checked against all authorization code flow enabled clients,
        /// and against all tenants when hosting a multi-tenanted provider.
        /// </summary>
        /// <param name="postLogoutRedirectUri">The URI to check.</param>
        /// <returns>True for known URI.</returns>
        Task<bool> IsKnownPostLogoutRedirectUri(string postLogoutRedirectUri);
    }
}
