// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;

namespace InHouseOidc.PageClient
{
    public static class HttpContextExtension
    {
        public static async Task PageClientLogout(this HttpContext httpContext, string? redirectUri = null)
        {
            await httpContext.SignOutAsync(PageConstant.AuthenticationSchemeCookie);
            await httpContext.SignOutAsync(
                OpenIdConnectDefaults.AuthenticationScheme,
                new AuthenticationProperties { RedirectUri = redirectUri }
            );
        }

        public static async Task ProviderPageClientLogout(this HttpContext httpContext, string? redirectUri = null)
        {
            await httpContext.SignOutAsync(
                OpenIdConnectDefaults.AuthenticationScheme,
                new AuthenticationProperties { RedirectUri = redirectUri }
            );
        }
    }
}
