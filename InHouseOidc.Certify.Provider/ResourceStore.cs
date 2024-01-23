// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Provider;

namespace InHouseOidc.Certify.Provider
{
    public class ResourceStore : IResourceStore
    {
        private readonly Dictionary<string, string[]> resources = new();

        public ResourceStore(IConfiguration configuration)
        {
            var resources = configuration.GetSection("ResourceStore").GetChildren();
            foreach (var resource in resources)
            {
                var resourceConfig = resource.Get<ResourceConfig>();
                this.resources.Add(resource.Key, resourceConfig?.Scopes ?? Array.Empty<string>());
            }
        }

        public Task<List<string>> GetAudiences(List<string> scopes)
        {
            var results = new List<string>();
            foreach (var scope in scopes)
            {
                foreach (var kvp in this.resources)
                {
                    if (kvp.Value.Contains(scope))
                    {
                        if (!results.Contains(kvp.Key))
                        {
                            results.Add(kvp.Key);
                        }
                    }
                }
            }
            return Task.FromResult(results);
        }

        private class ResourceConfig
        {
            public string[] Scopes { get; set; } = Array.Empty<string>();
        }
    }
}
