// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Provider.Constant;
using InHouseOidc.Provider.Exception;
using InHouseOidc.Provider.Extension;
using InHouseOidc.Provider.Type;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text.Encodings.Web;

namespace InHouseOidc.Provider.Handler
{
    internal class ProviderAuthenticationHandler
        : AuthenticationHandler<AuthenticationSchemeOptions>,
            IAuthenticationRequestHandler
    {
        private readonly ILoggerFactory loggerFactory;
        private readonly ProviderOptions providerOptions;
        private readonly ConcurrentDictionary<string, IEndpointHandler> endpointHandlerDictionary;

        public ProviderAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> authenticationSchemeOptions,
            ILoggerFactory loggerFactory,
            ProviderOptions providerOptions,
            IServiceProvider serviceProvider,
            UrlEncoder urlEncoder,
            ISystemClock systemClock
        ) : base(authenticationSchemeOptions, loggerFactory, urlEncoder, systemClock)
        {
            this.loggerFactory = loggerFactory;
            this.providerOptions = providerOptions;
            this.endpointHandlerDictionary = new();
            if (providerOptions.GrantTypes.Contains(GrantType.AuthorizationCode))
            {
                this.AddEndpointHandler(
                    serviceProvider,
                    providerOptions.AuthorizationEndpointUri,
                    typeof(IEndpointHandler<AuthorizationHandler>)
                );
                this.AddEndpointHandler(
                    serviceProvider,
                    providerOptions.EndSessionEndpointUri,
                    typeof(IEndpointHandler<EndSessionHandler>)
                );
            }
            if (providerOptions.CheckSessionEndpointEnabled)
            {
                this.AddEndpointHandler(
                    serviceProvider,
                    providerOptions.CheckSessionEndpointUri,
                    typeof(IEndpointHandler<CheckSessionHandler>)
                );
            }
            this.AddEndpointHandler(
                serviceProvider,
                providerOptions.DiscoveryEndpointUri,
                typeof(IEndpointHandler<DiscoveryHandler>)
            );
            this.AddEndpointHandler(
                serviceProvider,
                providerOptions.JsonWebKeySetUri,
                typeof(IEndpointHandler<JsonWebKeySetHandler>)
            );
            this.AddEndpointHandler(
                serviceProvider,
                providerOptions.TokenEndpointUri,
                typeof(IEndpointHandler<TokenHandler>)
            );
            if (providerOptions.UserInfoEndpointEnabled)
            {
                this.AddEndpointHandler(
                    serviceProvider,
                    providerOptions.UserInfoEndpointUri,
                    typeof(IEndpointHandler<UserInfoHandler>)
                );
            }
        }

        public async Task<bool> HandleRequestAsync()
        {
            // See if it's an endpoint we're handling
            var endpointHandler = this.ResolveEndpointHandler();
            if (endpointHandler == null)
            {
                return false;
            }
            bool result;
            try
            {
                result = await endpointHandler.HandleRequest(this.Request);
            }
            catch (BadRequestException badRequestException)
            {
                // Log the bad request details locally and return a 400 to the caller
                this.LogMessageException(endpointHandler, badRequestException);
                await this.Context.Response.WriteBadRequestContent(badRequestException);
                return true;
            }
            catch (InternalErrorException internalErrorException)
            {
                // Log the internal error details locally and return a 500 to the caller
                this.LogMessageException(endpointHandler, internalErrorException);
                await this.Context.Response.WriteInternalErrorContent();
                return true;
            }
            catch (RedirectErrorException redirectErrorException)
            {
                // Log the redirect errors details locally and return a 302 to the caller
                this.LogMessageException(endpointHandler, redirectErrorException);
                this.Context.Response.WriteRedirect(redirectErrorException);
                return true;
            }
            catch (System.Exception exception)
            {
                // Log the unhandled exception details locally and return a 500 to the caller
                var logger = this.loggerFactory.CreateLogger(endpointHandler.GetType());
                logger.Log(
                    this.providerOptions.LogFailuresAsInformation ? LogLevel.Information : LogLevel.Error,
                    exception,
                    ExceptionConstant.UnhandledException
                );
                await this.Context.Response.WriteInternalErrorContent();
                return true;
            }
            return result;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            throw new NotImplementedException();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Usage",
            "CA2254:Template should be a static expression",
            Justification = "Log template passed via exception properties"
        )]
        private void LogMessageException(IEndpointHandler endpointHandler, LogMessageException logMessageException)
        {
            var logger = this.loggerFactory.CreateLogger(endpointHandler.GetType());
            var logLevel = this.providerOptions.LogFailuresAsInformation ? LogLevel.Information : LogLevel.Error;
            logger.Log(logLevel, logMessageException.LogMessage, logMessageException.Args);
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

        private void AddEndpointHandler(IServiceProvider serviceProvider, Uri uri, System.Type handlerType)
        {
            var endpointHandler = (IEndpointHandler?)serviceProvider.GetService(handlerType);
            if (endpointHandler != null)
            {
                this.endpointHandlerDictionary.TryAdd(uri.OriginalString, endpointHandler);
            }
        }
    }
}
