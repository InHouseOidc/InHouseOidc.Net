// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common;
using InHouseOidc.Common.Constant;
using InHouseOidc.Provider.Constant;
using InHouseOidc.Provider.Exception;
using InHouseOidc.Provider.Extension;
using Microsoft.AspNetCore.Http;

namespace InHouseOidc.Provider.Handler
{
    internal class UserInfoHandler(IUserStore userStore, IValidationHandler validationHandler)
        : IEndpointHandler<UserInfoHandler>
    {
        private readonly IUserStore userStore = userStore;
        private readonly IValidationHandler validationHandler = validationHandler;

        public async Task<bool> HandleRequest(HttpRequest httpRequest)
        {
            // Only GET & POST allowed
            if (!HttpMethods.IsGet(httpRequest.Method) && !HttpMethods.IsPost(httpRequest.Method))
            {
                throw new BadRequestException(
                    ProviderConstant.InvalidHttpMethod,
                    "HttpMethod not supported: {method}",
                    httpRequest.Method
                );
            }
            // Check for the bearer authorisation header
            string? token = null;
            string? authorisationHeader = httpRequest.Headers[ApiConstant.Authorization];
            if (
                !string.IsNullOrEmpty(authorisationHeader)
                && authorisationHeader.StartsWith(ApiConstant.Bearer, StringComparison.InvariantCultureIgnoreCase)
            )
            {
                token = authorisationHeader[ApiConstant.Bearer.Length..];
            }
            if (string.IsNullOrEmpty(token) && HttpMethods.IsPost(httpRequest.Method))
            {
                // Parse the form post body
                var formDictionary =
                    await httpRequest.GetFormDictionary()
                    ?? throw new BadRequestException(
                        ProviderConstant.InvalidContentType,
                        "User info post request used invalid content type"
                    );
                formDictionary.TryGetValue(JsonWebTokenConstant.AccessToken, out token);
            }
            if (string.IsNullOrEmpty(token))
            {
                throw new BadRequestException(ProviderConstant.InvalidRequest, "Missing bearer token");
            }
            // Validate the token
            var issuer = httpRequest.GetBaseUriString();
            var claimsPrincipal =
                await this.validationHandler.ValidateJsonWebToken(null, issuer, token, true)
                ?? throw new BadRequestException(ProviderConstant.InvalidToken, "Bearer token invalid");
            // Subject and scope claims must be included in the access token
            var subjectClaim = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == JsonWebTokenClaim.Subject);
            var scopeClaims = claimsPrincipal.Claims.Where(c => c.Type == JsonWebTokenClaim.Scope).ToList();
            if (subjectClaim == null || scopeClaims.Count == 0)
            {
                throw new BadRequestException(ProviderConstant.InvalidToken, "Token is missing required claims");
            }
            List<string> scopes;
            if (scopeClaims.Count == 1 && scopeClaims.First().Value.Contains(' '))
            {
                scopes = scopeClaims.First().Value.Split(" ").ToList();
            }
            else
            {
                scopes = scopeClaims.Select(c => c.Value).ToList();
            }
            // Access the user claims
            var claims =
                await this.userStore.GetUserClaims(issuer, subjectClaim.Value, scopes)
                ?? throw new BadRequestException(ProviderConstant.InvalidToken, "Unable to access user claims");
            // Include any non-standard claims returned
            var nonStandardClaims = claims.Where(c => !JsonWebTokenClaim.StandardClaims.Contains(c.Type));
            // Extract requested standard claims only
            var standardClaims = new List<Claim>();
            standardClaims.AddRange(
                ExtractScopeClaims(JsonWebTokenConstant.Address, scopes, JsonWebTokenClaim.AddressClaims, claims)
            );
            standardClaims.AddRange(
                ExtractScopeClaims(JsonWebTokenConstant.Email, scopes, JsonWebTokenClaim.EmailClaims, claims)
            );
            standardClaims.AddRange(
                ExtractScopeClaims(JsonWebTokenConstant.Phone, scopes, JsonWebTokenClaim.PhoneClaims, claims)
            );
            standardClaims.AddRange(
                ExtractScopeClaims(JsonWebTokenConstant.Profile, scopes, JsonWebTokenClaim.ProfileClaims, claims)
            );
            // Return as JSON
            var returnClaims = new Dictionary<string, object>();
            var allClaims = standardClaims.Concat(nonStandardClaims).ToList();
            var activeClaimTypes = allClaims.Select(c => c.Type).Distinct().ToList();
            foreach (var activeClaimType in activeClaimTypes)
            {
                var activeValues = allClaims.Where(c => c.Type == activeClaimType).ToList();
                if (activeValues.Count == 1)
                {
                    returnClaims.Add(activeClaimType, GetClaimValue(activeValues.First()));
                }
                else
                {
                    returnClaims.Add(activeClaimType, activeValues.Select(c => GetClaimValue(c)).ToList());
                }
            }
            var claimsSerialised = JsonSerializer.Serialize(returnClaims, JsonHelper.JsonSerializerOptions);
            var httpResponse = httpRequest.HttpContext.Response;
            httpResponse.ContentType = ContentTypeConstant.ApplicationJson;
            httpResponse.StatusCode = (int)HttpStatusCode.OK;
            await httpResponse.WriteAsync(claimsSerialised, Encoding.UTF8);
            return true;
        }

        private static IEnumerable<Claim> ExtractScopeClaims(
            string scope,
            List<string> requestedScopes,
            List<string> scopeClaims,
            List<Claim> userInfoClaims
        )
        {
            if (requestedScopes.Contains(scope))
            {
                var foundClaims = userInfoClaims.Where(c => scopeClaims.Contains(c.Type));
                foreach (var claim in foundClaims)
                {
                    yield return claim;
                }
            }
        }

        private static object GetClaimValue(Claim claim)
        {
            switch (claim.ValueType)
            {
                case ClaimValueTypes.Boolean:
                    if (bool.TryParse(claim.Value, out var boolValue))
                    {
                        return boolValue;
                    }
                    break;
                case ClaimValueTypes.Integer:
                case ClaimValueTypes.Integer32:
                    if (int.TryParse(claim.Value, out var intValue))
                    {
                        return intValue;
                    }
                    break;
                case ClaimValueTypes.Integer64:
                    if (long.TryParse(claim.Value, out var longValue))
                    {
                        return longValue;
                    }
                    break;
            }
            return claim.Value;
        }
    }
}
