// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common;
using InHouseOidc.Common.Constant;
using InHouseOidc.Provider.Extension;
using InHouseOidc.Provider.Type;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Security.Claims;

namespace InHouseOidc.Provider.Handler
{
    internal class ValidationHandler : IValidationHandler
    {
        private readonly IClientStore clientStore;
        private readonly ILogger<ValidationHandler> logger;
        private readonly ProviderOptions providerOptions;
        private readonly IServiceProvider serviceProvider;

        public ValidationHandler(
            IClientStore clientStore,
            ILogger<ValidationHandler> logger,
            ProviderOptions providerOptions,
            IServiceProvider serviceProvider
        )
        {
            this.clientStore = clientStore;
            this.logger = logger;
            this.providerOptions = providerOptions;
            this.serviceProvider = serviceProvider;
        }

        public async Task<(AuthorizationRequest?, RedirectError?)> ParseValidateAuthorizationRequest(
            Dictionary<string, string> parameters
        )
        {
            // Validate the client id and redirect uri parameters first as the determine whether any redirect error can go back to the redirect uri
            if (!parameters.TryGetNonEmptyValue(AuthorizationEndpointConstant.ClientId, out var clientId))
            {
                return ErrorAuthorization(RedirectErrorType.InvalidRequest, "Authorization request missing client_id");
            }
            var client = await this.clientStore.GetClient(clientId);
            if (client == null)
            {
                return ErrorAuthorization(RedirectErrorType.InvalidRequest, "Unknown client id: {clientId}", clientId);
            }
            if (!parameters.TryGetNonEmptyValue(AuthorizationEndpointConstant.RedirectUri, out var redirectUri))
            {
                return ErrorAuthorization(
                    RedirectErrorType.InvalidRequest,
                    "Authorization request missing redirect_uri"
                );
            }
            if (redirectUri.Length > 512)
            {
                return ErrorAuthorization(
                    RedirectErrorType.InvalidRequest,
                    "Redirect URI exceeds maximum length of 512 characters"
                );
            }
            if (client.RedirectUris == null)
            {
                return ErrorAuthorization(
                    RedirectErrorType.InvalidRequest,
                    "Redirect URIs not specified for client id: {clientId}",
                    clientId
                );
            }
            if (!client.RedirectUris.Any(u => u.Equals(redirectUri, StringComparison.OrdinalIgnoreCase)))
            {
                return ErrorAuthorization(
                    RedirectErrorType.InvalidRequest,
                    "Authorization redirect_uri invalid: {redirectUri}",
                    redirectUri
                );
            }
            // Capture any state to pass back on errors
            if (parameters.TryGetNonEmptyValue(AuthorizationEndpointConstant.State, out var state))
            {
                if (state.Length > 512)
                {
                    return ErrorAuthorization(
                        redirectUri,
                        null,
                        RedirectErrorType.InvalidRequest,
                        "State exceeds maximum length of 512 characters"
                    );
                }
            }
            // Make sure no request parameter has been included
            if (parameters.TryGetValue(AuthorizationEndpointConstant.Request, out var _))
            {
                return ErrorAuthorization(
                    redirectUri,
                    state,
                    RedirectErrorType.RequestNotSupported,
                    "Request parameter is not supported"
                );
            }
            // Make sure all required fields are present
            var authCodeRequiredFields = this.providerOptions.AuthorizationCodePkceRequired
                ? AuthorizationEndpointConstant.AuthorizationCodeRequiredFields
                : AuthorizationEndpointConstant.AuthorizationCodeWithoutPkceRequiredFields;
            if (!RequiredFormFields(authCodeRequiredFields, parameters))
            {
                return ErrorAuthorization(
                    redirectUri,
                    state,
                    RedirectErrorType.InvalidRequest,
                    "Authorization missing one or more required fields: {requiredFields}",
                    AuthorizationEndpointConstant.AuthorizationCodeRequiredFields
                );
            }
            // Check grant type
            if (client.GrantTypes == null)
            {
                return ErrorAuthorization(
                    redirectUri,
                    state,
                    RedirectErrorType.UnauthorizedClient,
                    "Grant types not specified for client id: {clientId}",
                    clientId
                );
            }
            if (!client.GrantTypes.Any(d => d.Equals(GrantType.AuthorizationCode)))
            {
                return ErrorAuthorization(
                    redirectUri,
                    state,
                    RedirectErrorType.UnauthorizedClient,
                    "Client does not allow authorization_code grant type"
                );
            }
            // Parse out the remaining fields
            if (
                !EnumHelper.TryParseEnumMember<ResponseType>(
                    parameters[AuthorizationEndpointConstant.ResponseType],
                    out var responseType
                )
            )
            {
                return ErrorAuthorization(
                    redirectUri,
                    state,
                    RedirectErrorType.InvalidRequest,
                    "Invalid response type: {responseType}",
                    parameters[AuthorizationEndpointConstant.ResponseType]
                );
            }
            var codeChallengeMethod = (CodeChallengeMethod?)null;
            if (
                parameters.TryGetValue(
                    AuthorizationEndpointConstant.CodeChallengeMethod,
                    out var codeChallengeMethodParameter
                )
            )
            {
                if (
                    !EnumHelper.TryParseEnumMember<CodeChallengeMethod>(
                        codeChallengeMethodParameter,
                        out var codeChallengeMethodParsed
                    )
                )
                {
                    return ErrorAuthorization(
                        redirectUri,
                        state,
                        RedirectErrorType.InvalidRequest,
                        "Invalid code challenge method: {codeChallengeMethod}",
                        parameters[AuthorizationEndpointConstant.CodeChallengeMethod]
                    );
                }
                codeChallengeMethod = codeChallengeMethodParsed;
            }
            if (parameters.TryGetValue(AuthorizationEndpointConstant.CodeChallenge, out var codeChallenge))
            {
                if (codeChallenge.Length < 43 || codeChallenge.Length > 128)
                {
                    return ErrorAuthorization(
                        redirectUri,
                        state,
                        RedirectErrorType.InvalidRequest,
                        "Invalid code challenge length. Expected 43-128 characters"
                    );
                }
            }
            // Check scopes
            var scope = parameters[AuthorizationEndpointConstant.Scope];
            if (scope.Length > 512)
            {
                return ErrorAuthorization(
                    redirectUri,
                    state,
                    RedirectErrorType.InvalidRequest,
                    "Scope exceeds maximum length of 512 characters"
                );
            }
            var requestedScopes = scope.Split(' ').ToList();
            if (!requestedScopes.Contains(JsonWebTokenConstant.OpenId))
            {
                return ErrorAuthorization(
                    redirectUri,
                    state,
                    RedirectErrorType.InvalidScope,
                    "Scope missing required openid entry"
                );
            }
            if (client.Scopes == null)
            {
                return ErrorAuthorization(
                    redirectUri,
                    state,
                    RedirectErrorType.InvalidScope,
                    "Scopes not specified for client id: {clientId}",
                    clientId
                );
            }
            var validScopes = client.Scopes.Distinct().Intersect(requestedScopes.Distinct());
            if (validScopes.Count() != requestedScopes.Count)
            {
                return ErrorAuthorization(
                    redirectUri,
                    state,
                    RedirectErrorType.InvalidScope,
                    "Invalid scope requested: {scopes}",
                    requestedScopes
                );
            }
            var authorizationRequest = new AuthorizationRequest(clientId, redirectUri, responseType, scope)
            {
                State = state,
            };
            if (parameters.TryGetValue(AuthorizationEndpointConstant.IdTokenHint, out var idTokenHint))
            {
                authorizationRequest.IdTokenHint = idTokenHint;
            }
            if (parameters.ContainsKey(AuthorizationEndpointConstant.MaxAge))
            {
                if (int.TryParse(parameters[AuthorizationEndpointConstant.MaxAge], out var maxAge))
                {
                    authorizationRequest.MaxAge = maxAge;
                }
                else
                {
                    return ErrorAuthorization(
                        redirectUri,
                        state,
                        RedirectErrorType.InvalidRequest,
                        "Invalid max age: {maxAge}",
                        parameters[AuthorizationEndpointConstant.MaxAge]
                    );
                }
            }
            if (parameters.TryGetValue(AuthorizationEndpointConstant.Nonce, out var nonce))
            {
                if (nonce.Length > 512)
                {
                    return ErrorAuthorization(
                        redirectUri,
                        state,
                        RedirectErrorType.InvalidRequest,
                        "Nonce exceeds maximum length of 512 characters"
                    );
                }
                authorizationRequest.Nonce = nonce;
            }
            if (parameters.TryGetValue(AuthorizationEndpointConstant.Prompt, out var promptString))
            {
                if (EnumHelper.TryParseEnumMember<Prompt>(promptString, out var prompt))
                {
                    authorizationRequest.Prompt = prompt;
                }
                else
                {
                    return ErrorAuthorization(
                        redirectUri,
                        state,
                        RedirectErrorType.InvalidRequest,
                        "Invalid prompt: {prompt}",
                        parameters[AuthorizationEndpointConstant.Prompt]
                    );
                }
            }
            if (parameters.TryGetValue(AuthorizationEndpointConstant.ResponseMode, out var responseModeString))
            {
                if (EnumHelper.TryParseEnumMember<ResponseMode>(responseModeString, out var responseMode))
                {
                    authorizationRequest.ResponseMode = responseMode;
                }
                else
                {
                    return ErrorAuthorization(
                        redirectUri,
                        state,
                        RedirectErrorType.InvalidRequest,
                        "Invalid response mode: {responseMode}",
                        parameters[AuthorizationEndpointConstant.ResponseMode]
                    );
                }
            }
            authorizationRequest.CodeChallengeMethod = codeChallengeMethod;
            authorizationRequest.CodeChallenge = codeChallenge;
            return (authorizationRequest, null);
        }

        public ClaimsPrincipal? ValidateJsonWebToken(string? audience, string issuer, string jwt, bool validateLifetime)
        {
            var signingKeys = this.providerOptions.SigningKeys
                .Resolve(this.serviceProvider)
                .Select(s => s.X509SecurityKey);
            var handler = new Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler();
            var tokenValidationParameters = new TokenValidationParameters
            {
                IssuerSigningKeys = signingKeys.Where(s => s != null),
                ValidIssuer = issuer,
                ValidateAudience = !string.IsNullOrEmpty(audience),
                ValidAudience = audience,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = validateLifetime,
            };
            var tokenValidationResult = handler.ValidateToken(jwt, tokenValidationParameters);
            if (tokenValidationResult.IsValid)
            {
                return new ClaimsPrincipal(tokenValidationResult.ClaimsIdentity);
            }
            this.logger.Log(
                this.providerOptions.LogFailuresAsInformation ? LogLevel.Information : LogLevel.Error,
                tokenValidationResult.Exception,
                "Json web token validation failed: {exceptionType}",
                tokenValidationResult.Exception.GetType().Name
            );
            return null;
        }

        private static (AuthorizationRequest?, RedirectError?) ErrorAuthorization(
            RedirectErrorType redirectErrorType,
            string logMessage,
            params object[]? args
        )
        {
            return (null, new RedirectError(redirectErrorType, logMessage, args));
        }

        private static (AuthorizationRequest?, RedirectError?) ErrorAuthorization(
            string? redirectUri,
            string? state,
            RedirectErrorType redirectErrorType,
            string logMessage,
            params object[]? args
        )
        {
            return (
                null,
                new RedirectError(redirectErrorType, logMessage, args) { RedirectUri = redirectUri, State = state }
            );
        }

        private static bool RequiredFormFields(List<string> requiredFields, Dictionary<string, string> formDictionary)
        {
            var foundFields = requiredFields.Intersect(formDictionary.Keys.Where(f => !string.IsNullOrEmpty(f)));
            return foundFields.Count() == requiredFields.Count;
        }
    }
}
