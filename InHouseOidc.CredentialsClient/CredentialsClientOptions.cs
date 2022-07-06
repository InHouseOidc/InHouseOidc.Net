// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.CredentialsClient
{
    /// <summary>
    /// Specifies the options that configure a client credentials client.
    /// </summary>
    public class CredentialsClientOptions
    {
        /// <summary>
        /// Gets the client identifier registered at the OIDC Provider.  Required.
        /// </summary>
        public string? ClientId { get; init; }

        /// <summary>
        /// Gets the client secret associated with the client identifier to be validated by the OIDC Provider.  Required.
        /// </summary>
        public string? ClientSecret { get; init; }

        /// <summary>
        /// Gets the base address (in full URL form) for the OIDC Provider.  Required.
        /// </summary>
        public string? OidcProviderAddress { get; init; }

        /// <summary>
        /// Gets the scope (single) or scopes (space separated) requested to be included in the access token.
        /// </summary>
        public string? Scope { get; init; }
    }
}
