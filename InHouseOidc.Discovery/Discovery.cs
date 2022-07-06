// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Discovery
{
    public class Discovery
    {
        public Discovery(
            string? authorizationEndpoint,
            string? endSessionEndpoint,
            DateTimeOffset expiryUtc,
            List<string> grantTypesSupported,
            string issuer,
            string? tokenEndpoint,
            List<string> tokenEndpointAuthMethodsSupported
        )
        {
            this.AuthorizationEndpoint = authorizationEndpoint;
            this.EndSessionEndpoint = endSessionEndpoint;
            this.ExpiryUtc = expiryUtc;
            this.GrantTypesSupported = grantTypesSupported;
            this.Issuer = issuer;
            this.TokenEndpoint = tokenEndpoint;
            this.TokenEndpointAuthMethodsSupported = tokenEndpointAuthMethodsSupported;
        }

        public string? AuthorizationEndpoint { get; private set; }
        public string? EndSessionEndpoint { get; private set; }
        public DateTimeOffset ExpiryUtc { get; private set; }
        public List<string> GrantTypesSupported { get; private set; }
        public string Issuer { get; private set; }
        public string? TokenEndpoint { get; private set; }
        public List<string> TokenEndpointAuthMethodsSupported { get; private set; }
    }
}
