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

namespace InHouseOidc.Provider.Handler
{
    internal class AuthorizationHandler(
        ICodeStore codeStore,
        ProviderOptions providerOptions,
        IServiceProvider serviceProvider,
        IUtcNow utcNow,
        IValidationHandler validationHandler
    ) : IEndpointHandler<AuthorizationHandler>
    {
        private readonly ICodeStore codeStore = codeStore;
        private readonly ProviderOptions providerOptions = providerOptions;
        private readonly IServiceProvider serviceProvider = serviceProvider;
        private readonly IUtcNow utcNow = utcNow;
        private readonly IValidationHandler validationHandler = validationHandler;

        public async Task<bool> HandleRequest(HttpRequest httpRequest)
        {
            // Only GET & POST allowed
            if (!HttpMethods.IsGet(httpRequest.Method) && !HttpMethods.IsPost(httpRequest.Method))
            {
                throw this.RedirectToErrorUri(httpRequest, "HttpMethod not supported: {method}", httpRequest.Method);
            }
            // Extract parameters to a dictionary
            var parameters =
                (
                    HttpMethods.IsGet(httpRequest.Method)
                        ? httpRequest.GetQueryDictionary()
                        : await httpRequest.GetFormDictionary()
                ) ?? throw this.RedirectToErrorUri(httpRequest, "Unable to resolve authorization request parameters");
            // Parse and validate the request
            var (authorizationRequest, authorizationError) =
                await this.validationHandler.ParseValidateAuthorizationRequest(parameters);
            if (authorizationError != null)
            {
                if (authorizationError.RedirectUri == null)
                {
                    throw this.RedirectToErrorUri(httpRequest, authorizationError.LogMessage, authorizationError.Args);
                }
                else
                {
                    throw RedirectToReturnUri(
                        authorizationError.RedirectErrorType,
                        authorizationError.RedirectUri,
                        authorizationError.SessionState,
                        authorizationError.State,
                        authorizationError.LogMessage,
                        authorizationError.Args
                    );
                }
            }
            if (authorizationRequest == null)
            {
                // Note: this could should be unreachable as ParseValidateAuthorizationRequest should always return one non-null value
                throw this.RedirectToErrorUri(httpRequest, "Unable to parse and validate authorization request");
            }
            // Request well formed, see if the user is authenticated
            var (claimsPrincipal, authenticationProperties) = await httpRequest.GetClaimsPrincipal(
                this.serviceProvider
            );
            if (
                claimsPrincipal == null
                || authenticationProperties == null
                || authorizationRequest.Prompt == Prompt.Login
            )
            {
                switch (authorizationRequest.Prompt)
                {
                    case Prompt.Login:
                    case null:
                        // User not authenticated and/or prompt=[empty]/login so redirect to Login endpoint
                        this.RedirectToLogin(httpRequest, parameters);
                        return true;
                    case Prompt.None:
                        // User not authenticated and prompt=none so redirect to redirectURI with error
                        throw RedirectToReturnUri(
                            RedirectErrorType.LoginRequired,
                            authorizationRequest.RedirectUri,
                            authorizationRequest.SessionState,
                            authorizationRequest.State,
                            "Login required with prompt=none"
                        );
                    default:
                        throw RedirectToReturnUri(
                            RedirectErrorType.ServerError,
                            authorizationRequest.RedirectUri,
                            authorizationRequest.SessionState,
                            authorizationRequest.State,
                            "Unknown Prompt: {prompt}",
                            authorizationRequest.Prompt ?? Prompt.NotSpecified
                        );
                }
            }
            // Check if the maximum age requested has now passed
            if (authorizationRequest.MaxAge.HasValue)
            {
                var authenticationTime = claimsPrincipal.GetAuthenticationTimeClaim();
                if (authenticationTime.AddSeconds(authorizationRequest.MaxAge.Value) < this.utcNow.UtcNow)
                {
                    // Force login
                    this.RedirectToLogin(httpRequest, parameters);
                    return true;
                }
            }
            // Check if the time to session expiry is less than the mimumum allowed for token issuance
            if (authenticationProperties.ExpiresUtc.HasValue)
            {
                var timeToSessionExpiry = authenticationProperties.ExpiresUtc.Value - this.utcNow.UtcNow;
                if (timeToSessionExpiry < this.providerOptions.AuthorizationMinimumTokenExpiry)
                {
                    // Tokens can be issued, but token lifetime would be below the minimum time allowed
                    throw RedirectToReturnUri(
                        RedirectErrorType.LoginRequired,
                        authorizationRequest.RedirectUri,
                        authorizationRequest.SessionState,
                        authorizationRequest.State,
                        "Login required as session is near expiry"
                    );
                }
            }
            // Use the authentication expiry as the session expiry
            authorizationRequest.SessionExpiryUtc = authenticationProperties.ExpiresUtc;
            // Check any id token hint is valid
            var issuer = httpRequest.GetBaseUriString();
            if (authorizationRequest.IdTokenHint != null)
            {
                var tokenPrincipal =
                    await this.validationHandler.ValidateJsonWebToken(
                        null,
                        issuer,
                        authorizationRequest.IdTokenHint,
                        true
                    )
                    ?? throw RedirectToReturnUri(
                        RedirectErrorType.InvalidRequest,
                        authorizationRequest.RedirectUri,
                        authorizationRequest.SessionState,
                        authorizationRequest.State,
                        "Invalid id token hint"
                    );
                if (tokenPrincipal.GetSubjectClaim() != claimsPrincipal.GetSubjectClaim())
                {
                    throw RedirectToReturnUri(
                        RedirectErrorType.InvalidRequest,
                        authorizationRequest.RedirectUri,
                        authorizationRequest.SessionState,
                        authorizationRequest.State,
                        "Id token hint subject does not match authenticated subject"
                    );
                }
            }
            if (this.providerOptions.CheckSessionEndpointEnabled)
            {
                // Attach a new session id to the authorisation request
                var sessionId = claimsPrincipal.GetSessionIdClaim();
                authorizationRequest.SessionState = HashHelper.GenerateSessionState(
                    null,
                    authorizationRequest.ClientId,
                    authorizationRequest.RedirectUri,
                    sessionId
                );
                // Issue new session cookie, or replace any different session cookie
                var cookieSessionId = httpRequest.Cookies[this.providerOptions.CheckSessionCookieName];
                if (cookieSessionId != sessionId)
                {
                    httpRequest.HttpContext.Response.AppendSessionCookie(
                        this.providerOptions.CheckSessionCookieName,
                        httpRequest.IsHttps,
                        sessionId
                    );
                }
            }
            // User authenticated so issue a new authorisation code and redirect to redirectURI with the authorisation code included
            var code = await this.CreateAuthorizationCode(authorizationRequest, claimsPrincipal, issuer);
            var queryBuilderCode = new QueryBuilder
            {
                { AuthorizationEndpointConstant.Code, code },
                { AuthorizationEndpointConstant.Scope, authorizationRequest.Scope },
            };
            if (!string.IsNullOrEmpty(authorizationRequest.SessionState))
            {
                queryBuilderCode.Add(AuthorizationEndpointConstant.SessionState, authorizationRequest.SessionState);
            }
            if (!string.IsNullOrEmpty(authorizationRequest.State))
            {
                queryBuilderCode.Add(AuthorizationEndpointConstant.State, authorizationRequest.State);
            }
            httpRequest.HttpContext.Response.Redirect($"{authorizationRequest.RedirectUri}{queryBuilderCode}");
            return true;
        }

        private async Task<string> CreateAuthorizationCode(
            AuthorizationRequest authorizationRequest,
            ClaimsPrincipal claimsPrincipal,
            string issuer
        )
        {
            var subject = claimsPrincipal.GetSubjectClaim();
            authorizationRequest.AuthorizationRequestClaims.AddRange(
                claimsPrincipal.Claims.Select(c => new AuthorizationRequestClaim(c.Type, c.Value))
            );
            var content = JsonSerializer.Serialize(authorizationRequest, JsonHelper.JsonSerializerOptions);
            var storedCode = new StoredCode(
                HashHelper.GenerateCode(),
                CodeType.AuthorizationCode,
                content,
                issuer,
                subject
            )
            {
                Created = this.utcNow.UtcNow,
                Expiry = this.utcNow.UtcNow.AddMinutes(5),
            };
            await this.codeStore.SaveCode(storedCode);
            return storedCode.Code;
        }

        private RedirectErrorException RedirectToErrorUri(
            HttpRequest httpRequest,
            string logMessage,
            params object[]? args
        )
        {
            // Redirect to standard error page, no indication to caller as to the nature of the problem
            var uri = $"{httpRequest.GetBaseUriString()}{this.providerOptions.ErrorPath}";
            return new RedirectErrorException(RedirectErrorType.InvalidRequest, uri, logMessage, args);
        }

        private void RedirectToLogin(HttpRequest httpRequest, Dictionary<string, string> parameters)
        {
            parameters.Remove(AuthorizationEndpointConstant.Prompt);
            var queryBuilderReturnUrl = new QueryBuilder(parameters);
            var queryBuilderRedirectUri = new QueryBuilder
            {
                {
                    AuthorizationEndpointConstant.ReturnUrl,
                    $"{this.providerOptions.AuthorizationEndpointUri.OriginalString}{queryBuilderReturnUrl}"
                },
            };
            httpRequest.HttpContext.Response.Redirect($"{this.providerOptions.LoginPath}{queryBuilderRedirectUri}");
        }

        private static RedirectErrorException RedirectToReturnUri(
            RedirectErrorType redirectErrorType,
            string redirectUri,
            string? sessionState,
            string? state,
            string logMessage,
            params object[]? args
        )
        {
            throw new RedirectErrorException(redirectErrorType, redirectUri, logMessage, args)
            {
                SessionState = sessionState,
                State = state,
            };
        }
    }
}
