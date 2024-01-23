// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Provider.Extension;
using InHouseOidc.Provider.Handler;
using InHouseOidc.Provider.Type;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography.X509Certificates;

namespace InHouseOidc.Provider
{
    /// <summary>
    /// Builds the services required to support an OIDC Provider.
    /// </summary>
    public class ProviderBuilder
    {
        internal ProviderOptions ProviderOptions { get; } = new ProviderOptions();
        internal IServiceCollection ServiceCollection { get; set; }

        public ProviderBuilder(IServiceCollection serviceCollection)
        {
            this.ServiceCollection = serviceCollection;
        }

        /// <summary>
        /// Builds the final services for the provider. Required as the final step of the provider setup.
        /// </summary>
        public void Build()
        {
            var authenticationBuilder = this.ServiceCollection.AddAuthentication(authenticationOptions =>
            {
                authenticationOptions.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                authenticationOptions.AddScheme<ProviderAuthenticationHandler>(
                    ProviderConstant.AuthenticationScheme,
                    null
                );
            });
            authenticationBuilder.AddCookie(
                CookieAuthenticationDefaults.AuthenticationScheme,
                options =>
                {
                    options.AccessDeniedPath = this.ProviderOptions.ErrorPath;
                    options.Cookie.Name = this.ProviderOptions.AuthenticationCookieName;
                    options.LoginPath = this.ProviderOptions.LoginPath;
                    options.LogoutPath = this.ProviderOptions.LogoutPath;
                    options.SlidingExpiration = false;
                }
            );
            this.ServiceCollection.AddSingleton(this.ProviderOptions);
        }

        /// <summary>
        /// Enables the authorisation code flow with .<br />
        /// See <see href="https://datatracker.ietf.org/doc/html/rfc6749#section-4.1"></see>
        /// See <see href="https://datatracker.ietf.org/doc/html/rfc7636"></see>
        /// Services must added to support this flow that implement IClientStore, ICodeStore, IResourceStore and IUserStore.
        /// </summary>
        /// <param name="requirePkce">Indicates whether only the Proof Key for Code Exchange (PKCE) mode is supported.<br />
        /// Optional (defaults to true).</param>
        /// <returns><see cref="ProviderBuilder"/> so additional calls can be chained.</returns>
        public ProviderBuilder EnableAuthorizationCodeFlow(bool requirePkce = true)
        {
            this.ServiceCollection.AddScoped<IEndpointHandler<AuthorizationHandler>, AuthorizationHandler>();
            this.ServiceCollection.AddScoped<IEndpointHandler<EndSessionHandler>, EndSessionHandler>();
            this.ServiceCollection.AddSingleton<IProviderSession, ProviderSessionHandler>();
            this.ProviderOptions.GrantTypes.Add(GrantType.AuthorizationCode);
            this.ProviderOptions.AuthorizationCodePkceRequired = requirePkce;
            return this;
        }

        /// <summary>
        /// Enables the check session endpoint.  Optional (disabled by default).<br />
        /// See <see href="https://openid.net/specs/openid-connect-session-1_0.html#OPiframe"></see>.
        /// </summary>
        /// <returns><see cref="ProviderBuilder"/> so additional calls can be chained.</returns>
        public ProviderBuilder EnableCheckSessionEndpoint()
        {
            this.ServiceCollection.AddSingleton<IEndpointHandler<CheckSessionHandler>, CheckSessionHandler>();
            this.ProviderOptions.CheckSessionEndpointEnabled = true;
            return this;
        }

        /// <summary>
        /// Enables the client credentials flow.<br />
        /// See <see href="https://datatracker.ietf.org/doc/html/rfc6749#section-4.4"></see>
        /// Services must added to support this flow that implement IClientStore and IResourceStore.
        /// </summary>
        /// <returns><see cref="ProviderBuilder"/> so additional calls can be chained.</returns>
        public ProviderBuilder EnableClientCredentialsFlow()
        {
            this.ServiceCollection.AddSingleton<IProviderToken, ProviderTokenHandler>();
            this.ProviderOptions.GrantTypes.Add(GrantType.ClientCredentials);
            return this;
        }

        /// <summary>
        /// Enables the refresh token grant.<br />
        /// See <see href="https://datatracker.ietf.org/doc/html/rfc6749#section-6"></see>
        /// Refresh tokens apply only with the AuthorizationCode flow grant enabled.
        /// </summary>
        /// <returns><see cref="ProviderBuilder"/> so additional calls can be chained.</returns>
        public ProviderBuilder EnableRefreshTokenGrant()
        {
            this.ProviderOptions.GrantTypes.Add(GrantType.RefreshToken);
            return this;
        }

        /// <summary>
        /// Enables the user info endpoint.  Optional (disabled by default).<br />
        /// See <see href="https://openid.net/specs/openid-connect-core-1_0.html#UserInfo"></see>
        /// The UserInfo only applies with the AuthorizationCode flow grant enabled.
        /// </summary>
        /// <returns><see cref="ProviderBuilder"/> so additional calls can be chained.</returns>
        public ProviderBuilder EnableUserInfoEndpoint()
        {
            this.ServiceCollection.AddScoped<IEndpointHandler<UserInfoHandler>, UserInfoHandler>();
            this.ProviderOptions.UserInfoEndpointEnabled = true;
            return this;
        }

        /// <summary>
        /// Log authentication failures as information.  Optional (defaults to True).<br />
        /// When set to false authentication failures are logged as errors.<br />
        /// Use set to false in development and testing to highlight authentication issues.<br />
        /// Setting to false in production will likely cause high numbers of false alarms due to automated hacking attempts.
        /// </summary>
        /// <param name="asInformation">.</param>
        /// <returns><see cref="ProviderBuilder"/> so additional calls can be chained.</returns>
        public ProviderBuilder LogFailuresAsInformation(bool asInformation)
        {
            this.ProviderOptions.LogFailuresAsInformation = asInformation;
            return this;
        }

        /// <summary>
        /// Sets the endpoint address to use for authorisation.  Optional (defaults to "/connect/authorize")<br />
        /// See <see href="https://openid.net/specs/openid-connect-discovery-1_0.html#ProviderMetadata"></see>.
        /// </summary>
        /// <param name="uriString">The endpoint address in string form.</param>
        /// <returns><see cref="ProviderBuilder"/> so additional calls can be chained.</returns>
        public ProviderBuilder SetAuthorizationEndpointAddress(string uriString) =>
            this.SetAuthorizationEndpointUri(new Uri(uriString, UriKind.RelativeOrAbsolute));

        /// <summary>
        /// Sets the endpoint URI to use for authorisation.  Optional (defaults to "/connect/authorize").<br />
        /// See <see href="https://openid.net/specs/openid-connect-discovery-1_0.html#ProviderMetadata"></see>.
        /// </summary>
        /// <param name="uri">The endpoint Uri.</param>
        /// <returns><see cref="ProviderBuilder"/> so additional calls can be chained.</returns>
        public ProviderBuilder SetAuthorizationEndpointUri(Uri uri)
        {
            AssertIsValidRelativeUri(uri);
            this.ProviderOptions.AuthorizationEndpointUri = uri;
            return this;
        }

        /// <summary>
        /// Sets the minimum token expiry time allowed for the authorization code flow.  Optional (defaults to 1 minute).<br />
        /// Prevents silent token renewal loops when approaching expiry of the session.<br />
        /// When below the minimum time the authorization endpoint will respond with error "login_required".
        /// </summary>
        /// <param name="minimumExpiry">The mimimum expiry time.</param>
        /// <returns><see cref="ProviderBuilder"/> so additional calls can be chained.</returns>
        public ProviderBuilder SetAuthorizationMinimumTokenExpiry(TimeSpan minimumExpiry)
        {
            this.ProviderOptions.AuthorizationMinimumTokenExpiry = minimumExpiry;
            return this;
        }

        /// <summary>
        /// Sets the endpoint address to use to check session state.  Optional (defaults to "/connect/checksession")<br />
        /// See <see href="https://openid.net/specs/openid-connect-session-1_0.html#OPiframe"></see>.
        /// </summary>
        /// <param name="uriString">The endpoint address in string form.</param>
        /// <returns><see cref="ProviderBuilder"/> so additional calls can be chained.</returns>
        public ProviderBuilder SetCheckSessionEndpointAddress(string uriString) =>
            this.SetCheckSessionEndpointUri(new Uri(uriString, UriKind.RelativeOrAbsolute));

        /// <summary>
        /// Sets the endpoint URI to use to check session state.  Optional (defaults to "/connect/checksession")<br />
        /// See <see href="https://openid.net/specs/openid-connect-session-1_0.html#OPiframe"></see>.
        /// </summary>
        /// <param name="uri">The endpoint Uri.</param>
        /// <returns><see cref="ProviderBuilder"/> so additional calls can be chained.</returns>
        public ProviderBuilder SetCheckSessionEndpointUri(Uri uri)
        {
            AssertIsValidRelativeUri(uri);
            this.ProviderOptions.CheckSessionEndpointUri = uri;
            return this;
        }

        /// <summary>
        /// Sets the endpoint address to use to end session.  Optional (defaults to "/connect/endsession")<br />
        /// See <see href="https://openid.net/specs/openid-connect-rpinitiated-1_0.html"></see>.
        /// </summary>
        /// <param name="uriString">The endpoint address in string form.</param>
        /// <returns><see cref="ProviderBuilder"/> so additional calls can be chained.</returns>
        public ProviderBuilder SetEndSessionEndpointAddress(string uriString) =>
            this.SetEndSessionEndpointUri(new Uri(uriString, UriKind.RelativeOrAbsolute));

        /// <summary>
        /// Sets the endpoint URI to use to end session.  Optional (defaults to "/connect/endsession").<br />
        /// See <see href="https://openid.net/specs/openid-connect-rpinitiated-1_0.html"></see>.
        /// </summary>
        /// <param name="uri">The endpoint Uri.</param>
        /// <returns><see cref="ProviderBuilder"/> so additional calls can be chained.</returns>
        public ProviderBuilder SetEndSessionEndpointUri(Uri uri)
        {
            AssertIsValidRelativeUri(uri);
            this.ProviderOptions.EndSessionEndpointUri = uri;
            return this;
        }

        /// <summary>
        /// Sets the value to store in the JWT "idp" claim.  Optional (defaults to "internal").
        /// </summary>
        /// <param name="identityProvider">.</param>
        /// <returns><see cref="ProviderBuilder"/> so additional calls can be chained.</returns>
        public ProviderBuilder SetIdentityProvider(string identityProvider)
        {
            this.ProviderOptions.IdentityProvider = identityProvider;
            return this;
        }

        /// <summary>
        /// Sets the signing certificate(s) included in the Json Web Key Set.<br />
        /// Required unless ICertificateStore is implemented to provide certificates at runtime.<br />
        /// See <see href="https://openid.net/specs/openid-connect-core-1_0.html#SigEnc"></see>
        /// Certificate selection respects NotBefore and NotAfter certificate properties,<br />
        /// and always selects the certificate with the longest time to expiry when issuing new tokens.<br />
        /// Use multiple certificates to rollover keys by loading a replacement certificate (that has at least 24 hours
        /// of overlap with your current certificate) at least 24 hours before your current certificate expires .<br />
        /// </summary>
        /// <param name="x509Certificate2s">The list of certificates to use for signing purposes.</param>
        /// <returns><see cref="ProviderBuilder"/> so additional calls can be chained.</returns>
        public ProviderBuilder SetSigningCertificates(IEnumerable<X509Certificate2> x509Certificate2s)
        {
            this.ProviderOptions.SigningKeys.StoreSigningKeys(x509Certificate2s);
            return this;
        }

        /// <summary>
        /// Sets the expiry time for any signing certificate(s) loaded from the certificate store.  Optional (defaults to 12 hours).<br />
        /// Once the expiry time has been reached certificates are automatically reloaded from the certificate store.<br />
        /// Ensure your store has access to a replacement certificate before the last certificate reload prior to your active certificate expiry.<br />
        /// Note:  Only certificates loaded from the <see cref="ICertificateStore"/> implementation are expired.
        /// </summary>
        /// <param name="certificateExpiry">The expiry time for all certificates loaded from the certificate store.</param>
        /// <returns><see cref="ProviderBuilder"/> so additional calls can be chained.</returns>
        public ProviderBuilder SetStoreSigningCertificateExpiry(TimeSpan certificateExpiry)
        {
            this.ProviderOptions.StoreSigningKeyExpiry = certificateExpiry;
            return this;
        }

        /// <summary>
        /// Sets the endpoint address to use to issue tokens.  Optional (defaults to "/connect/token")<br />
        /// See <see href="https://openid.net/specs/openid-connect-discovery-1_0.html#ProviderMetadata"></see>.
        /// </summary>
        /// <param name="uriString">The endpoint address in string form.</param>
        /// <returns><see cref="ProviderBuilder"/> so additional calls can be chained.</returns>
        public ProviderBuilder SetTokenEndpointAddress(string uriString) =>
            this.SetTokenEndpointUri(new Uri(uriString, UriKind.RelativeOrAbsolute));

        /// <summary>
        /// Sets the endpoint URI to use to issue tokens.  Optional (defaults to "/connect/token").<br />
        /// See <see href="https://openid.net/specs/openid-connect-discovery-1_0.html#ProviderMetadata"></see>.
        /// </summary>
        /// <param name="uri">The endpoint Uri.</param>
        /// <returns><see cref="ProviderBuilder"/> so additional calls can be chained.</returns>
        public ProviderBuilder SetTokenEndpointUri(Uri uri)
        {
            AssertIsValidRelativeUri(uri);
            this.ProviderOptions.TokenEndpointUri = uri;
            return this;
        }

        /// <summary>
        /// Sets the endpoint address used for user information.  Optional (defaults to "/connect/userinfo")<br />
        /// See <see href="https://openid.net/specs/openid-connect-core-1_0.html#UserInfo"></see>.
        /// </summary>
        /// <param name="uriString">The endpoint address in string form.</param>
        /// <returns><see cref="ProviderBuilder"/> so additional calls can be chained.</returns>
        public ProviderBuilder SetUserInfoEndpointAddress(string uriString) =>
            this.SetUserInfoEndpointUri(new Uri(uriString, UriKind.RelativeOrAbsolute));

        /// <summary>
        /// Sets the endpoint URI used for user information.  Optional (defaults to "/connect/userinfo")<br />
        /// See <see href="https://openid.net/specs/openid-connect-session-1_0.html#OPiframe"></see>.
        /// </summary>
        /// <param name="uri">The endpoint Uri.</param>
        /// <returns><see cref="ProviderBuilder"/> so additional calls can be chained.</returns>
        public ProviderBuilder SetUserInfoEndpointUri(Uri uri)
        {
            AssertIsValidRelativeUri(uri);
            this.ProviderOptions.UserInfoEndpointUri = uri;
            return this;
        }

        private static void AssertIsValidRelativeUri(Uri uri)
        {
            if (!uri.IsWellFormedOriginalString())
            {
                throw new ArgumentException("Invalid URI (not well formed)", nameof(uri));
            }
            if (uri.OriginalString.StartsWith('~'))
            {
                throw new ArgumentException("Invalid URI (must not start with ~)", nameof(uri));
            }
            if (uri.IsAbsoluteUri)
            {
                throw new ArgumentException("Invalid URI (must not be absolute)", nameof(uri));
            }
        }
    }
}
