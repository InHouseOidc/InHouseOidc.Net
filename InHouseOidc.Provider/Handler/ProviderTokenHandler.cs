// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common;

namespace InHouseOidc.Provider.Handler
{
    internal class ProviderTokenHandler(IJsonWebTokenHandler jsonWebTokenHandler, IUtcNow utcNow) : IProviderToken
    {
        private readonly IJsonWebTokenHandler jsonWebTokenHandler = jsonWebTokenHandler;
        private readonly IUtcNow utcNow = utcNow;

        public async Task<string> GetProviderAccessToken(
            string clientId,
            TimeSpan expires,
            string issuer,
            List<string> scopes
        )
        {
            var utcNow = this.utcNow.UtcNow;
            var expiry = utcNow.UtcDateTime.Add(expires);
            return await this.jsonWebTokenHandler.GetAccessToken(clientId, expiry, issuer, scopes, null);
        }
    }
}
