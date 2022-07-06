// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common.Constant;
using InHouseOidc.Provider;
using System.Security.Claims;

namespace InHouseOidc.Certify.Provider
{
    public class UserStore : IUserStore
    {
        private readonly Dictionary<string, UserConfig> users = new();

        public UserStore(IConfiguration configuration)
        {
            var users = configuration.GetSection("UserStore").GetChildren();
            foreach (var user in users)
            {
                var userConfig = user.Get<UserConfig>();
                this.users.Add(user.Key, userConfig);
            }
        }

        public Task<List<Claim>?> GetUserClaims(string issuer, string subject, List<string> scopes)
        {
            if (this.users.TryGetValue(subject, out var userConfig))
            {
                var claims = new List<Claim> { new Claim(JsonWebTokenClaim.Subject, subject) };
                foreach (var claim in userConfig.Claims)
                {
                    var claimValue = claim.Value.ToString();
                    if (claimValue != null)
                    {
                        var claimType = claim.Value.GetType().Name switch
                        {
                            "long" => ClaimValueTypes.Integer64,
                            "boolean" => ClaimValueTypes.Boolean,
                            _ => ClaimValueTypes.String
                        };
                        claims.Add(new Claim(claim.Key, claimValue, claimType));
                    }
                    return Task.FromResult<List<Claim>?>(claims);
                }
            }
            return Task.FromResult((List<Claim>?)null);
        }

        public Task<bool> IsUserActive(string issuer, string subject)
        {
            if (this.users.TryGetValue(subject ?? string.Empty, out var userConfig))
            {
                return Task.FromResult(userConfig.IsActive);
            }
            return Task.FromResult(false);
        }

        private class UserConfig
        {
            private Dictionary<string, object> claims = new();

            public Dictionary<string, object> Claims
            {
                get { return this.claims; }
                set
                {
                    foreach (var kvp in value)
                    {
                        // Configuration.Get<Dictionary<string, object>> returns all objects values as strings
                        // Transform booleans and numerics to native types
                        var stringValue = kvp.Value.ToString();
                        if (stringValue != null)
                        {
                            if (long.TryParse(stringValue, out var longValue))
                            {
                                value[kvp.Key] = longValue;
                            }
                            else if (bool.TryParse(stringValue, out var boolValue))
                            {
                                value[kvp.Key] = boolValue;
                            }
                        }
                    }
                    this.claims = value;
                }
            }
            public bool IsActive { get; set; }
        }
    }
}
