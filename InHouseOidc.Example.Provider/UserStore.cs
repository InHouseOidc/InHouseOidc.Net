// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common.Constant;
using InHouseOidc.Provider;

namespace InHouseOidc.Example.Provider
{
    public class UserStore : IUserStore
    {
        public Task<List<Claim>?> GetUserClaims(string issuer, string subject, List<string> scopes)
        {
            var claims = new List<Claim>
            {
                new(JsonWebTokenClaim.Subject, subject),
                new(JsonWebTokenClaim.PhoneNumber, "+64 (21) 1111111"),
            };
            return Task.FromResult<List<Claim>?>(claims);
        }

        public Task<bool> IsUserActive(string issuer, string subject)
        {
            return Task.FromResult(true);
        }
    }
}
