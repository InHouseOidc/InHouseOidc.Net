// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common;
using InHouseOidc.Common.Constant;
using InHouseOidc.Provider.Exception;
using InHouseOidc.Provider.Extension;
using InHouseOidc.Provider.Type;
using Microsoft.AspNetCore.Authentication;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace InHouseOidc.Provider.Handler
{
    internal class JsonWebTokenHandler : IJsonWebTokenHandler
    {
        private readonly ProviderOptions providerOptions;
        private readonly IResourceStore resourceStore;
        private readonly IServiceProvider serviceProvider;
        private readonly IUtcNow utcNow;

        public JsonWebTokenHandler(
            ProviderOptions providerOptions,
            IResourceStore resourceStore,
            IServiceProvider serviceProvider,
            IUtcNow utcNow
        )
        {
            this.providerOptions = providerOptions;
            this.resourceStore = resourceStore;
            this.serviceProvider = serviceProvider;
            this.utcNow = utcNow;
        }

        public async Task<string> GetAccessToken(
            string clientId,
            DateTimeOffset expiry,
            string issuer,
            List<string> scopes,
            string? subject
        )
        {
            var header = this.GetJwtHeader(JsonWebTokenConstant.AccessTokenType);
            var utcNow = this.utcNow.UtcNow;
            var payload = new JwtPayload(issuer, null, null, utcNow.UtcDateTime, expiry.UtcDateTime);
            payload.AddClaim(
                new Claim(JsonWebTokenClaim.IssuedAt, utcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            );
            payload.AddClaim(new Claim(JsonWebTokenClaim.ClientId, clientId));
            if (!string.IsNullOrEmpty(subject))
            {
                payload.AddClaim(new Claim(JsonWebTokenClaim.Subject, subject));
            }
            var audiences = await this.resourceStore.GetAudiences(scopes);
            foreach (var audience in audiences.Distinct())
            {
                payload.AddClaim(new Claim(JsonWebTokenClaim.Audience, audience));
            }
            foreach (var requestScope in scopes)
            {
                payload.AddClaim(new Claim(JsonWebTokenClaim.Scope, requestScope));
            }
            var token = new JwtSecurityToken(header, payload);
            var handler = new JwtSecurityTokenHandler();
            return handler.WriteToken(token);
        }

        public string GetIdToken(
            AuthorizationRequest authorizationRequest,
            string clientId,
            string issuer,
            List<string> scopes,
            string subject
        )
        {
            var utcNow = this.utcNow.UtcNow;
            var header = this.GetJwtHeader(JsonWebTokenConstant.Jwt);
            var claims = new List<Claim>
            {
                new Claim(JsonWebTokenClaim.IssuedAt, utcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim(JsonWebTokenClaim.Subject, subject),
            };
            if (!string.IsNullOrWhiteSpace(authorizationRequest.Nonce))
            {
                claims.Add(new Claim(JsonWebTokenClaim.Nonce, authorizationRequest.Nonce));
            }
            claims.Add(
                new Claim(
                    JsonWebTokenClaim.AuthenticationTime,
                    authorizationRequest.GetClaimValue(JsonWebTokenClaim.AuthenticationTime),
                    ClaimValueTypes.Integer64
                )
            );
            claims.Add(
                new Claim(
                    JsonWebTokenClaim.IdentityProvider,
                    authorizationRequest.GetClaimValue(JsonWebTokenClaim.IdentityProvider)
                )
            );
            claims.Add(
                new Claim(JsonWebTokenClaim.SessionId, authorizationRequest.GetClaimValue(JsonWebTokenClaim.SessionId))
            );
            var authorizationRequestClaims = authorizationRequest.AuthorizationRequestClaims;
            if (!this.providerOptions.UserInfoEndpointEnabled)
            {
                // Only include standard claims in ID token when userinfo is disabled
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
            }
            if (!authorizationRequest.SessionExpiryUtc.HasValue)
            {
                throw new InternalErrorException("AuthorizationRequest has no value for SessionExpiryUtc");
            }
            var expires = authorizationRequest.SessionExpiryUtc.Value.UtcDateTime;
            var payload = new JwtPayload(issuer, clientId, claims, utcNow.UtcDateTime.AddSeconds(-1), expires);
            // Add claims that need to be serialized as arrays even with only 1 entry
            if (authorizationRequestClaims.Any(c => c.Type == JsonWebTokenClaim.AuthenticationMethodReference))
            {
                var amrs = authorizationRequestClaims
                    .Where(c => c.Type == JsonWebTokenClaim.AuthenticationMethodReference)
                    .Select(c => c.Value)
                    .ToArray();
                payload.Add(JsonWebTokenClaim.AuthenticationMethodReference, amrs);
            }
            var roles = ExtractScopeClaims(
                    JsonWebTokenConstant.Role,
                    scopes,
                    JsonWebTokenClaim.RoleClaims,
                    authorizationRequestClaims
                )
                .Select(c => c.Value)
                .ToArray();
            if (roles.Any())
            {
                payload.Add(JsonWebTokenClaim.Role, roles);
            }
            var token = new JwtSecurityToken(header, payload);
            var handler = new JwtSecurityTokenHandler();
            return handler.WriteToken(token);
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
            return new List<Claim>();
        }

        private JwtHeader GetJwtHeader(string tokenType)
        {
            // Filter out not-before and expired, sort so longest expiry period appears first
            var utcNow = this.utcNow.UtcNow;
            var signingKey = this.providerOptions.SigningKeys
                .Resolve(this.serviceProvider)
                .Where(sk => sk.NotAfter >= utcNow && sk.NotBefore <= utcNow)
                .OrderByDescending(sk => (sk.NotAfter - utcNow).TotalSeconds)
                .FirstOrDefault();
            if (signingKey == null)
            {
                throw new InternalErrorException("Unable to resolve signing credentials for JWT");
            }
            return new JwtHeader(signingKey.SigningCredentials)
            {
                [JsonWebTokenClaim.Typ] = tokenType,
                [JsonWebTokenClaim.X5t] = signingKey.JsonWebKey.X5t,
            };
        }
    }
}
