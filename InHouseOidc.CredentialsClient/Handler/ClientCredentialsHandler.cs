// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common.Constant;
using System.Net.Http.Headers;

namespace InHouseOidc.CredentialsClient.Handler
{
    internal class ClientCredentialsHandler : DelegatingHandler
    {
        private readonly IClientCredentialsResolver clientCredentialsResolver;
        private readonly string clientName;

        public ClientCredentialsHandler(IClientCredentialsResolver clientCredentialsResolver, string clientName)
        {
            this.clientCredentialsResolver = clientCredentialsResolver;
            this.clientName = clientName;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage httpRequestMessage,
            CancellationToken cancellationToken
        )
        {
            await this.SetAuthorisationHeader(httpRequestMessage, cancellationToken);
            var response = await base.SendAsync(httpRequestMessage, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await this.clientCredentialsResolver.ClearClientToken(this.clientName);
                await this.SetAuthorisationHeader(httpRequestMessage, cancellationToken);
                response = await base.SendAsync(httpRequestMessage, cancellationToken);
            }
            return response;
        }

        private async Task SetAuthorisationHeader(
            HttpRequestMessage httpRequestMessage,
            CancellationToken cancellationToken
        )
        {
            var token = await this.clientCredentialsResolver.GetClientToken(this.clientName, cancellationToken);
            if (token != null)
            {
                httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue(
                    JsonWebTokenConstant.Bearer,
                    token
                );
            }
        }
    }
}
