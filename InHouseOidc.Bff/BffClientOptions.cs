// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Bff
{
    /// <summary>
    /// Specifies the options that configure a BFF OIDC client.
    /// </summary>
    public class BffClientOptions
    {
        /// <summary>
        /// The client identifier registered at the OIDC Provider. Required.
        /// </summary>
        required public string ClientId { get; init; }

        /// <summary>
        /// The client secret registered at the OIDC Provider. Optional.
        /// </summary>
        required public string ClientSecret { get; init; }

        /// <summary>
        /// The base address (in full URL form) for the OIDC Provider.  Required.
        /// </summary>
        required public string OidcProviderAddress { get; init; }

        /// <summary>
        /// The scope (single) or scopes (space separated) requested to be included in the access token.  Optional.
        /// </summary>
        public string? Scope { get; init; }
    }
}
