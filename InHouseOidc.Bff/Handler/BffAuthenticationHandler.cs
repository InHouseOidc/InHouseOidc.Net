// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using System.Collections.Concurrent;
using System.Text.Encodings.Web;
using InHouseOidc.Bff.Type;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InHouseOidc.Bff.Handler
{
    internal class BffAuthenticationHandler
        : AuthenticationHandler<AuthenticationSchemeOptions>,
            IAuthenticationRequestHandler
    {
        private readonly ConcurrentDictionary<string, IEndpointHandler> endpointHandlerDictionary;

        public BffAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> authenticationSchemeOptions,
            ClientOptions clientOptions,
            ILoggerFactory loggerFactory,
            IServiceProvider serviceProvider,
            UrlEncoder urlEncoder
        )
            : base(authenticationSchemeOptions, loggerFactory, urlEncoder)
        {
            this.endpointHandlerDictionary = new();
            this.AddEndpointHandler(
                serviceProvider,
                clientOptions.LoginEndpointUri,
                typeof(IEndpointHandler<LoginHandler>)
            );
            this.AddEndpointHandler(
                serviceProvider,
                clientOptions.LogoutEndpointUri,
                typeof(IEndpointHandler<LogoutHandler>)
            );
            this.AddEndpointHandler(
                serviceProvider,
                clientOptions.UserInfoEndpointUri,
                typeof(IEndpointHandler<UserInfoHandler>)
            );
        }

        public async Task<bool> HandleRequestAsync()
        {
            var endpointHandler = this.ResolveEndpointHandler();
            if (endpointHandler == null)
            {
                return false;
            }
            return await endpointHandler.HandleRequest(this.Context);
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            throw new NotImplementedException();
        }

        private void AddEndpointHandler(IServiceProvider serviceProvider, Uri uri, System.Type handlerType)
        {
            var endpointHandler = (IEndpointHandler?)serviceProvider.GetService(handlerType);
            if (endpointHandler != null)
            {
                this.endpointHandlerDictionary.TryAdd(uri.OriginalString, endpointHandler);
            }
        }

        private IEndpointHandler? ResolveEndpointHandler()
        {
            var requestPath = this.Request.Path.Value;
            if (string.IsNullOrEmpty(requestPath))
            {
                return null;
            }
            if (!this.endpointHandlerDictionary.TryGetValue(requestPath, out var endpointHandler))
            {
                return null;
            }
            return endpointHandler;
        }
    }
}
