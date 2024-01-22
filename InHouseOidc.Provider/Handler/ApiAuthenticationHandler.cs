// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Provider.Constant;
using InHouseOidc.Provider.Extension;
using InHouseOidc.Provider.Type;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

namespace InHouseOidc.Provider.Handler
{
    internal class ApiAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly ApiAuthenticationOptions apiAuthenticationOptions;
        private readonly IValidationHandler validationHandler;

        public ApiAuthenticationHandler(
            ApiAuthenticationOptions apiAuthenticationOptions,
            IOptionsMonitor<AuthenticationSchemeOptions> authenticationSchemeOptions,
            ILoggerFactory logger,
            ISystemClock systemClock,
            UrlEncoder urlEncoder,
            IValidationHandler validationHandler
        ) : base(authenticationSchemeOptions, logger, urlEncoder, systemClock)
        {
            this.apiAuthenticationOptions = apiAuthenticationOptions;
            this.validationHandler = validationHandler;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Check for the bearer authorisation header
            string? authorisationHeader = this.Request.Headers[ApiConstant.Authorization];
            if (
                string.IsNullOrEmpty(authorisationHeader)
                || !authorisationHeader.StartsWith(ApiConstant.Bearer, StringComparison.InvariantCultureIgnoreCase)
            )
            {
                return AuthenticateResult.NoResult();
            }
            // Validate the token
            var token = authorisationHeader[ApiConstant.Bearer.Length..];
            if (token.Trim().Length == 0)
            {
                return AuthenticateResult.NoResult();
            }
            var issuer = this.Request.GetBaseUriString();
            var claimsPrincipal = await this.validationHandler.ValidateJsonWebToken(
                this.apiAuthenticationOptions.Audience,
                issuer,
                token,
                true
            );
            if (claimsPrincipal == null)
            {
                return AuthenticateResult.NoResult();
            }
            // Return a ticket
            var authenticationTicket = new AuthenticationTicket(claimsPrincipal, this.Scheme.Name);
            return AuthenticateResult.Success(authenticationTicket);
        }
    }
}
