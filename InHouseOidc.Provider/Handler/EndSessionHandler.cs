// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common;
using InHouseOidc.Common.Constant;
using InHouseOidc.Common.Extension;
using InHouseOidc.Provider.Exception;
using InHouseOidc.Provider.Extension;
using InHouseOidc.Provider.Type;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using System.Security.Claims;

namespace InHouseOidc.Provider.Handler
{
    internal class EndSessionHandler : IEndpointHandler<EndSessionHandler>
    {
        private readonly IClientStore clientStore;
        private readonly ICodeStore codeStore;
        private readonly ProviderOptions providerOptions;
        private readonly IServiceProvider serviceProvider;
        private readonly IUtcNow utcNow;
        private readonly IValidationHandler validationHandler;

        public EndSessionHandler(
            IClientStore clientStore,
            ICodeStore codeStore,
            ProviderOptions providerOptions,
            IServiceProvider serviceProvider,
            IUtcNow utcNow,
            IValidationHandler validationHandler
        )
        {
            this.clientStore = clientStore;
            this.codeStore = codeStore;
            this.providerOptions = providerOptions;
            this.serviceProvider = serviceProvider;
            this.utcNow = utcNow;
            this.validationHandler = validationHandler;
        }

        public async Task<bool> HandleRequest(HttpRequest httpRequest)
        {
            // Only GET & POST allowed
            if (!HttpMethods.IsGet(httpRequest.Method) && !HttpMethods.IsPost(httpRequest.Method))
            {
                throw this.EndSessionError(httpRequest, "HttpMethod not supported: {method}", httpRequest.Method);
            }
            // Extract parameters to a dictionary
            var parameters = HttpMethods.IsGet(httpRequest.Method)
                ? httpRequest.GetQueryDictionary()
                : await httpRequest.GetFormDictionary();
            if (parameters == null)
            {
                throw this.EndSessionError(httpRequest, "Unable to resolve end session parameters");
            }
            // Parse and validate the request
            var issuer = httpRequest.GetBaseUriString();
            parameters.TryGetNonEmptyValue(EndSessionEndpointConstant.IdTokenHint, out var idTokenHint);
            ClaimsPrincipal? tokenPrincipal = null;
            if (idTokenHint != null)
            {
                tokenPrincipal = await this.validationHandler.ValidateJsonWebToken(null, issuer, idTokenHint, false);
                if (tokenPrincipal == null)
                {
                    throw this.EndSessionError(httpRequest, "Invalid id token hint");
                }
            }
            if (
                parameters.TryGetNonEmptyValue(
                    EndSessionEndpointConstant.PostLogoutRedirectUri,
                    out var postLogoutRedirectUri
                )
            )
            {
                if (!await this.clientStore.IsKnownPostLogoutRedirectUri(postLogoutRedirectUri))
                {
                    throw this.EndSessionError(httpRequest, "Invalid post_logout_redirect_uri");
                }
            }
            if (!string.IsNullOrEmpty(postLogoutRedirectUri) && string.IsNullOrEmpty(idTokenHint))
            {
                throw this.EndSessionError(httpRequest, "Invalid post_logout_redirect_uri without id_token_hint");
            }
            if (parameters.TryGetValue(EndSessionEndpointConstant.State, out var state) && !string.IsNullOrEmpty(state))
            {
                if (state.Length > 512)
                {
                    throw this.EndSessionError(httpRequest, "State exceeds maximum length of 512 characters");
                }
            }
            // Request well formed, see if the user is authenticated
            var (claimsPrincipal, _) = await httpRequest.GetClaimsPrincipal(this.serviceProvider);
            if (claimsPrincipal == null)
            {
                if (!string.IsNullOrEmpty(idTokenHint))
                {
                    // Valid, or recently valid, token hint so let the user confirm if they want to logout
                    httpRequest.HttpContext.Response.Redirect(this.providerOptions.LogoutPath);
                    return true;
                }
                throw this.EndSessionError(httpRequest, "End session request for unauthenticated user");
            }
            // Validate any token hint subject matches the claims principal subjevt
            if (tokenPrincipal != null && tokenPrincipal.GetSubjectClaim() != claimsPrincipal.GetSubjectClaim())
            {
                throw this.EndSessionError(httpRequest, "Id token hint subject does not match authenticated subject");
            }
            // Issue a logout code and store it
            var subject = claimsPrincipal.GetSubjectClaim();
            var logoutRequest = new LogoutRequest
            {
                IdTokenHint = idTokenHint,
                PostLogoutRedirectUri = postLogoutRedirectUri,
                State = state,
                Subject = subject,
            };
            var logoutCode = await this.CreateLogoutCode(httpRequest, logoutRequest, subject);
            // Redirect to the logout page
            var queryBuilderLogoutUri = new QueryBuilder { { EndSessionEndpointConstant.LogoutCode, logoutCode } };
            httpRequest.HttpContext.Response.Redirect($"{this.providerOptions.LogoutPath}{queryBuilderLogoutUri}");
            return true;
        }

        private async Task<string> CreateLogoutCode(
            HttpRequest httpRequest,
            LogoutRequest logoutRequest,
            string subject
        )
        {
            var content = System.Text.Json.JsonSerializer.Serialize(logoutRequest, JsonHelper.JsonSerializerOptions);
            var storedCode = new StoredCode(
                HashHelper.GenerateCode(),
                CodeType.LogoutCode,
                content,
                httpRequest.GetBaseUriString(),
                subject
            )
            {
                Created = this.utcNow.UtcNow,
                Expiry = this.utcNow.UtcNow.AddMinutes(5),
            };
            await this.codeStore.SaveCode(storedCode);
            return storedCode.Code;
        }

        private RedirectErrorException EndSessionError(
            HttpRequest httpRequest,
            string logMessage,
            params object[]? args
        )
        {
            // Redirect to standard error page, no indication to caller as to the nature of the problem
            var uri = $"{httpRequest.GetBaseUriString()}{this.providerOptions.ErrorPath}";
            return new RedirectErrorException(RedirectErrorType.InvalidRequest, uri, logMessage, args);
        }
    }
}
