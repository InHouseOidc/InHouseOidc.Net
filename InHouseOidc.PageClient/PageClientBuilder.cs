// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Discovery;
using InHouseOidc.PageClient.Resolver;
using InHouseOidc.PageClient.Type;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace InHouseOidc.PageClient
{
    /// <summary>
    /// Builds the services required to support client access to APIs secured with OIDC Provider.
    /// </summary>
    public class PageClientBuilder(IServiceCollection serviceCollection)
    {
        internal ClientOptions ClientOptions { get; } = new ClientOptions();
        internal IServiceCollection ServiceCollection { get; set; } = serviceCollection;

        /// <summary>
        /// Adds OIDC client support for an MVC or Razor page based website outgoing API calls.<br />
        /// A named HttpClient is added with a handler to automatically include an access token header in all requests.
        /// </summary>
        /// <param name="clientName">The named HttpClient requiring access tokens.</param>
        /// <returns>ProviderBuilder.</returns>
        public PageClientBuilder AddApiClient(string clientName)
        {
            // Configure
            if (!this.ClientOptions.PageApiClients.TryAdd(clientName, clientName))
            {
                throw new ArgumentException($"Duplicate client name: {clientName}", nameof(clientName));
            }
            return this;
        }

        /// <summary>
        /// Adds OIDC client support for a MVC or Razor page based website.
        /// </summary>
        /// <param name="pageClientOptions">Configuration options for the MVC / Razor page client.</param>
        /// <returns>ProviderBuilder.</returns>
        public PageClientBuilder AddClient(PageClientOptions pageClientOptions)
        {
            if (this.ClientOptions.PageClientOptions != null)
            {
                throw new ArgumentException("AddOidcPageClient can only be called once");
            }
            this.ClientOptions.PageClientOptions = pageClientOptions;
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
            // Setup any page API clients added
            if (!this.ClientOptions.PageApiClients.IsEmpty)
            {
                this.ServiceCollection.TryAddSingleton<IPageAccessTokenResolver, PageAccessTokenResolver>();
                foreach (var clientName in this.ClientOptions.PageApiClients.Keys)
                {
                    // Add the HTTP client and bind the token handler
                    this.ServiceCollection.AddHttpClient(clientName).AddPageApiClientToken(clientName);
                }
            }
            // Setup page client
            if (this.ClientOptions.PageClientOptions != null)
            {
                JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();
                var authenticationBuilder = this.ServiceCollection.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = PageConstant.AuthenticationSchemeCookie;
                    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                });
                if (this.ClientOptions.PageClientOptions.IssueLocalAuthenticationCookie)
                {
                    authenticationBuilder.AddCookie(
                        PageConstant.AuthenticationSchemeCookie,
                        options =>
                        {
                            options.AccessDeniedPath = this.ClientOptions.PageClientOptions.AccessDeniedPath;
                            options.Cookie.Name =
                                this.ClientOptions.PageClientOptions.CookieName
                                ?? this.ClientOptions.PageClientOptions.ClientId;
                            options.LoginPath = this.ClientOptions.PageClientOptions.LoginPath;
                            options.LogoutPath = this.ClientOptions.PageClientOptions.LogoutPath;
                            options.SlidingExpiration = false;
                        }
                    );
                }
                authenticationBuilder.AddOpenIdConnect(options =>
                {
                    options.Authority = this.ClientOptions.PageClientOptions.OidcProviderAddress;
                    options.CallbackPath = this.ClientOptions.PageClientOptions.CallbackPath;
                    foreach (var uniqueClaimMapping in this.ClientOptions.PageClientOptions.UniqueClaimMappings)
                    {
                        options.ClaimActions.MapUniqueJsonKey(uniqueClaimMapping.Key, uniqueClaimMapping.Value);
                    }
                    options.ClientId = this.ClientOptions.PageClientOptions.ClientId;
                    options.ClientSecret = this.ClientOptions.PageClientOptions.ClientSecret;
                    options.GetClaimsFromUserInfoEndpoint = this.ClientOptions
                        .PageClientOptions
                        .GetClaimsFromUserInfoEndpoint;
                    options.RequireHttpsMetadata =
                        this.ClientOptions.PageClientOptions.OidcProviderAddress?.StartsWith("https://") ?? true;
                    options.ResponseMode = OpenIdConnectResponseMode.Query;
                    options.ResponseType = OpenIdConnectResponseType.Code;
                    options.SaveTokens = true;
                    var scopes = this.ClientOptions.PageClientOptions.Scope?.Split(' ');
                    if (scopes != null && scopes.Length > 0)
                    {
                        foreach (var scope in scopes)
                        {
                            options.Scope.Add(scope);
                        }
                    }
                    options.SignInScheme = PageConstant.AuthenticationSchemeCookie;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = this.ClientOptions.PageClientOptions.NameClaimType,
                        RoleClaimType = this.ClientOptions.PageClientOptions.RoleClaimType,
                        ValidateIssuer = true,
                        ValidateIssuerSigningKey = true,
                        ValidateLifetime = true,
                        ValidIssuer = this.ClientOptions.PageClientOptions.OidcProviderAddress,
                    };
                    options.UsePkce = true;
                    // Authentication cookie expiry is sourced from id_token expiry
                    options.UseTokenLifetime = true;
                });
            }
        }

        /// <summary>
        /// Enables validation that discovery grant types are provided.  Optional (defaults to true).
        /// </summary>
        /// <param name="enable">Enable or disable validation.</param>
        /// <returns><see cref="PageClientBuilder"/> so additional calls can be chained.</returns>
        public PageClientBuilder EnableDiscoveryGrantTypeValidation(bool enable)
        {
            this.ClientOptions.DiscoveryOptions.ValidateGrantTypes = enable;
            return this;
        }

        /// <summary>
        /// Enables validation that discovery issuer matches OIDC provider.  Optional (defaults to true).
        /// </summary>
        /// <param name="enable">Enable or disable validation.</param>
        /// <returns><see cref="PageClientBuilder"/> so additional calls can be chained.</returns>
        public PageClientBuilder EnableDiscoveryIssuerValidation(bool enable)
        {
            this.ClientOptions.DiscoveryOptions.ValidateIssuer = enable;
            return this;
        }

        /// <summary>
        /// Sets time to cache discovery information.  Optional (defaults to 30 minutes).
        /// </summary>
        /// <param name="discoveryCacheTime">The TimeSpan to cache for.</param>
        /// <returns><see cref="PageClientBuilder"/> so additional calls can be chained.</returns>
        public PageClientBuilder SetDiscoveryCacheTime(TimeSpan discoveryCacheTime)
        {
            this.ClientOptions.DiscoveryOptions.CacheTime = discoveryCacheTime;
            return this;
        }

        /// <summary>
        /// Sets HttpClient name to use for internal operations.  Optional (defaults to "InHouseOidc.HttpClient").
        /// </summary>
        /// <param name="internalHttpClientName">The HttpClient name.</param>
        /// <returns><see cref="PageClientBuilder"/> so additional calls can be chained.</returns>
        public PageClientBuilder SetInternalHttpClientName(string internalHttpClientName)
        {
            this.ClientOptions.InternalHttpClientName = internalHttpClientName;
            this.ServiceCollection.AddHttpClient(internalHttpClientName);
            return this;
        }

        /// <summary>
        /// Sets the maximum retry attempts to make when making provider requests.  Optional (defaults to 5).
        /// </summary>
        /// <param name="maxRetryAttempts">The maximum retry attempts.</param>
        /// <returns><see cref="PageClientBuilder"/> so additional calls can be chained.</returns>
        public PageClientBuilder SetMaxRetryAttempts(int maxRetryAttempts)
        {
            this.ClientOptions.MaxRetryAttempts = maxRetryAttempts;
            return this;
        }

        /// <summary>
        /// Sets the base delay time to use between retryable provider requests.  Optional (defaults to 50 milliseconds).<br />
        /// Each progressive retry doubles the delay time between attempts, e.g. 50ms, 100ms, 200ms, 400ms, 800ms.
        /// </summary>
        /// <param name="retryDelayMilliseconds">The retry base time in milliseconds.</param>
        /// <returns><see cref="PageClientBuilder"/> so additional calls can be chained.</returns>
        public PageClientBuilder SetRetryDelayMilliseconds(int retryDelayMilliseconds)
        {
            this.ClientOptions.RetryDelayMilliseconds = retryDelayMilliseconds;
            return this;
        }
    }
}
