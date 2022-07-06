// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Common.Constant
{
    public static class DiscoveryConstant
    {
        public const string AuthorizationEndpoint = "authorization_endpoint";
        public const string ClaimsSupported = "claims_supported";
        public const string ClientSecretBasic = "client_secret_basic";
        public const string ClientSecretPost = "client_secret_post";
        public const string CheckSessionEndpoint = "check_session_iframe";
        public const string CodeChallengeMethodsSupported = "code_challenge_methods_supported";
        public const string DefaultInternalHttpClientName = "InHouseOidc.HttpClient";
        public const string EndSessionEndpoint = "end_session_endpoint";
        public const string GrantTypesSupported = "grant_types_supported";
        public const string IdTokenSigningAlgValuesSupported = "id_token_signing_alg_values_supported";
        public const string Issuer = "issuer";
        public const string JsonWebKeySetUri = "jwks_uri";
        public const string PromptLogin = "login";
        public const string PromptNone = "none";
        public const string Public = "public";
        public const string RequestParameterSupported = "request_parameter_supported";
        public const string ResponseModeFormPost = "form_post";
        public const string ResponseModeQuery = "query";
        public const string ResponseModesSupported = "response_modes_supported";
        public const string ResponseTypeCode = "code";
        public const string ResponseTypesSupported = "response_types_supported";
        public const string RS256 = "RS256";
        public const string S256 = "S256";
        public const string ScopesSupported = "scopes_supported";
        public const string SubjectTypesSupported = "subject_types_supported";
        public const string TokenEndpoint = "token_endpoint";
        public const string TokenEndpointAuthMethodsSupported = "token_endpoint_auth_methods_supported";
        public const string UserInfoEndpoint = "userinfo_endpoint";
    }
}
