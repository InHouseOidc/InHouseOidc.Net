// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common.Constant;

namespace InHouseOidc.Common.Extension
{
    public static class ClaimsPrincipalExtension
    {
        public static DateTimeOffset GetAuthenticationTimeClaim(this ClaimsPrincipal claimsPrincipal)
        {
            var claim = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == JsonWebTokenClaim.AuthenticationTime);
            return claim == null
                ? throw new InvalidOperationException("auth_time claim not found")
                : DateTimeOffset.FromUnixTimeSeconds(long.Parse(claim.Value));
        }

        public static string GetSessionIdClaim(this ClaimsPrincipal claimsPrincipal)
        {
            var claim = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == JsonWebTokenClaim.SessionId);
            return claim == null ? throw new InvalidOperationException("sid claim not found") : claim.Value;
        }

        public static string GetSubjectClaim(this ClaimsPrincipal claimsPrincipal)
        {
            var claim = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == JsonWebTokenClaim.Subject);
            if (claim == null)
            {
                claim = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                if (claim == null)
                {
                    throw new InvalidOperationException("sub/nameidentifier claim not found");
                }
            }
            return claim.Value;
        }
    }
}
