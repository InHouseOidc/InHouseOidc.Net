// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using System.Collections.Concurrent;
using InHouseOidc.Common.Constant;
using InHouseOidc.Discovery;

namespace InHouseOidc.Bff.Type
{
    internal class ClientOptions
    {
        public string AuthenticationCookieName { get; set; } = BffConstant.AuthenticationCookieName;
        public string CallbackPath { get; set; } = BffConstant.CallbackPath;
        public ConcurrentDictionary<string, string> BffApiClients { get; } = new();
        public BffClientOptions? BffClientOptions { get; set; }
        public ConcurrentDictionary<string, BffClientOptions> BffClientOptionsMultitenant { get; } = new();
        public DiscoveryOptions DiscoveryOptions { get; } = new();
        public bool GetClaimsFromUserInfoEndpoint { get; set; } = false;
        public string InternalHttpClientName { get; set; } = DiscoveryConstant.DefaultInternalHttpClientName;
        public Uri LoginEndpointUri { get; set; } = new Uri(BffConstant.LoginPath, UriKind.Relative);
        public Uri LogoutEndpointUri { get; set; } = new Uri(BffConstant.LogoutPath, UriKind.Relative);
        public int MaxRetryAttempts { get; set; } = 5;
        public string NameClaimType { get; set; } = JsonWebTokenClaim.Name;
        public string PostLogoutRedirectAddress { get; set; } = "/";
        public int RetryDelayMilliseconds { get; set; } = 50;
        public string RoleClaimType { get; set; } = JsonWebTokenClaim.Role;
        public string SignedOutCallbackPath { get; set; } = BffConstant.SignedOutCallbackPath;
        public Dictionary<string, string> UniqueClaimMappings { get; set; } = [];
        public Uri UserInfoEndpointUri { get; set; } = new Uri(BffConstant.UserInfoPath, UriKind.Relative);
    }
}
