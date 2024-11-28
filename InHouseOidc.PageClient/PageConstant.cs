// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.PageClient
{
    public class PageConstant
    {
        public const string AuthenticationCookieName = "InHouseOidc.Page";
        public const string AuthenticationSchemeCookie = "InHouseOidc.CookieScheme";
        public const string CallbackPath = "/connect/authorize/callback";
        public const string ExpiresAt = "expires_at";
    }
}
