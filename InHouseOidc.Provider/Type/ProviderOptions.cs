// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common.Constant;

namespace InHouseOidc.Provider.Type
{
    internal class ProviderOptions
    {
        public string AuthenticationCookieName { get; set; } = ProviderConstant.AuthenticationCookieName;
        public bool AuthorizationCodePkceRequired { get; set; } = true;
        public Uri AuthorizationEndpointUri { get; set; } =
            new Uri(ProviderConstant.AuthorizationPath, UriKind.Relative);
        public TimeSpan AuthorizationMinimumTokenExpiry { get; set; } = TimeSpan.FromSeconds(60);
        public string CheckSessionCookieName { get; set; } = ProviderConstant.CheckSessionCookieName;
        public bool CheckSessionEndpointEnabled { get; set; } = false;
        public Uri CheckSessionEndpointUri { get; set; } = new Uri(ProviderConstant.CheckSessionPath, UriKind.Relative);
        public Uri DiscoveryEndpointUri { get; } = new Uri(ProviderConstant.DiscoveryPath, UriKind.Relative);
        public Uri EndSessionEndpointUri { get; set; } = new Uri(ProviderConstant.EndSessionPath, UriKind.Relative);
        public string ErrorPath { get; set; } = ProviderConstant.ErrorPath;
        public List<GrantType> GrantTypes { get; } = new();
        public string IdentityProvider { get; set; } = ProviderConstant.IdentityProvider;
        public Uri JsonWebKeySetUri { get; } = new Uri(ProviderConstant.JsonWebKeysPath, UriKind.Relative);
        public bool LogFailuresAsInformation { get; set; } = true;
        public string LoginPath { get; set; } = ProviderConstant.LoginPath;
        public string LogoutPath { get; set; } = ProviderConstant.LogoutPath;
        public List<SigningKey> SigningKeys { get; set; } = new();
        public Uri TokenEndpointUri { get; set; } = new Uri(ProviderConstant.TokenPath, UriKind.Relative);
        public List<string> TokenEndpointAuthMethods { get; } =
            new(new[] { DiscoveryConstant.ClientSecretPost, DiscoveryConstant.ClientSecretBasic });
        public bool UserInfoEndpointEnabled { get; set; } = false;
        public Uri UserInfoEndpointUri { get; set; } = new Uri(ProviderConstant.UserInfoPath, UriKind.Relative);
    }
}
