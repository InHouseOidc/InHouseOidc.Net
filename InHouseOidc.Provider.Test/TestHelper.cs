// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common.Constant;
using InHouseOidc.Test.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace InHouseOidc.Provider.Test
{
    internal static class TestHelper
    {
        internal static string ReadBodyAsString(HttpResponse httpResponse)
        {
            using var reader = new StreamReader(httpResponse.Body, Encoding.UTF8);
            httpResponse.Body.Seek(0, SeekOrigin.Begin);
            return reader.ReadToEnd();
        }

        internal static (ClaimsPrincipal, AuthenticationProperties) SetupClaimsPrincipal(
            TimeSpan authenticationOffset,
            string scheme,
            string? subject,
            string sessionId,
            DateTimeOffset utcNow,
            List<Claim>? extraClaims = null,
            TimeSpan? sessionExpiry = null
        )
        {
            var claims = new List<Claim>
            {
                new(JsonWebTokenClaim.SessionId, sessionId),
                new(
                    JsonWebTokenClaim.AuthenticationTime,
                    (utcNow + authenticationOffset).ToUnixTimeSeconds().ToString()
                ),
            };
            if (!string.IsNullOrEmpty(subject))
            {
                claims.Add(new Claim(JsonWebTokenClaim.Subject, subject));
            }
            if (extraClaims != null)
            {
                claims.AddRange(extraClaims);
            }
            var claimsIdentity = new ClaimsIdentity(claims, scheme);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            var authenticationProperties = new AuthenticationProperties(new Dictionary<string, string?>())
            {
                ExpiresUtc = utcNow + (sessionExpiry ?? TimeSpan.FromHours(1)),
            };
            return (claimsPrincipal, authenticationProperties);
        }

        internal static void SetupContextClaimsPrincipal(
            DefaultHttpContext context,
            TestServiceCollection serviceCollection,
            bool authenticeSuccess,
            TimeSpan authenticationOffset,
            string scheme,
            string subject,
            string sessionId,
            DateTimeOffset utcNow,
            TimeSpan? sessionExpiry = null
        )
        {
            var authenticationScheme = new AuthenticationScheme(scheme, null, typeof(TestHandler));
            var mockAuthenticationSchemeProvider = new Mock<IAuthenticationSchemeProvider>();
            mockAuthenticationSchemeProvider
                .Setup(m => m.GetDefaultAuthenticateSchemeAsync())
                .ReturnsAsync(authenticationScheme);
            serviceCollection.AddSingleton(mockAuthenticationSchemeProvider.Object);
            var mockAuthenticationHandler = new Mock<IAuthenticationHandler>(MockBehavior.Strict);
            var mockAuthenticationHandlerProvider = new Mock<IAuthenticationHandlerProvider>(MockBehavior.Strict);
            var (claimsPrincipal, authenticationProperties) = SetupClaimsPrincipal(
                authenticationOffset,
                scheme,
                subject,
                sessionId,
                utcNow,
                null,
                sessionExpiry
            );
            var authenticateResult = authenticeSuccess
                ? AuthenticateResult.Success(
                    new AuthenticationTicket(claimsPrincipal, authenticationProperties, scheme)
                )
                : AuthenticateResult.NoResult();
            mockAuthenticationHandler.Setup(m => m.AuthenticateAsync()).ReturnsAsync(authenticateResult);
            mockAuthenticationHandlerProvider
                .Setup(m => m.GetHandlerAsync(context, scheme))
                .ReturnsAsync(mockAuthenticationHandler.Object);
            serviceCollection.AddSingleton(mockAuthenticationHandlerProvider.Object);
        }

        internal class TestHandler : IAuthenticationHandler
        {
            public Task<AuthenticateResult> AuthenticateAsync()
            {
                throw new NotImplementedException();
            }

            public Task ChallengeAsync(AuthenticationProperties? properties)
            {
                throw new NotImplementedException();
            }

            public Task ForbidAsync(AuthenticationProperties? properties)
            {
                throw new NotImplementedException();
            }

            public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
            {
                throw new NotImplementedException();
            }
        }
    }
}
