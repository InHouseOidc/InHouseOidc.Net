// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common.Constant;

namespace InHouseOidc.Discovery
{
    public class DiscoveryOptions
    {
        public TimeSpan CacheTime { get; set; } = TimeSpan.FromMinutes(30);
        public string InternalHttpClientName { get; set; } = DiscoveryConstant.DefaultInternalHttpClientName;
        public int MaxRetryAttempts { get; set; } = 5;
        public int RetryDelayMilliseconds { get; set; } = 50;
        public bool ValidateGrantTypes { get; set; } = true;
        public bool ValidateIssuer { get; set; } = true;
    }
}
