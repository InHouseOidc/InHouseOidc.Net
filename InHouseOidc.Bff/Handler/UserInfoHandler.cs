// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using System.Net;
using System.Security.Claims;
using InHouseOidc.Bff.Resolver;
using InHouseOidc.Bff.Type;
using InHouseOidc.Discovery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace InHouseOidc.Bff.Handler
{
    internal class UserInfoHandler(
        IBffClientResolver bffClientResolver,
        ClientOptions clientOptions,
        IDiscoveryResolver discoveryResolver
    ) : IEndpointHandler<UserInfoHandler>
    {
        public async Task<bool> HandleRequest(HttpContext httpContext)
        {
            if (!HttpMethods.IsGet(httpContext.Request.Method))
            {
                httpContext.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                return true;
            }
            var authenticateResult = await httpContext.AuthenticateAsync(BffConstant.AuthenticationSchemeCookie);
            if (authenticateResult.Succeeded && authenticateResult.Principal.Identity?.IsAuthenticated == true)
            {
                // Authenticated, return user information
                var identity = authenticateResult.Principal.Identity;
                var (bffClientOptions, scheme) = bffClientResolver.GetClient(httpContext);
                var discovery =
                    await discoveryResolver.GetDiscovery(
                        clientOptions.DiscoveryOptions,
                        bffClientOptions.OidcProviderAddress,
                        CancellationToken.None
                    ) ?? throw new InvalidOperationException("Unable to resolve discovery");
                var checkSessionUri = discovery.CheckSessionEndpoint;
                var sessionExpiry = authenticateResult.Properties.ExpiresUtc?.ToString("u");
                var sessionState = authenticateResult.Properties.GetString(OpenIdConnectSessionProperties.SessionState);
                var claims = ((ClaimsIdentity)identity)
                    .Claims.OrderBy(c => c.Type)
                    .Select(c => new { type = c.Type, value = c.Value })
                    .ToArray();
                await httpContext.Response.WriteAsJsonAsync(
                    new
                    {
                        checkSessionUri,
                        claims,
                        clientId = bffClientOptions.ClientId,
                        isAuthenticated = true,
                        sessionExpiry,
                        sessionState,
                    }
                );
            }
            else
            {
                // Not authenticated
                await httpContext.Response.WriteAsJsonAsync(new { isAuthenticated = false });
            }
            return true;
        }
    }
}
