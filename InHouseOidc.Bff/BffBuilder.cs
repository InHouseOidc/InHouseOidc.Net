// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Bff.Handler;
using InHouseOidc.Bff.Resolver;
using InHouseOidc.Bff.Type;
using InHouseOidc.Discovery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace InHouseOidc.Bff
{
    /// <summary>
    /// Builds the services required to support client access to APIs secured with OIDC Provider.
    /// </summary>
    public class BffBuilder(IServiceCollection serviceCollection)
    {
        internal ClientOptions ClientOptions { get; } = new ClientOptions();
        internal IServiceCollection ServiceCollection { get; set; } = serviceCollection;

        /// <summary>
        /// Adds OIDC client support for a BFF API to make outgoing API calls.<br />
        /// A named HttpClient is added with a handler to automatically include an access token header in all requests.
        /// </summary>
        /// <param name="clientName">The named HttpClient requiring access tokens.</param>
        /// <returns><see cref="BffBuilder"/> so additional calls can be chained.</returns>
        public BffBuilder AddApiClient(string clientName)
        {
            // Configure
            if (!this.ClientOptions.BffApiClients.TryAdd(clientName, clientName))
            {
                throw new ArgumentException($"Duplicate client name: {clientName}", nameof(clientName));
            }
            return this;
        }

        /// <summary>
        /// Adds OIDC clients for multitenant BFF APIs.<br />
        /// Use either AddMultitenantOidcClients or SetOidcClient, not both.
        /// </summary>
        /// <param name="bffHostsClientOptions">Host names and configuration options for BFF OIDC clients.</param>
        /// <returns><see cref="BffBuilder"/> so additional calls can be chained.</returns>
        public BffBuilder AddMultitenantOidcClients(Dictionary<string, BffClientOptions> bffHostsClientOptions)
        {
            if (this.ClientOptions.BffClientOptions != null)
            {
                throw new InvalidOperationException("Use either AddMultitenantOidcClients or SetOidcClient, not both");
            }
            foreach (var (key, value) in bffHostsClientOptions)
            {
                this.ClientOptions.BffClientOptionsMultitenant.TryAdd(key, value);
            }
            return this;
        }

        /// <summary>
        /// Builds the final services for the client. Required as the final step of the client setup.
        /// </summary>
        public void Build()
        {
            this.ClientOptions.DiscoveryOptions.CacheTime = this.ClientOptions.DiscoveryOptions.CacheTime;
            this.ClientOptions.DiscoveryOptions.InternalHttpClientName = this.ClientOptions.InternalHttpClientName;
            this.ClientOptions.DiscoveryOptions.MaxRetryAttempts = this.ClientOptions.MaxRetryAttempts;
            this.ClientOptions.DiscoveryOptions.RetryDelayMilliseconds = this.ClientOptions.RetryDelayMilliseconds;
            this.ServiceCollection.AddSingleton(this.ClientOptions);
            this.ServiceCollection.AddHttpClient(this.ClientOptions.InternalHttpClientName);
            this.ServiceCollection.TryAddSingleton<IDiscoveryResolver, DiscoveryResolver>();
            JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();
            var authenticationBuilder = this.ServiceCollection.AddAuthentication(options =>
            {
                options.AddScheme<BffAuthenticationHandler>(BffConstant.AuthenticationSchemeBff, null);
            });
            authenticationBuilder.AddCookie(
                BffConstant.AuthenticationSchemeCookie,
                options =>
                {
                    options.AccessDeniedPath = null;
                    options.Cookie.HttpOnly = true;
                    options.Cookie.Name = this.ClientOptions.AuthenticationCookieName;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.Cookie.SameSite = SameSiteMode.Strict;
                    options.SlidingExpiration = false;
                }
            );
            if (this.ClientOptions.BffClientOptions == null)
            {
                foreach (var (hostname, bffClientOptions) in this.ClientOptions.BffClientOptionsMultitenant)
                {
                    this.AddOidcClient(authenticationBuilder, bffClientOptions, hostname, true);
                }
            }
            else
            {
                this.AddOidcClient(
                    authenticationBuilder,
                    this.ClientOptions.BffClientOptions,
                    OpenIdConnectDefaults.AuthenticationScheme,
                    false
                );
            }
            // Setup any BFF API clients added
            if (!this.ClientOptions.BffApiClients.IsEmpty)
            {
                this.ServiceCollection.TryAddSingleton<IBffAccessTokenResolver, BffAccessTokenResolver>();
                foreach (var clientName in this.ClientOptions.BffApiClients.Keys)
                {
                    // Add the HTTP client and bind the token handler
                    this.ServiceCollection.AddHttpClient(clientName).AddBffApiClientToken(clientName);
                }
            }
            this.ServiceCollection.AddAuthorizationBuilder()
                .AddPolicy(
                    BffConstant.BffApiPolicy,
                    authorizationPolicyBuilder =>
                    {
                        authorizationPolicyBuilder.AddAuthenticationSchemes(BffConstant.AuthenticationSchemeCookie);
                        authorizationPolicyBuilder.RequireAuthenticatedUser();
                    }
                );
            this.ServiceCollection.AddSingleton<IEndpointHandler<LoginHandler>, LoginHandler>();
            this.ServiceCollection.AddSingleton<IEndpointHandler<LogoutHandler>, LogoutHandler>();
            this.ServiceCollection.AddSingleton<IEndpointHandler<UserInfoHandler>, UserInfoHandler>();
            this.ServiceCollection.AddSingleton<
                IAuthorizationMiddlewareResultHandler,
                BffApiAuthorizationMiddlewareResultHandler
            >();
        }

        /// <summary>
        /// Enables validation that discovery grant types are provided.  Optional (defaults to true).
        /// </summary>
        /// <param name="enable">Enable or disable validation.</param>
        /// <returns><see cref="BffBuilder"/> so additional calls can be chained.</returns>
        public BffBuilder EnableDiscoveryGrantTypeValidation(bool enable)
        {
            this.ClientOptions.DiscoveryOptions.ValidateGrantTypes = enable;
            return this;
        }

        /// <summary>
        /// Enables validation that discovery issuer matches OIDC provider.  Optional (defaults to true).
        /// </summary>
        /// <param name="enable">Enable or disable validation.</param>
        /// <returns><see cref="BffBuilder"/> so additional calls can be chained.</returns>
        public BffBuilder EnableDiscoveryIssuerValidation(bool enable)
        {
            this.ClientOptions.DiscoveryOptions.ValidateIssuer = enable;
            return this;
        }

        /// <summary>
        /// Enables getting additional claims from the OIDC provider.  Optional (defaults to false).
        /// </summary>
        /// <param name="enable">Enable or disable additional claims retrieval.</param>
        /// <returns><see cref="BffBuilder"/> so additional calls can be chained.</returns>
        public BffBuilder EnableGetClaimsFromUserInfoEndpoint(bool enable)
        {
            this.ClientOptions.GetClaimsFromUserInfoEndpoint = enable;
            return this;
        }

        /// <summary>
        /// Sets the cookie name to issue for BFF authentication.  Optional (defaults to "InHouseOidc.Bff").<br />
        /// </summary>
        /// <param name="cookieName">The cookie name to use for BFF authentication.</param>
        /// <returns><see cref="BffBuilder"/> so additional calls can be chained.</returns>
        public BffBuilder SetAuthenticationCookieName(string cookieName)
        {
            this.ClientOptions.AuthenticationCookieName = cookieName;
            return this;
        }

        /// <summary>
        /// Sets the path used for OIDC callbacks.  Optional (defaults to "/api/auth/callback").
        /// </summary>
        /// <param name="callbackPath">The path the endpoint will use.</param>
        /// <returns><see cref="BffBuilder"/> so additional calls can be chained.</returns>
        public BffBuilder SetCallbackPath(string callbackPath)
        {
            this.ClientOptions.CallbackPath = callbackPath;
            return this;
        }

        /// <summary>
        /// Sets time to cache discovery information.  Optional (defaults to 30 minutes).
        /// </summary>
        /// <param name="discoveryCacheTime">The TimeSpan to cache for.</param>
        /// <returns><see cref="BffBuilder"/> so additional calls can be chained.</returns>
        public BffBuilder SetDiscoveryCacheTime(TimeSpan discoveryCacheTime)
        {
            this.ClientOptions.DiscoveryOptions.CacheTime = discoveryCacheTime;
            return this;
        }

        /// <summary>
        /// Sets HttpClient name to use for internal operations.  Optional (defaults to "InHouseOidc.HttpClient").
        /// </summary>
        /// <param name="internalHttpClientName">The HttpClient name.</param>
        /// <returns><see cref="BffBuilder"/> so additional calls can be chained.</returns>
        public BffBuilder SetInternalHttpClientName(string internalHttpClientName)
        {
            this.ClientOptions.InternalHttpClientName = internalHttpClientName;
            this.ServiceCollection.AddHttpClient(internalHttpClientName);
            return this;
        }

        /// <summary>
        /// Sets the path for initiating a login.  Optional (defaults to "/api/auth/login").
        /// </summary>
        /// <param name="loginPath">The path the endpoint will use.</param>
        /// <returns><see cref="BffBuilder"/> so additional calls can be chained.</returns>
        public BffBuilder SetLoginPath(string loginPath)
        {
            this.ClientOptions.LoginEndpointUri = new Uri(loginPath, UriKind.Relative);
            return this;
        }

        /// <summary>
        /// Sets the path for initiating a logout.  Optional (defaults to "/api/auth/logout").
        /// </summary>
        /// <param name="logoutPath">The path the endpoint will use.</param>
        /// <returns><see cref="BffBuilder"/> so additional calls can be chained.</returns>
        public BffBuilder SetLogoutPath(string logoutPath)
        {
            this.ClientOptions.LogoutEndpointUri = new Uri(logoutPath, UriKind.Relative);
            return this;
        }

        /// <summary>
        /// Sets the maximum retry attempts to make when making provider requests.  Optional (defaults to 5).
        /// </summary>
        /// <param name="maxRetryAttempts">The maximum retry attempts.</param>
        /// <returns><see cref="BffBuilder"/> so additional calls can be chained.</returns>
        public BffBuilder SetMaxRetryAttempts(int maxRetryAttempts)
        {
            this.ClientOptions.MaxRetryAttempts = maxRetryAttempts;
            return this;
        }

        /// <summary>
        /// Sets the claim type used to source the identity name.  Optional (defaults to "name").
        /// </summary>
        /// <param name="nameClaimType">The claim type.</param>
        /// <returns><see cref="BffBuilder"/> so additional calls can be chained.</returns>
        public BffBuilder SetNameClaimType(string nameClaimType)
        {
            this.ClientOptions.NameClaimType = nameClaimType;
            return this;
        }

        /// <summary>
        /// Sets the OIDC client for a single tenant BFF API.<br/>
        /// Use either SetOidcClient or AddMultitenantOidcClients, not both.
        /// </summary>
        /// <param name="bffClientOptions">Configuration options for the BFF API client.</param>
        /// <returns><see cref="BffBuilder"/> so additional calls can be chained.</returns>
        public BffBuilder SetOidcClient(BffClientOptions bffClientOptions)
        {
            if (!this.ClientOptions.BffClientOptionsMultitenant.IsEmpty)
            {
                throw new InvalidOperationException("Use either SetOidcClient or AddMultitenantOidcClients, not both");
            }
            this.ClientOptions.BffClientOptions = bffClientOptions;
            return this;
        }

        /// <summary>
        /// Sets the URL to redirect to post logout (absolute or relative URL).  Optional (defaults to "/").
        /// </summary>
        /// <param name="address">The address to use.</param>
        /// <returns><see cref="BffBuilder"/> so additional calls can be chained.</returns>
        public BffBuilder SetPostLogoutRedirectAddress(string address)
        {
            this.ClientOptions.PostLogoutRedirectAddress = address;
            return this;
        }

        /// <summary>
        /// Sets the base delay time to use between retryable provider requests.  Optional (defaults to 50 milliseconds).<br />
        /// Each progressive retry doubles the delay time between attempts, e.g. 50ms, 100ms, 200ms, 400ms, 800ms.
        /// </summary>
        /// <param name="retryDelayMilliseconds">The retry base time in milliseconds.</param>
        /// <returns><see cref="BffBuilder"/> so additional calls can be chained.</returns>
        public BffBuilder SetRetryDelayMilliseconds(int retryDelayMilliseconds)
        {
            this.ClientOptions.RetryDelayMilliseconds = retryDelayMilliseconds;
            return this;
        }

        /// <summary>
        /// Sets the claim type used to source the identity roles.  Optional (defaults to "role").
        /// </summary>
        /// <param name="roleClaimType">The claim type.</param>
        /// <returns><see cref="BffBuilder"/> so additional calls can be chained.</returns>
        public BffBuilder SetRoleClaimType(string roleClaimType)
        {
            this.ClientOptions.RoleClaimType = roleClaimType;
            return this;
        }

        /// <summary>
        /// Sets the dictionary of claims to map from JSON returned from the UserInfo endpoint.  Optional.<br />
        /// </summary>
        /// <param name="uniqueClaimMappings">The unique claims to map.</param>
        /// <returns><see cref="BffBuilder"/> so additional calls can be chained.</returns>
        public BffBuilder SetUniqueClaimMappings(Dictionary<string, string> uniqueClaimMappings)
        {
            this.ClientOptions.UniqueClaimMappings = uniqueClaimMappings;
            return this;
        }

        /// <summary>
        /// Sets the path for accessing user information from the front-end.  Optional (defaults to "/api/auth/user-info").
        /// </summary>
        /// <param name="userInfoPath">The path the endpoint will use.</param>
        /// <returns><see cref="BffBuilder"/> so additional calls can be chained.</returns>
        public BffBuilder SetUserInfoPath(string userInfoPath)
        {
            this.ClientOptions.UserInfoEndpointUri = new Uri(userInfoPath, UriKind.Relative);
            return this;
        }

        private void AddOidcClient(
            AuthenticationBuilder authenticationBuilder,
            BffClientOptions bffClientOptions,
            string schemeName,
            bool useCallbackPathSuffix
        )
        {
            authenticationBuilder.AddOpenIdConnect(
                schemeName,
                options =>
                {
                    options.AccessDeniedPath = null;
                    options.Authority = bffClientOptions.OidcProviderAddress;
                    options.CallbackPath = useCallbackPathSuffix
                        ? $"{this.ClientOptions.CallbackPath}/{schemeName.Replace('.', '-')}"
                        : this.ClientOptions.CallbackPath;
                    foreach (var uniqueClaimMapping in this.ClientOptions.UniqueClaimMappings)
                    {
                        options.ClaimActions.MapUniqueJsonKey(uniqueClaimMapping.Key, uniqueClaimMapping.Value);
                    }
                    options.ClientId = bffClientOptions.ClientId;
                    options.ClientSecret = bffClientOptions.ClientSecret;
                    options.GetClaimsFromUserInfoEndpoint = this.ClientOptions.GetClaimsFromUserInfoEndpoint;
                    options.RequireHttpsMetadata = bffClientOptions.OidcProviderAddress.StartsWith("https://");
                    options.ResponseMode = OpenIdConnectResponseMode.Query;
                    options.ResponseType = OpenIdConnectResponseType.Code;
                    options.SaveTokens = true;
                    var scopes = bffClientOptions.Scope?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
                    foreach (var scope in scopes)
                    {
                        options.Scope.Add(scope);
                    }
                    options.SignInScheme = BffConstant.AuthenticationSchemeCookie;
                    options.SignedOutCallbackPath = useCallbackPathSuffix
                        ? $"{this.ClientOptions.SignedOutCallbackPath}/{schemeName.Replace('.', '-')}"
                        : this.ClientOptions.SignedOutCallbackPath;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = this.ClientOptions.NameClaimType,
                        RoleClaimType = this.ClientOptions.RoleClaimType,
                        ValidateIssuer = true,
                        ValidateIssuerSigningKey = true,
                        ValidateLifetime = true,
                        ValidIssuer = bffClientOptions.OidcProviderAddress,
                    };
                    options.UsePkce = true;
                    // Authentication cookie expiry is sourced from id_token expiry
                    options.UseTokenLifetime = true;
                }
            );
        }
    }
}
