// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Provider
{
    /// <summary>
    /// Interface implemented by the OIDC Provider to allow the provider host to request access tokens.
    /// </summary>
    public interface IProviderToken
    {
        /// <summary>
        /// Issues an access token allowing the OIDC Provider host to authenticate outgoing calls.<br />
        /// </summary>
        /// <param name="clientId">The client identifier of the.</param>
        /// <param name="expiry">The absolute time the access token is valid for.</param>
        /// <param name="issuer">The issuer of the token.</param>
        /// <param name="scopes">Scopes to include in the access token.</param>
        /// <returns>The access token.</returns>
        Task<string> GetProviderAccessToken(string clientId, TimeSpan expiry, string issuer, List<string> scopes);
    }
}
