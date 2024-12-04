// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using System.Net.Http.Headers;
using InHouseOidc.Bff.Resolver;
using InHouseOidc.Common.Constant;

namespace InHouseOidc.Bff.Handler
{
    internal class BffApiClientHandler(IBffAccessTokenResolver bffAccessTokenResolver, string clientName)
        : DelegatingHandler
    {
        private readonly IBffAccessTokenResolver bffAccessTokenResolver = bffAccessTokenResolver;
        private readonly string clientName = clientName;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage httpRequestMessage,
            CancellationToken cancellationToken
        )
        {
            var accessToken = await this.bffAccessTokenResolver.GetClientToken(this.clientName, cancellationToken);
            if (accessToken != null)
            {
                httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue(
                    JsonWebTokenConstant.Bearer,
                    accessToken
                );
            }
            var response = await base.SendAsync(httpRequestMessage, cancellationToken);
            return response;
        }
    }
}
