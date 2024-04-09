// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common;
using InHouseOidc.Common.Constant;
using InHouseOidc.Provider.Exception;
using InHouseOidc.Provider.Type;
using Microsoft.IdentityModel.Tokens;

namespace InHouseOidc.Provider.Handler
{
    internal class JsonWebTokenHandler(
        ProviderOptions providerOptions,
        IResourceStore resourceStore,
        ISigningKeyHandler signingKeyHandler,
        IUtcNow utcNow
    ) : IJsonWebTokenHandler
    {
        private readonly ProviderOptions providerOptions = providerOptions;
        private readonly IResourceStore resourceStore = resourceStore;
        private readonly ISigningKeyHandler signingKeyHandler = signingKeyHandler;
        private readonly IUtcNow utcNow = utcNow;

        public async Task<string> GetAccessToken(
            string clientId,
            DateTimeOffset expiry,
            string issuer,
            List<string> scopes,
            string? subject
        )
        {
            var securityTokenDescriptor = await this.GetSecurityTokenDescriptor(JsonWebTokenConstant.AccessTokenType);
            var utcNow = this.utcNow.UtcNow;
            securityTokenDescriptor.Issuer = issuer;
            securityTokenDescriptor.IssuedAt = utcNow.UtcDateTime;
            securityTokenDescriptor.NotBefore = utcNow.UtcDateTime;
            securityTokenDescriptor.Expires = expiry.UtcDateTime;
            securityTokenDescriptor.Claims.Add(JsonWebTokenClaim.ClientId, clientId);
            if (!string.IsNullOrEmpty(subject))
            {
                securityTokenDescriptor.Claims.Add(JsonWebTokenClaim.Subject, subject);
            }
            var audiences = await this.resourceStore.GetAudiences(scopes);
            securityTokenDescriptor.Claims.Add(JsonWebTokenClaim.Audience, audiences.ToArray());
            securityTokenDescriptor.Claims.Add(JsonWebTokenClaim.Scope, scopes.ToArray());
            var handler = new Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler();
            return handler.CreateToken(securityTokenDescriptor);
        }

        public async Task<string> GetIdToken(
            AuthorizationRequest authorizationRequest,
            string clientId,
            string issuer,
            List<string> scopes,
            string subject
        )
        {
            var utcNow = this.utcNow.UtcNow;
            var securityTokenDescriptor = await this.GetSecurityTokenDescriptor(JsonWebTokenConstant.Jwt);
            securityTokenDescriptor.Audience = clientId;
            securityTokenDescriptor.IssuedAt = utcNow.UtcDateTime.AddSeconds(-1);
            securityTokenDescriptor.Issuer = issuer;
            securityTokenDescriptor.NotBefore = utcNow.UtcDateTime.AddSeconds(-1);
            securityTokenDescriptor.Claims.Add(JsonWebTokenClaim.Subject, subject);
            if (!string.IsNullOrWhiteSpace(authorizationRequest.Nonce))
            {
                securityTokenDescriptor.Claims.Add(JsonWebTokenClaim.Nonce, authorizationRequest.Nonce);
            }
            securityTokenDescriptor.Claims.Add(
                JsonWebTokenClaim.AuthenticationTime,
                long.Parse(authorizationRequest.GetClaimValue(JsonWebTokenClaim.AuthenticationTime))
            );
            securityTokenDescriptor.Claims.Add(
                JsonWebTokenClaim.IdentityProvider,
                authorizationRequest.GetClaimValue(JsonWebTokenClaim.IdentityProvider)
            );
            securityTokenDescriptor.Claims.Add(
                JsonWebTokenClaim.SessionId,
                authorizationRequest.GetClaimValue(JsonWebTokenClaim.SessionId)
            );
            var authorizationRequestClaims = authorizationRequest.AuthorizationRequestClaims;
            if (!this.providerOptions.UserInfoEndpointEnabled)
            {
                // Only include standard claims in ID token when userinfo is disabled
                var claims = new List<Claim>();
                claims.AddRange(
                    ExtractScopeClaims(
                        JsonWebTokenConstant.Address,
                        scopes,
                        JsonWebTokenClaim.AddressClaims,
                        authorizationRequestClaims
                    )
                );
                claims.AddRange(
                    ExtractScopeClaims(
                        JsonWebTokenConstant.Email,
                        scopes,
                        JsonWebTokenClaim.EmailClaims,
                        authorizationRequestClaims
                    )
                );
                claims.AddRange(
                    ExtractScopeClaims(
                        JsonWebTokenConstant.Phone,
                        scopes,
                        JsonWebTokenClaim.PhoneClaims,
                        authorizationRequestClaims
                    )
                );
                claims.AddRange(
                    ExtractScopeClaims(
                        JsonWebTokenConstant.Profile,
                        scopes,
                        JsonWebTokenClaim.ProfileClaims,
                        authorizationRequestClaims
                    )
                );
                foreach (var claim in claims)
                {
                    securityTokenDescriptor.Claims.Add(claim.Type, claim.Value);
                }
            }
            if (!authorizationRequest.SessionExpiryUtc.HasValue)
            {
                throw new InternalErrorException("AuthorizationRequest has no value for SessionExpiryUtc");
            }
            securityTokenDescriptor.Expires = authorizationRequest.SessionExpiryUtc.Value.UtcDateTime;
            // Add claims that need to be serialized as arrays even with only 1 entry
            if (authorizationRequestClaims.Any(c => c.Type == JsonWebTokenClaim.AuthenticationMethodReference))
            {
                var amrs = authorizationRequestClaims
                    .Where(c => c.Type == JsonWebTokenClaim.AuthenticationMethodReference)
                    .Select(c => c.Value)
                    .ToArray();
                securityTokenDescriptor.Claims.Add(JsonWebTokenClaim.AuthenticationMethodReference, amrs);
            }
            var roles = ExtractScopeClaims(
                    JsonWebTokenConstant.Role,
                    scopes,
                    JsonWebTokenClaim.RoleClaims,
                    authorizationRequestClaims
                )
                .Select(c => c.Value)
                .ToArray();
            if (roles.Length > 0)
            {
                securityTokenDescriptor.Claims.Add(JsonWebTokenClaim.Role, roles);
            }
            var handler = new Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler();
            return handler.CreateToken(securityTokenDescriptor);
        }

        private static List<Claim> ExtractScopeClaims(
            string scope,
            List<string> requestedScopes,
            List<string> scopeClaims,
            List<AuthorizationRequestClaim> authorizationRequestClaims
        )
        {
            if (requestedScopes.Contains(scope))
            {
                return authorizationRequestClaims
                    .Where(c => scopeClaims.Contains(c.Type))
                    .Select(c => new Claim(c.Type, c.Value))
                    .ToList();
            }
            return [];
        }

        private async Task<SecurityTokenDescriptor> GetSecurityTokenDescriptor(string tokenType)
        {
            // Filter out not-before and expired, sort so longest expiry period appears first
            var utcNow = this.utcNow.UtcNow;
            var signingKeys = await this.signingKeyHandler.Resolve();
            var signingKey =
                signingKeys
                    .Where(sk => sk.NotAfter >= utcNow && sk.NotBefore <= utcNow)
                    .OrderByDescending(sk => (sk.NotAfter - utcNow).TotalSeconds)
                    .FirstOrDefault()
                ?? throw new InternalErrorException("Unable to resolve signing credentials for JWT");
            // Setup the token descriptor
            var securityTokenDescriptor = new SecurityTokenDescriptor
            {
                Claims = new Dictionary<string, object>(),
                SigningCredentials = signingKey.SigningCredentials,
                TokenType = tokenType,
            };
            return securityTokenDescriptor;
        }
    }
}
