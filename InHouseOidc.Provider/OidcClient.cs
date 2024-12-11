// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Provider
{
    /// <summary>
    /// Specifies the parameters that configure a client supported by the OIDC Provider.
    /// </summary>
    public class OidcClient
    {
        /// <summary>
        /// Gets the expiry time for access tokens issued.
        /// </summary>
        public TimeSpan AccessTokenExpiry { get; init; }

        /// <summary>
        /// Gets the unique identifier for this client.
        /// </summary>
        public string? ClientId { get; init; }

        /// <summary>
        /// Indicates that a client secret must be supplied to authenticate this client.
        /// </summary>
        public bool? ClientSecretRequired { get; init; }

        /// <summary>
        /// Gets the grant types this client supports. Required for all flows.
        /// </summary>
        public List<GrantType>? GrantTypes { get; init; }

        /// <summary>
        /// Gets the expiry time for identity tokens issued.
        /// DEPRECATED: id token expiry is sourced from session expiry.
        /// </summary>
        [Obsolete("id token expiry is sourced from session expiry (via IProviderSession.Login)")]
        [ExcludeFromCodeCoverage(Justification = "Obsolete")]
        public TimeSpan? IdentityTokenExpiry { get; init; }

        /// <summary>
        /// Gets the allowed post logout redirect URIs.  Required for the authorisation code flow.
        /// </summary>
        [ExcludeFromCodeCoverage(
            Justification = "Property is provided for IClientStore implementations to use in IsKnownPostLogoutRedirectUri, not used by published assemblies"
        )]
        public List<string>? RedirectUrisPostLogout { get; init; }

        /// <summary>
        /// Gets the allowed redirect URIs.  Required for authorisation code flow.
        /// </summary>
        public List<string>? RedirectUris { get; init; }

        /// <summary>
        /// Gets the valid scopes for this client.<br />
        /// For the client credentials flow valid resources (audiences) for scopes must be provided via IResourceSource.<br />
        /// For the authorisation code flow the "openid" scope is required.
        /// </summary>
        public List<string>? Scopes { get; init; }
    }
}
