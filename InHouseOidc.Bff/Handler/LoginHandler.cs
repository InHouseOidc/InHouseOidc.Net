// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using System.Net;
using InHouseOidc.Bff.Resolver;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace InHouseOidc.Bff.Handler
{
    internal class LoginHandler(IBffClientResolver bffClientResolver) : IEndpointHandler<LoginHandler>
    {
        public async Task<bool> HandleRequest(HttpContext httpContext)
        {
            if (!HttpMethods.IsGet(httpContext.Request.Method))
            {
                httpContext.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                return true;
            }
            var properties = new AuthenticationProperties
            {
                RedirectUri = QueryParamResolver.GetValue(httpContext.Request, "/", "returnUrl"),
            };
            var (_, scheme) = bffClientResolver.GetClient(httpContext);
            await httpContext.ChallengeAsync(scheme, properties);
            return true;
        }
    }
}
