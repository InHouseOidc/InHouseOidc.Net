// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common.Constant;

namespace InHouseOidc.Common
{
    public static class ClaimsExtension
    {
        public static bool HasScope(this IEnumerable<Claim> claims, string scope)
        {
            var scopeClaims = claims.Where(c => c.Type == JsonWebTokenClaim.Scope).ToList();
            if (scopeClaims.Count == 0)
            {
                return false;
            }
            // Check for multiple claims, 1 scope per claim
            if (scopeClaims.Count > 1)
            {
                return scopeClaims.Any(c => c.Value == scope);
            }
            // Only 1 claim, check for space separated scopes
            var scopeClaim = scopeClaims.First();
            if (scopeClaim.Value.Contains(' '))
            {
                var splitScopes = scopeClaim.Value.Split(' ');
                return splitScopes.Contains(scope);
            }
            return scopeClaim.Value == scope;
        }
    }
}
