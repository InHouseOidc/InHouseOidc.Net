// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Bff
{
    public class BffConstant
    {
        public const string AuthenticationCookieName = "InHouseOidc.Bff";
        public const string AuthenticationSchemeBff = "InHouseOidc.BffScheme";
        public const string AuthenticationSchemeCookie = "InHouseOidc.BffCookieScheme";
        public const string AuthenticationSchemeMultiTenant = "InHouseOidc.MultiTenantScheme";
        public const string BffApiPolicy = "InHouseOidc.BffApi";
        public const string CallbackPath = "/api/auth/callback";
        public const string ExpiresAt = "expires_at";
        public const string LoginPath = "/api/auth/login";
        public const string LogoutPath = "/api/auth/logout";
        public const string SignedOutCallbackPath = "/api/auth/signout-callback";
        public const string UserInfoPath = "/api/auth/user-info";
    }
}
