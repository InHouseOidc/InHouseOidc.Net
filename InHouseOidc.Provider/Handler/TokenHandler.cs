// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common;
using InHouseOidc.Common.Constant;
using InHouseOidc.Provider.Exception;
using InHouseOidc.Provider.Extension;
using InHouseOidc.Provider.Type;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace InHouseOidc.Provider.Handler
{
    internal class TokenHandler : IEndpointHandler<TokenHandler>
    {
        private readonly IClientStore clientStore;
        private readonly ICodeStore? codeStore;
        private readonly IJsonWebTokenHandler jsonWebTokenHandler;
        private readonly ProviderOptions providerOptions;
        private readonly IUtcNow utcNow;
        private readonly IUserStore? userStore;

        public TokenHandler(
            IClientStore clientStore,
            IJsonWebTokenHandler jsonWebTokenHandler,
            ProviderOptions providerOptions,
            IServiceProvider serviceProvider,
            IUtcNow utcNow
        )
        {
            this.clientStore = clientStore;
            this.jsonWebTokenHandler = jsonWebTokenHandler;
            this.providerOptions = providerOptions;
            this.utcNow = utcNow;
            if (providerOptions.GrantTypes.Contains(GrantType.AuthorizationCode))
            {
                this.codeStore = serviceProvider.GetRequiredService<ICodeStore>();
                this.userStore = serviceProvider.GetRequiredService<IUserStore>();
            }
        }

        public async Task<bool> HandleRequest(HttpRequest httpRequest)
        {
            // Only POST with form body is allowed
            if (!HttpMethods.IsPost(httpRequest.Method))
            {
                throw new BadRequestException(
                    ProviderConstant.InvalidHttpMethod,
                    "Token request used invalid method: {method}",
                    httpRequest.Method
                );
            }
            // Parse the form post body
            var formDictionary = await httpRequest.GetFormDictionary();
            if (formDictionary == null)
            {
                throw new BadRequestException(
                    ProviderConstant.InvalidContentType,
                    "Token request used invalid content type"
                );
            }
            // Handle according to grant type
            if (!formDictionary.TryGetValue(TokenEndpointConstant.GrantType, out var grantType))
            {
                throw new BadRequestException(ProviderConstant.InvalidGrant, "Token request missing grant_type");
            }
            var issuer = httpRequest.GetBaseUriString();
            switch (grantType)
            {
                case TokenEndpointConstant.AuthorizationCode:
                    if (!this.providerOptions.GrantTypes.Contains(GrantType.AuthorizationCode))
                    {
                        throw new BadRequestException(
                            ProviderConstant.UnsupportedGrantType,
                            "Token request used unsupported grant type: {grantType}",
                            grantType
                        );
                    }
                    return await this.HandleAuthorizationCode(formDictionary, httpRequest, issuer);
                case TokenEndpointConstant.ClientCredentials:
                    if (!this.providerOptions.GrantTypes.Contains(GrantType.ClientCredentials))
                    {
                        throw new BadRequestException(
                            ProviderConstant.UnsupportedGrantType,
                            "Token request used unsupported grant type: {grantType}",
                            grantType
                        );
                    }
                    return await this.HandleClientCredentials(formDictionary, httpRequest, issuer);
                case TokenEndpointConstant.RefreshToken:
                    if (!this.providerOptions.GrantTypes.Contains(GrantType.RefreshToken))
                    {
                        throw new BadRequestException(
                            ProviderConstant.UnsupportedGrantType,
                            "Token request used unsupported grant type: {grantType}",
                            grantType
                        );
                    }
                    return await this.HandleRefreshToken(formDictionary, httpRequest, issuer);
                default:
                    throw new BadRequestException(
                        ProviderConstant.UnsupportedGrantType,
                        "Unsupported grant_type requested"
                    );
            }
        }

        private async Task<bool> HandleAuthorizationCode(
            Dictionary<string, string> formDictionary,
            HttpRequest httpRequest,
            string issuer
        )
        {
            // Load and validate the authorization request
            if (
                !formDictionary.TryGetValue(TokenEndpointConstant.Code, out var code) || string.IsNullOrWhiteSpace(code)
            )
            {
                throw this.AuthorizationBadRequest(
                    issuer,
                    null,
                    ProviderConstant.InvalidRequest,
                    "Token request missing code"
                );
            }
            // Check for appropriate fields only
            if (!ValidateFormFields(TokenEndpointConstant.AuthorizationCodeValidFields, formDictionary))
            {
                throw this.AuthorizationBadRequest(
                    issuer,
                    code,
                    ProviderConstant.InvalidRequest,
                    "Token request includes invalid form field: {formFields}",
                    formDictionary.Keys
                );
            }
            // Validate the client
            var clientValidate = await this.ValidateClient(httpRequest, formDictionary);
            if (clientValidate.ErrorMessage != null)
            {
                throw new BadRequestException(
                    ProviderConstant.InvalidClient,
                    clientValidate.ErrorMessage,
                    clientValidate.ErrorArgs
                );
            }
            // Check grant type
            if (clientValidate.OidcClient.GrantTypes == null)
            {
                throw this.AuthorizationBadRequest(
                    issuer,
                    code,
                    ProviderConstant.InvalidClient,
                    "Grant types not specified for client id: {clientId}",
                    clientValidate.ClientId
                );
            }
            if (!clientValidate.OidcClient.GrantTypes.Any(d => d.Equals(GrantType.AuthorizationCode)))
            {
                throw this.AuthorizationBadRequest(
                    issuer,
                    code,
                    ProviderConstant.InvalidRequest,
                    "Client does not allow authorization_code grant type"
                );
            }
            // Retrieve the authorization request using the authorization code
            var storedCode = await this.RequiredCodeStore.GetCode(code, CodeType.AuthorizationCode, issuer);
            if (
                storedCode == null
                || storedCode.Expiry < this.utcNow.UtcNow
                || storedCode.ConsumeCount > 0
                || storedCode.Content == null
            )
            {
                var originalConsumeCount = storedCode?.ConsumeCount ?? 0;
                if (storedCode != null)
                {
                    await this.RequiredCodeStore.ConsumeCode(code, CodeType.AuthorizationCode, issuer);
                }
                var error = originalConsumeCount > 0 ? ProviderConstant.InvalidGrant : ProviderConstant.InvalidRequest;
                throw this.AuthorizationBadRequest(
                    issuer,
                    code,
                    error,
                    "Authorisation code not found, expired, or already consumed"
                );
            }
            var authorizationRequest = JsonSerializer.Deserialize<AuthorizationRequest>(
                storedCode.Content,
                JsonHelper.JsonSerializerOptions
            );
            if (authorizationRequest == null)
            {
                throw this.AuthorizationBadRequest(
                    issuer,
                    code,
                    ProviderConstant.InvalidRequest,
                    "Unable to deserialize persisted authorisation request"
                );
            }
            // Check the redirect_uris match the request
            if (
                !formDictionary.TryGetValue(TokenEndpointConstant.RedirectUri, out var redirectUri)
                || string.IsNullOrWhiteSpace(redirectUri)
            )
            {
                throw this.AuthorizationBadRequest(
                    issuer,
                    code,
                    ProviderConstant.InvalidRequest,
                    "Token request missing redirect_uri name"
                );
            }
            if (authorizationRequest.RedirectUri != redirectUri || string.IsNullOrWhiteSpace(storedCode.Subject))
            {
                throw this.AuthorizationBadRequest(
                    issuer,
                    code,
                    ProviderConstant.InvalidRequest,
                    "Persisted request parameters mismatch"
                );
            }
            // Check the code verifier
            if (
                formDictionary.TryGetValue(TokenEndpointConstant.CodeVerifier, out var codeVerifier)
                && !string.IsNullOrWhiteSpace(codeVerifier)
            )
            {
                var codeVerifierHashed = authorizationRequest.CodeChallengeMethod switch
                {
                    CodeChallengeMethod.S256 => HashHelper.HashCodeVerifierS256(codeVerifier),
                    _
                        => throw this.AuthorizationBadRequest(
                            issuer,
                            code,
                            ProviderConstant.InvalidRequest,
                            "Invalid code challenge method {codeChallengeMethod}",
                            authorizationRequest.CodeChallengeMethod ?? CodeChallengeMethod.None
                        )
                };
                if (
                    string.IsNullOrEmpty(authorizationRequest.CodeChallenge)
                    || !CryptographicOperations.FixedTimeEquals(
                        Encoding.ASCII.GetBytes(codeVerifierHashed),
                        Encoding.ASCII.GetBytes(authorizationRequest.CodeChallenge)
                    )
                )
                {
                    throw this.AuthorizationBadRequest(
                        issuer,
                        code,
                        ProviderConstant.InvalidRequest,
                        "Code verifier mismatch"
                    );
                }
            }
            else
            {
                if (this.providerOptions.AuthorizationCodePkceRequired)
                {
                    throw this.AuthorizationBadRequest(
                        issuer,
                        code,
                        ProviderConstant.InvalidRequest,
                        "Token request missing code_verifier"
                    );
                }
            }
            // Check the user is still active
            if (!await this.RequiredUserStore.IsUserActive(storedCode.Issuer, storedCode.Subject))
            {
                throw this.AuthorizationBadRequest(
                    issuer,
                    code,
                    ProviderConstant.InvalidRequest,
                    "User is now inactive"
                );
            }
            // Good request
            await this.RequiredCodeStore.ConsumeCode(code, CodeType.AuthorizationCode, issuer);
            var requestedScopes = authorizationRequest.Scope.Split(' ').ToList();
            if (!authorizationRequest.SessionExpiryUtc.HasValue)
            {
                throw this.AuthorizationBadRequest(
                    issuer,
                    code,
                    ProviderConstant.InvalidRequest,
                    "AuthorizationRequest has no value for SessionExpiryUtc"
                );
            }
            var sesionExpiryUtc = authorizationRequest.SessionExpiryUtc.Value;
            var accessTokenExpiry = new[]
            {
                this.utcNow.UtcNow.UtcDateTime.Add(clientValidate.OidcClient.AccessTokenExpiry),
                sesionExpiryUtc.UtcDateTime,
            }.Min();
            var accessToken = await this.jsonWebTokenHandler.GetAccessToken(
                clientValidate.ClientId,
                accessTokenExpiry,
                issuer,
                requestedScopes,
                storedCode.Subject
            );
            var idToken = this.jsonWebTokenHandler.GetIdToken(
                authorizationRequest,
                clientValidate.ClientId,
                issuer,
                requestedScopes,
                storedCode.Subject
            );
            var refreshToken = requestedScopes.Contains(TokenEndpointConstant.OfflineAccess)
                ? await this.IssueRefreshToken(
                    clientValidate.ClientId,
                    issuer,
                    requestedScopes,
                    sesionExpiryUtc,
                    storedCode.Subject
                )
                : null;
            await this.ReturnTokens(
                httpRequest,
                accessToken,
                accessTokenExpiry,
                idToken,
                refreshToken,
                authorizationRequest.SessionState
            );
            return true;
        }

        private async Task<bool> HandleClientCredentials(
            Dictionary<string, string> formDictionary,
            HttpRequest httpRequest,
            string issuer
        )
        {
            // Check for appropriate fields only
            if (!ValidateFormFields(TokenEndpointConstant.ClientCredentialsValidFields, formDictionary))
            {
                throw new BadRequestException(
                    ProviderConstant.InvalidRequest,
                    "Token request includes invalid form field: {formFields}",
                    formDictionary.Keys
                );
            }
            // Validate the client
            var clientValidate = await this.ValidateClient(httpRequest, formDictionary);
            if (clientValidate.ErrorMessage != null)
            {
                throw new BadRequestException(
                    ProviderConstant.InvalidClient,
                    clientValidate.ErrorMessage,
                    clientValidate.ErrorArgs
                );
            }
            // Check grant type
            if (clientValidate.OidcClient.GrantTypes == null)
            {
                throw new BadRequestException(
                    ProviderConstant.InvalidClient,
                    "Grant types not specified for client id: {clientId}",
                    clientValidate.ClientId
                );
            }
            if (!clientValidate.OidcClient.GrantTypes.Any(d => d.Equals(GrantType.ClientCredentials)))
            {
                throw new BadRequestException(
                    ProviderConstant.InvalidClient,
                    "Client does not allow client_credentials grant type"
                );
            }
            // Check scopes
            if (clientValidate.OidcClient.Scopes == null)
            {
                throw new BadRequestException(
                    ProviderConstant.InvalidClient,
                    "Scopes not specified for client id: {clientId}",
                    clientValidate.ClientId
                );
            }
            if (
                !formDictionary.TryGetValue(TokenEndpointConstant.Scope, out var requestedScopeValue)
                || string.IsNullOrEmpty(requestedScopeValue)
            )
            {
                throw new BadRequestException(ProviderConstant.InvalidScope, "Token request missing scope name");
            }
            var requestedScopes = requestedScopeValue.Split(' ').ToList();
            var validScopes = clientValidate.OidcClient.Scopes.Distinct().Intersect(requestedScopes.Distinct());
            if (validScopes.Count() != requestedScopes.Count)
            {
                throw new BadRequestException(
                    ProviderConstant.InvalidScope,
                    "Invalid scope requested: {scopes}",
                    requestedScopes
                );
            }
            // Good request
            var accessTokenExpiry = this.utcNow.UtcNow.UtcDateTime.Add(clientValidate.OidcClient.AccessTokenExpiry);
            var accessToken = await this.jsonWebTokenHandler.GetAccessToken(
                clientValidate.ClientId,
                accessTokenExpiry,
                issuer,
                requestedScopes,
                null
            );
            await this.ReturnTokens(httpRequest, accessToken, accessTokenExpiry, null, null, null);
            return true;
        }

        private async Task<bool> HandleRefreshToken(
            Dictionary<string, string> formDictionary,
            HttpRequest httpRequest,
            string issuer
        )
        {
            // Load and validate the refresh token request
            if (
                !formDictionary.TryGetValue(TokenEndpointConstant.RefreshToken, out var refreshToken)
                || string.IsNullOrWhiteSpace(refreshToken)
            )
            {
                throw new BadRequestException(ProviderConstant.InvalidRequest, "Token request missing refresh token");
            }
            // Check for appropriate fields only
            if (!ValidateFormFields(TokenEndpointConstant.RefreshTokenValidFields, formDictionary))
            {
                throw new BadRequestException(
                    ProviderConstant.InvalidRequest,
                    "Token request includes invalid form field: {formFields}",
                    formDictionary.Keys
                );
            }
            // Get the client
            if (
                !formDictionary.TryGetValue(TokenEndpointConstant.ClientId, out var clientId)
                || string.IsNullOrWhiteSpace(clientId)
            )
            {
                throw new BadRequestException(ProviderConstant.InvalidClient, "Token request missing client_id");
            }
            var client = await this.clientStore.GetClient(clientId);
            if (client == null)
            {
                throw new BadRequestException(
                    ProviderConstant.InvalidClient,
                    "Unknown client id: {clientId}",
                    clientId
                );
            }
            // Check grant type
            if (client.GrantTypes == null)
            {
                throw new BadRequestException(
                    ProviderConstant.InvalidClient,
                    "Grant types not specified for client id: {clientId}",
                    clientId
                );
            }
            if (!client.GrantTypes.Any(d => d.Equals(GrantType.RefreshToken)))
            {
                throw new BadRequestException(
                    ProviderConstant.InvalidRequest,
                    "Client does not allow refresh_token grant type"
                );
            }
            // Check the refresh token is valid
            var storedCode = await this.RequiredCodeStore.GetCode(refreshToken, CodeType.RefreshTokenCode, issuer);
            if (storedCode == null || storedCode.Expiry < this.utcNow.UtcNow || storedCode.Content == null)
            {
                if (storedCode != null)
                {
                    await this.RequiredCodeStore.DeleteCode(refreshToken, CodeType.RefreshTokenCode, issuer);
                }
                throw new BadRequestException(ProviderConstant.InvalidRequest, "Refresh token not found or expired");
            }
            var refreshTokenRequest = JsonSerializer.Deserialize<RefreshTokenRequest>(
                storedCode.Content,
                JsonHelper.JsonSerializerOptions
            );
            if (refreshTokenRequest == null)
            {
                throw new BadRequestException(
                    ProviderConstant.InvalidRequest,
                    "Unable to deserialize persisted refresh token request"
                );
            }
            // Check client id matches
            if (refreshTokenRequest.ClientId != clientId)
            {
                throw new BadRequestException(
                    ProviderConstant.InvalidRequest,
                    "Client does not match refresh token request"
                );
            }
            // Check the user is still active
            if (!await this.RequiredUserStore.IsUserActive(storedCode.Issuer, storedCode.Subject))
            {
                throw new BadRequestException(ProviderConstant.InvalidToken, "Refresh token user inactive");
            }
            // Check the scopes are either skipped or identical to, or a subset of, the original scopes
            var requestedScopes = (List<string>?)null;
            if (formDictionary.TryGetValue(TokenEndpointConstant.Scope, out var requestedScopeValue))
            {
                requestedScopes = requestedScopeValue.Split(' ').ToList();
                var persistedScopes = refreshTokenRequest.Scope.Split(',').ToList();
                var validScopes = persistedScopes.Distinct().Intersect(requestedScopes.Distinct());
                if (validScopes.Count() != requestedScopes.Count)
                {
                    throw new BadRequestException(
                        ProviderConstant.InvalidScope,
                        "Invalid scope requested: {scopes}",
                        requestedScopes
                    );
                }
            }
            else
            {
                // No scopes requested, just use the same scopes as the original request
                requestedScopes = refreshTokenRequest.Scope.Split(',').ToList();
            }
            // Good request
            var utcNow = this.utcNow.UtcNow;
            var accessTokenExpiry = new[]
            {
                utcNow.UtcDateTime.Add(client.AccessTokenExpiry),
                refreshTokenRequest.SessionExpiryUtc.UtcDateTime,
            }.Min();
            var accessToken = await this.jsonWebTokenHandler.GetAccessToken(
                clientId,
                accessTokenExpiry,
                issuer,
                requestedScopes,
                storedCode.Subject
            );
            var newRefreshToken = await this.IssueRefreshToken(
                clientId,
                issuer,
                requestedScopes,
                refreshTokenRequest.SessionExpiryUtc,
                storedCode.Subject
            );
            await this.RequiredCodeStore.DeleteCode(refreshToken, CodeType.RefreshTokenCode, issuer);
            await this.ReturnTokens(httpRequest, accessToken, accessTokenExpiry, null, newRefreshToken, null);
            return true;
        }

        private async Task<string> IssueRefreshToken(
            string clientId,
            string issuer,
            List<string> requestedScopes,
            DateTimeOffset sessionExpiryUtc,
            string subject
        )
        {
            var refreshToken = new RefreshTokenRequest(clientId, string.Join(',', requestedScopes), sessionExpiryUtc);
            var content = JsonSerializer.Serialize(refreshToken, JsonHelper.JsonSerializerOptions);
            var storedCode = new StoredCode(
                HashHelper.GenerateCode(),
                CodeType.RefreshTokenCode,
                content,
                issuer,
                subject
            )
            {
                Created = this.utcNow.UtcNow,
                Expiry = sessionExpiryUtc,
            };
            await this.RequiredCodeStore.SaveCode(storedCode);
            return storedCode.Code;
        }

        private async Task ReturnTokens(
            HttpRequest httpRequest,
            string? accessToken,
            DateTime accessTokenExpiry,
            string? idToken,
            string? refreshToken,
            string? sessionState
        )
        {
            // Return as JSON
            using var memoryStream = new MemoryStream();
            using var utf8JsonWriter = new Utf8JsonWriter(memoryStream, JsonHelper.JsonWriterOptions);
            utf8JsonWriter.WriteStartObject();
            if (accessToken != null)
            {
                utf8JsonWriter.WriteNameValue(JsonWebTokenConstant.AccessToken, accessToken);
            }
            var accessTokenExpirySeconds = Math.Max(
                (int)(accessTokenExpiry - this.utcNow.UtcNow.DateTime).TotalSeconds,
                0
            );
            utf8JsonWriter.WriteNameValue(JsonWebTokenConstant.ExpiresIn, accessTokenExpirySeconds);
            if (idToken != null)
            {
                utf8JsonWriter.WriteNameValue(JsonWebTokenConstant.IdToken, idToken);
            }
            if (refreshToken != null)
            {
                utf8JsonWriter.WriteNameValue(JsonWebTokenConstant.RefreshToken, refreshToken);
            }
            if (sessionState != null)
            {
                utf8JsonWriter.WriteNameValue(JsonWebTokenConstant.SessionState, sessionState);
            }
            utf8JsonWriter.WriteNameValue(JsonWebTokenConstant.TokenType, JsonWebTokenConstant.Bearer);
            utf8JsonWriter.WriteEndObject();
            utf8JsonWriter.Flush();
            var response = httpRequest.HttpContext.Response;
            response.ContentType = ContentTypeConstant.ApplicationJson;
            response.ContentLength = memoryStream.Length;
            memoryStream.Seek(0, SeekOrigin.Begin);
            // Write response content
            await httpRequest.HttpContext.Response.WriteStreamJsonContent(memoryStream);
        }

        private BadRequestException AuthorizationBadRequest(
            string issuer,
            string? code,
            string error,
            string logMessage,
            params object[]? args
        )
        {
            if (code != null)
            {
                this.RequiredCodeStore.ConsumeCode(code, CodeType.AuthorizationCode, issuer).GetAwaiter().GetResult();
            }
            return new BadRequestException(error, logMessage, args);
        }

        [ExcludeFromCodeCoverage(
            Justification = "Exceptions are unreachable as constructor will ensure non-null values in enabled flows"
        )]
        private ICodeStore RequiredCodeStore
        {
            get
            {
                return this.codeStore ?? throw new InvalidOperationException("CodeStore is required but not available");
            }
        }

        [ExcludeFromCodeCoverage(
            Justification = "Exceptions are unreachable as constructor will ensure non-null values in enabled flows"
        )]
        private IUserStore RequiredUserStore
        {
            get
            {
                return this.userStore ?? throw new InvalidOperationException("UserStore is required but not available");
            }
        }

        private async Task<ClientValidation> ValidateClient(
            HttpRequest httpRequest,
            Dictionary<string, string> formDictionary
        )
        {
            formDictionary.TryGetValue(TokenEndpointConstant.ClientId, out var clientId);
            if (!formDictionary.TryGetValue(TokenEndpointConstant.ClientSecret, out var clientSecret))
            {
                // Not a form parameter, may be in the header
                var authorizationHeader = httpRequest.Headers[ProviderConstant.Authorization].FirstOrDefault();
                if (!string.IsNullOrEmpty(authorizationHeader))
                {
                    if (!authorizationHeader.StartsWith(ProviderConstant.Basic))
                    {
                        return new ClientValidation { ErrorMessage = "Invalid client secret Authorization header" };
                    }
                    var basicAuthentication = Encoding.UTF8.GetString(
                        Convert.FromBase64String(authorizationHeader[ProviderConstant.Basic.Length..])
                    );
                    var basicParts = basicAuthentication.Split(':');
                    if (
                        basicParts.Length != 2
                        || string.IsNullOrEmpty(basicParts[0])
                        || string.IsNullOrEmpty(basicParts[1])
                    )
                    {
                        return new ClientValidation { ErrorMessage = "Malformed client secret Authorization header" };
                    }
                    var basicClientId = Uri.UnescapeDataString(basicParts[0]);
                    var basicClientSecret = Uri.UnescapeDataString(basicParts[1]);
                    clientSecret = basicClientSecret;
                    if (string.IsNullOrEmpty(clientId))
                    {
                        clientId = basicClientId;
                    }
                    else if (basicClientId != clientId)
                    {
                        return new ClientValidation
                        {
                            ErrorMessage = "Client identifier in Authorization header does not match form field",
                        };
                    }
                }
            }
            if (string.IsNullOrEmpty(clientId))
            {
                return new ClientValidation { ErrorMessage = "Token request missing client_id" };
            }
            var oidcClient = await this.clientStore.GetClient(clientId);
            if (oidcClient == null)
            {
                return new ClientValidation
                {
                    ErrorMessage = "Unknown client id: {clientId}",
                    ErrorArgs = new[] { clientId },
                };
            }
            if (oidcClient.ClientSecretRequired ?? false)
            {
                if (string.IsNullOrWhiteSpace(clientSecret))
                {
                    return new ClientValidation { ErrorMessage = "Token request missing client_secret" };
                }
                var isValidSecret = await this.clientStore.IsCorrectClientSecret(clientId, clientSecret);
                if (!isValidSecret)
                {
                    return new ClientValidation { ErrorMessage = "Invalid client_secret" };
                }
            }
            return new ClientValidation { OidcClient = oidcClient, ClientId = clientId };
        }

        private static bool ValidateFormFields(List<string> validFields, Dictionary<string, string> formDictionary)
        {
            foreach (var formFieldKey in formDictionary.Keys)
            {
                if (!validFields.Contains(formFieldKey))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
