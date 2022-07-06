// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Provider;

namespace InHouseOidc.Example.Provider
{
    public class ResourceStore : IResourceStore
    {
        public Task<List<string>> GetAudiences(List<string> scopes)
        {
            var results = new List<string>();
            foreach (var scope in scopes)
            {
                if (scope == "exampleapiscope")
                {
                    results.Add("exampleapiresource");
                }
                else if (scope == "exampleproviderapiscope")
                {
                    results.Add("exampleproviderapiresource");
                }
            }
            return Task.FromResult(results);
        }
    }
}
