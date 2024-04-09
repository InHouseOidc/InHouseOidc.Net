// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Common.Constant
{
    public static class TokenEndpointConstant
    {
        public const string AuthorizationCode = "authorization_code";
        public const string ClientCredentials = "client_credentials";
        public const string ClientId = "client_id";
        public const string ClientSecret = "client_secret";
        public const string Code = "code";
        public const string CodeVerifier = "code_verifier";
        public const string GrantType = "grant_type";
        public const string OfflineAccess = "offline_access";
        public const string RedirectUri = "redirect_uri";
        public const string RefreshToken = "refresh_token";
        public const string Scope = "scope";

        public static readonly List<string> AuthorizationCodeValidFields =
            new([ClientId, ClientSecret, Code, CodeVerifier, GrantType, RedirectUri, Scope]);
        public static readonly List<string> ClientCredentialsValidFields =
            new([ClientId, ClientSecret, GrantType, Scope]);
        public static readonly List<string> RefreshTokenValidFields = new([ClientId, GrantType, RefreshToken, Scope]);
    }
}
