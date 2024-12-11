// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using System.Dynamic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using InHouseOidc.Common.Constant;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;

namespace InHouseOidc.Example.Common
{
    public static class ExampleHelper
    {
        private static readonly JsonSerializerOptions JsonSerializerOptions = new() { WriteIndented = true };

        public static async Task<ExampleViewModel> GetExampleViewModel(
            HttpContext httpContext,
            string apiResult = "",
            string apiResultProvider = ""
        )
        {
            var accessToken = await httpContext.GetTokenAsync(
                OpenIdConnectDefaults.AuthenticationScheme,
                JsonWebTokenConstant.AccessToken
            );
            var accessTokenExpiry = await httpContext.GetTokenAsync(
                OpenIdConnectDefaults.AuthenticationScheme,
                PageConstant.ExpiresAt
            );
            var claims = new List<Claim>();
            var name = string.Empty;
            var role = string.Empty;
            if (httpContext.User.Identity is ClaimsIdentity claimsIdentity)
            {
                claims = claimsIdentity.Claims.OrderBy(c => c.Type).ToList();
                name = GetClaim(claimsIdentity, claimsIdentity.NameClaimType, "[Name claim not found]");
                role = GetClaim(claimsIdentity, claimsIdentity.RoleClaimType, "[Role claim not found]");
            }
            var idToken = await httpContext.GetTokenAsync(
                OpenIdConnectDefaults.AuthenticationScheme,
                JsonWebTokenConstant.IdToken
            );
            var refreshToken = await httpContext.GetTokenAsync(
                OpenIdConnectDefaults.AuthenticationScheme,
                JsonWebTokenConstant.RefreshToken
            );
            return new ExampleViewModel
            {
                AccessToken = accessToken,
                AccessTokenExpiry =
                    accessTokenExpiry == null
                        ? string.Empty
                        : DateTime.Parse(accessTokenExpiry).ToUniversalTime().ToString("u"),
                AccessTokenJson = GetTokenJson(accessToken),
                ApiResultProvider = apiResultProvider,
                ApiResult = apiResult,
                Claims = claims,
                IdToken = idToken,
                IdTokenExpiry = GetIdTokenExpiry(idToken),
                IdTokenJson = GetTokenJson(idToken),
                Name = name,
                RefreshToken = refreshToken,
                Role = role,
                SessionExpiry = await GetSessionExpiry(httpContext),
            };
        }

        public static async Task<string> CallApi(HttpClient httpClient, string apiAddress)
        {
            var response = await httpClient.GetAsync(new Uri(new Uri(apiAddress), "/secure"));
            if (response.IsSuccessStatusCode)
            {
                return $"/secure response: {await response.Content.ReadAsStringAsync()}";
            }
            return $"/secure response: StatusCode = {(int)response.StatusCode} {response.StatusCode}";
        }

        private static string GetClaim(ClaimsIdentity claimsIdentity, string type, string defaultValue)
        {
            var claims = claimsIdentity.Claims.Where(c => c.Type == type).Select(c => c.Value).ToList();
            if (claims == null || claims.Count == 0)
            {
                return defaultValue;
            }
            return string.Join(",", claims);
        }

        private static string GetIdTokenExpiry(string? idToken)
        {
            if (idToken == null)
            {
                return string.Empty;
            }
            var handler = new JwtSecurityTokenHandler();
            var securityToken = handler.ReadJwtToken(idToken);
            var exp = securityToken.Claims.FirstOrDefault(c => c.Type == JsonWebTokenClaim.Exp);
            if (exp == null)
            {
                return string.Empty;
            }
            var expiry = DateTimeOffset.FromUnixTimeSeconds(int.Parse(exp.Value));
            return expiry.ToUniversalTime().ToString("u");
        }

        private static async Task<string> GetSessionExpiry(HttpContext httpContext)
        {
            var authenticateResult = await httpContext.AuthenticateAsync();
            if (authenticateResult == null || !authenticateResult.Succeeded)
            {
                return string.Empty;
            }
            return authenticateResult.Properties.ExpiresUtc?.ToString("u") ?? string.Empty;
        }

        private static string GetTokenJson(string? token)
        {
            if (token == null)
            {
                return string.Empty;
            }
            var handler = new JwtSecurityTokenHandler();
            var securityToken = handler.ReadJwtToken(token);
            var unformattedJson = securityToken.ToString();
            var parts = unformattedJson.Split("}.{");
            var header = JsonSerializer.Deserialize<ExpandoObject>(parts[0] + "}");
            var headerJson = JsonSerializer.Serialize(header, JsonSerializerOptions);
            var body = JsonSerializer.Deserialize<ExpandoObject>("{" + parts[1]);
            var bodyJson = JsonSerializer.Serialize(body, JsonSerializerOptions);
            return headerJson + Environment.NewLine + bodyJson;
        }
    }
}
