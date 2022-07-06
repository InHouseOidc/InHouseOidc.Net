// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Provider
{
    /// <summary>
    /// Interface to be implemented by the provider host API retrieve audiences related to API scopes.<br />
    /// Required by all flows.
    /// </summary>
    public interface IResourceStore
    {
        /// <summary>
        /// Resolves audiences from scopes.
        /// </summary>
        /// <param name="scopes">The list of scopes.</param>
        /// <returns>The list of audiences that relate to the scopes.</returns>
        Task<List<string>> GetAudiences(List<string> scopes);
    }
}
