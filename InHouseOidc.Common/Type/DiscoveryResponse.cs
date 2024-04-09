// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common.Constant;

namespace InHouseOidc.Common.Type
{
    public class DiscoveryResponse
    {
        [JsonPropertyName(DiscoveryConstant.AuthorizationEndpoint)]
        public string? AuthorizationEndpoint { get; set; }

        [JsonPropertyName(DiscoveryConstant.EndSessionEndpoint)]
        public string? EndSessionEndpoint { get; set; }

        [JsonPropertyName(DiscoveryConstant.GrantTypesSupported)]
        public List<string>? GrantTypesSupported { get; set; }

        [JsonPropertyName(DiscoveryConstant.Issuer)]
        public string? Issuer { get; set; }

        [JsonPropertyName(DiscoveryConstant.TokenEndpoint)]
        public string? TokenEndpoint { get; set; }

        [JsonPropertyName(DiscoveryConstant.TokenEndpointAuthMethodsSupported)]
        public List<string>? TokenEndpointAuthMethodsSupported { get; set; }
    }
}
