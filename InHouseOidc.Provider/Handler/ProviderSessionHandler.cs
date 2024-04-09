// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common;
using InHouseOidc.Common.Constant;
using InHouseOidc.Provider.Extension;
using InHouseOidc.Provider.Type;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;

namespace InHouseOidc.Provider.Handler
{
    internal class ProviderSessionHandler(
        ICodeStore codeStore,
        IHttpContextAccessor httpContextAccessor,
        ProviderOptions providerOptions,
        IUtcNow utcNow,
        IValidationHandler validationHandler
    ) : IProviderSession
    {
        private readonly ICodeStore codeStore = codeStore;
        private readonly IHttpContextAccessor httpContextAccessor = httpContextAccessor;
        private readonly ProviderOptions providerOptions = providerOptions;
        private readonly IUtcNow utcNow = utcNow;
        private readonly IValidationHandler validationHandler = validationHandler;

        public async Task<LogoutRequest?> GetLogoutRequest(string logoutCode)
        {
            // Access the logout request
            if (this.httpContextAccessor.HttpContext == null)
            {
                throw new InvalidOperationException("No HttpContext found to logout");
            }
            var issuer = this.httpContextAccessor.HttpContext.Request.GetBaseUriString();
            var storedCode = await this.codeStore.GetCode(logoutCode, CodeType.LogoutCode, issuer);
            if (storedCode == null || storedCode.Expiry < this.utcNow.UtcNow || storedCode.Content == null)
            {
                if (storedCode != null)
                {
                    await this.codeStore.DeleteCode(logoutCode, CodeType.LogoutCode, issuer);
                }
                return null;
            }
            var logoutRequest = JsonSerializer.Deserialize<LogoutRequest>(
                storedCode.Content,
                JsonHelper.JsonSerializerOptions
            );
            return logoutRequest;
        }

        public async Task<bool> IsValidReturnUrl(string returnUrl)
        {
            // Must be setup for authorization
            if (!this.providerOptions.GrantTypes.Contains(GrantType.AuthorizationCode))
            {
                throw new InvalidOperationException("AuthorizationCode flow not enabled");
            }
            // Check for good URI
            var returnUri = returnUrl.Split('?');
            if (returnUri.Length != 2)
            {
                return false;
            }
            // Check return path
            if (this.providerOptions.AuthorizationEndpointUri.OriginalString != returnUri[0])
            {
                return false;
            }
            // Check query parameters
            var queryCollection = QueryHelpers.ParseQuery(returnUri[1]);
            var queryDictionary = new Dictionary<string, string>();
            foreach (var item in queryCollection)
            {
                queryDictionary.Add(item.Key, item.Value.ToString());
            }
            var (_, authorizationError) = await this.validationHandler.ParseValidateAuthorizationRequest(
                queryDictionary
            );
            return authorizationError == null;
        }

        public async Task<ClaimsPrincipal> Login(HttpContext httpContext, List<Claim> claims, TimeSpan sessionExpiry)
        {
            // Must be setup for authorization
            if (!this.providerOptions.GrantTypes.Contains(GrantType.AuthorizationCode))
            {
                throw new InvalidOperationException("AuthorizationCode flow not enabled");
            }
            var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
            var sessionId = HashHelper.GenerateSessionId();
            claims.Add(
                new Claim(
                    JsonWebTokenClaim.AuthenticationTime,
                    this.utcNow.UtcNow.ToUnixTimeSeconds().ToString(),
                    ClaimValueTypes.Integer64
                )
            );
            claims.Add(new Claim(JsonWebTokenClaim.IdentityProvider, this.providerOptions.IdentityProvider));
            claims.Add(new Claim(JsonWebTokenClaim.SessionId, sessionId));
            identity.AddClaims(claims);
            var principal = new ClaimsPrincipal(identity);
            var authenticationProperties = new AuthenticationProperties
            {
                ExpiresUtc = this.utcNow.UtcNow.Add(sessionExpiry),
                IsPersistent = true,
            };
            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authenticationProperties
            );
            if (this.providerOptions.CheckSessionEndpointEnabled)
            {
                httpContext.Response.AppendSessionCookie(
                    this.providerOptions.CheckSessionCookieName,
                    httpContext.Request.IsHttps,
                    sessionId
                );
            }
            return principal;
        }

        public async Task Logout(
            HttpContext httpContext,
            string? logoutCode = null,
            LogoutRequest? logoutRequest = null
        )
        {
            // Must be setup for authorization
            if (!this.providerOptions.GrantTypes.Contains(GrantType.AuthorizationCode))
            {
                throw new InvalidOperationException("AuthorizationCode flow not enabled");
            }
            if (this.providerOptions.CheckSessionEndpointEnabled)
            {
                httpContext.Response.DeleteSessionCookie(this.providerOptions.CheckSessionCookieName);
            }
            if (!string.IsNullOrEmpty(logoutCode))
            {
                var issuer = httpContext.Request.GetBaseUriString();
                await this.codeStore.DeleteCode(logoutCode, CodeType.LogoutCode, issuer);
            }
            if (logoutRequest == null)
            {
                await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
            else
            {
                var authenticationProperties = (AuthenticationProperties?)null;
                if (!string.IsNullOrEmpty(logoutRequest.PostLogoutRedirectUri))
                {
                    var redirectUrl = logoutRequest.PostLogoutRedirectUri;
                    if (!string.IsNullOrEmpty(logoutRequest.State))
                    {
                        redirectUrl += $"?state={logoutRequest.State}";
                    }
                    authenticationProperties = new AuthenticationProperties { RedirectUri = redirectUrl };
                }
                await httpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    authenticationProperties
                );
            }
        }
    }
}
