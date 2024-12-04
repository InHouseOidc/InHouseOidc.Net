// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Discovery
{
    public class Discovery(
        string? authorizationEndpoint,
        string? checkSessionEndpoint,
        string? endSessionEndpoint,
        DateTimeOffset expiryUtc,
        List<string> grantTypesSupported,
        string issuer,
        string? tokenEndpoint,
        List<string> tokenEndpointAuthMethodsSupported
    )
    {
        public string? AuthorizationEndpoint { get; private set; } = authorizationEndpoint;
        public string? CheckSessionEndpoint { get; private set; } = checkSessionEndpoint;
        public string? EndSessionEndpoint { get; private set; } = endSessionEndpoint;
        public DateTimeOffset ExpiryUtc { get; private set; } = expiryUtc;
        public List<string> GrantTypesSupported { get; private set; } = grantTypesSupported;
        public string Issuer { get; private set; } = issuer;
        public string? TokenEndpoint { get; private set; } = tokenEndpoint;
        public List<string> TokenEndpointAuthMethodsSupported { get; private set; } = tokenEndpointAuthMethodsSupported;
    }
}
