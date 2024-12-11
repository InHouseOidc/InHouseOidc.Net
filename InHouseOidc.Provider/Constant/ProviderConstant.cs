// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Provider.Constant
{
    internal static class ProviderConstant
    {
        public const string AccessDenied = "access_denied";
        public const string AuthenticationCookieName = "InHouseOidc";
        public const string AuthenticationSchemeCookie = "InHouseOidc.CookieScheme";
        public const string AuthenticationSchemeProvider = "InHouseOidc.ProviderScheme";
        public const string Authorization = "Authorization";
        public const string AuthorizationPath = "/connect/authorize";
        public const string Basic = "Basic ";
        public const string CheckSessionCookieName = "InHouseOidc.CheckSession";
        public const string CheckSessionPath = "/connect/checksession";
        public const string DiscoveryPath = "/.well-known/openid-configuration";
        public const string EndSessionPath = "/connect/endsession";
        public const string ErrorPath = "/error";
        public const string IdentityProvider = "internal";
        public const string InvalidClient = "invalid_client";
        public const string InvalidContentType = "invalid_content_type";
        public const string InvalidGrant = "invalid_grant";
        public const string InvalidHttpMethod = "invalid_http_method";
        public const string InvalidScope = "invalid_scope";
        public const string InvalidRequest = "invalid_request";
        public const string InvalidToken = "invalid_token";
        public const string JsonWebKeysPath = "/.well-known/jwks";
        public const string LoginPath = "/login";
        public const string LogoutPath = "/logout";
        public const string LoginRequired = "login_required";
        public const string RequestNotSupported = "request_not_supported";
        public const string ServerError = "server_error";
        public const string TemporarilyUnavailable = "temporarily_unavailable";
        public const string TokenPath = "/connect/token";
        public const string UnauthorizedClient = "unauthorized_client";
        public const string UnsupportedGrantType = "unsupported_grant_type";
        public const string UnsupportedResponseType = "unsupported_response_type";
        public const string UserInfoPath = "/connect/userinfo";
    }
}
