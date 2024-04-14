// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common.Constant;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace InHouseOidc.PageClient
{
    /// <summary>
    /// Specifies the options that configure a web page client.
    /// </summary>
    public class PageClientOptions
    {
        /// <summary>
        /// The page client cookie authentication access denied path.<br />
        /// Optional (defaults to CookieAuthenticationDefaults.AccessDeniedPath).
        /// Only applies when IssueLocalAuthenticationCookie = true.<br />
        /// </summary>
        public string AccessDeniedPath { get; init; } = CookieAuthenticationDefaults.AccessDeniedPath;

        /// <summary>
        /// The path used for the callback with the authorisation code.<br />
        /// Optional (defaults to "/connect/authorize/callback").
        /// </summary>
        public string CallbackPath { get; init; } = PageConstant.CallbackPath;

        /// <summary>
        /// The client identifier registered at the OIDC Provider. Required.
        /// </summary>
        public string? ClientId { get; init; }

        /// <summary>
        /// The cookie name to issue for page client authentication.  Optional (defaults to the ClientId).<br />
        /// Only applies when IssueLocalAuthenticationCookie = true.<br />
        /// </summary>
        public string? CookieName { get; init; }

        /// <summary>
        /// Get additional user claims from the OIDC Provider.  Optional (defaults to false).
        /// </summary>
        public bool GetClaimsFromUserInfoEndpoint { get; init; } = false;

        /// <summary>
        /// Issue a local authentication cookie after successfully authenticating with the OIDC Provider.<br />
        /// Optional (defaults to true).<br />
        /// Note: Must be false if the page client is also the OIDC Provider.
        /// </summary>
        public bool IssueLocalAuthenticationCookie { get; init; } = true;

        /// <summary>
        /// The path to the login page.  Optional (defaults to CookieAuthenticationDefaults.LoginPath).<br />
        /// Only applies when IssueLocalAuthenticationCookie = true.<br />
        /// </summary>
        public string LoginPath { get; init; } = CookieAuthenticationDefaults.LoginPath;

        /// <summary>
        /// The path to the logout page.  Optional (defaults to CookieAuthenticationDefaults.LogoutPath).<br />
        /// Only applies when IssueLocalAuthenticationCookie = true.<br />
        /// </summary>
        public string LogoutPath { get; init; } = CookieAuthenticationDefaults.LogoutPath;

        /// <summary>
        /// The claim type used to source the identity name.  Optional (defaults to "name").
        /// </summary>
        public string NameClaimType { get; init; } = JsonWebTokenClaim.Name;

        /// <summary>
        /// The base address (in full URL form) for the OIDC Provider.  Required.
        /// </summary>
        public string? OidcProviderAddress { get; init; }

        /// <summary>
        /// The claim type used to source the identity roles.  Optional (defaults to "role").
        /// </summary>
        public string RoleClaimType { get; init; } = JsonWebTokenClaim.Role;

        /// <summary>
        /// The scope (single) or scopes (space separated) requested to be included in the access token.  Required.
        /// </summary>
        public string? Scope { get; init; }

        /// <summary>
        /// A dictionary of claims to map from JSON returned from the UserInfo endpoint.  Optional.<br />
        /// Stored as key (claim name) value (JSON property name) pairs.
        /// </summary>
        public Dictionary<string, string> UniqueClaimMappings { get; set; } = [];
    }
}
