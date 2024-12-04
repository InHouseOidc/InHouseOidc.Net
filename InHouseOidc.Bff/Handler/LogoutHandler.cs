// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using System.Net;
using InHouseOidc.Bff.Resolver;
using InHouseOidc.Bff.Type;
using InHouseOidc.Common.Constant;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace InHouseOidc.Bff.Handler
{
    internal class LogoutHandler(IBffClientResolver bffClientResolver, ClientOptions clientOptions)
        : IEndpointHandler<LogoutHandler>
    {
        public async Task<bool> HandleRequest(HttpContext httpContext)
        {
            if (!HttpMethods.IsGet(httpContext.Request.Method))
            {
                httpContext.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                return true;
            }
            var authenticateResult = await httpContext.AuthenticateAsync(BffConstant.AuthenticationSchemeCookie);
            if (!authenticateResult.Succeeded)
            {
                // We're already logged out (possibly due to check session on a different tab)
                httpContext.Response.Redirect(clientOptions.PostLogoutRedirectAddress);
                return true;
            }
            // Check the caller is able to supply the current session id
            var sessionId = QueryParamResolver.GetValue(httpContext.Request, string.Empty, "sessionId");
            if (sessionId != authenticateResult.Principal.FindFirst(JsonWebTokenClaim.SessionId)?.Value)
            {
                httpContext.Response.StatusCode = 400;
                return true;
            }
            // Sign out locally
            await httpContext.SignOutAsync(BffConstant.AuthenticationSchemeCookie);
            // Sign out at the OIDC identity provider
            var properties = new AuthenticationProperties { RedirectUri = clientOptions.PostLogoutRedirectAddress, };
            var (_, scheme) = bffClientResolver.GetClient(httpContext);
            await httpContext.SignOutAsync(scheme, properties);
            return true;
        }
    }
}
