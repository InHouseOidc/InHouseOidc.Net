// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace InHouseOidc.Test.Common
{
    public class TestMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage RequestMessage { get; set; } = new();
        public HttpResponseMessage ResponseMessage { get; set; } = new();
        public Exception? ThrowException { get; set; }
        public int SendCount { get; private set; } = 0;

        public void Clear()
        {
            this.RequestMessage = new();
            this.ResponseMessage = new();
            this.ThrowException = null;
            this.SendCount = 0;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            if (this.ThrowException != null)
            {
                throw this.ThrowException;
            }
            this.RequestMessage = request;
            this.SendCount++;
            return Task.FromResult(this.ResponseMessage);
        }
    }
}
