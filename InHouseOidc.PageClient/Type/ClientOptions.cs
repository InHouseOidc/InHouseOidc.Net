// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common.Constant;
using InHouseOidc.Discovery;
using System.Collections.Concurrent;

namespace InHouseOidc.PageClient.Type
{
    internal class ClientOptions
    {
        public TimeSpan DiscoveryCacheTime { get; set; } = TimeSpan.FromMinutes(30);
        public DiscoveryOptions DiscoveryOptions { get; } = new();
        public string InternalHttpClientName { get; set; } = DiscoveryConstant.DefaultInternalHttpClientName;
        public int MaxRetryAttempts { get; set; } = 5;
        public ConcurrentDictionary<string, string> PageApiClients { get; } = new();
        public PageClientOptions? PageClientOptions { get; set; }
        public int RetryDelayMilliseconds { get; set; } = 50;
    }
}
