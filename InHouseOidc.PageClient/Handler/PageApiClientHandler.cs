// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common.Constant;
using InHouseOidc.PageClient.Resolver;
using System.Net.Http.Headers;

namespace InHouseOidc.PageClient.Handler
{
    internal class PageApiClientHandler : DelegatingHandler
    {
        private readonly IPageAccessTokenResolver pageAccessTokenResolver;
        private readonly string clientName;

        public PageApiClientHandler(IPageAccessTokenResolver pageAccessTokenResolver, string clientName)
        {
            this.pageAccessTokenResolver = pageAccessTokenResolver;
            this.clientName = clientName;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage httpRequestMessage,
            CancellationToken cancellationToken
        )
        {
            var accessToken = await this.pageAccessTokenResolver.GetClientToken(this.clientName, cancellationToken);
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
