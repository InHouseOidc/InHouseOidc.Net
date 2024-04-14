// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common.Constant;
using InHouseOidc.Discovery;

namespace InHouseOidc.CredentialsClient.Type
{
    internal class ClientOptions
    {
        public ConcurrentDictionary<string, CredentialsClientOptions?> CredentialsClientsOptions { get; } = new();
        public DiscoveryOptions DiscoveryOptions { get; } = new();
        public string InternalHttpClientName { get; set; } = DiscoveryConstant.DefaultInternalHttpClientName;
        public int MaxRetryAttempts { get; set; } = 5;
        public int RetryDelayMilliseconds { get; set; } = 50;
    }
}
