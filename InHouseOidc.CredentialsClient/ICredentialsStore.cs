// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.CredentialsClient
{
    /// <summary>
    /// Interface to be implemented by the host to provide access to client credentials options after startup.<br />
    /// Implement this interface if you load client credentials secrets from a remote key vault, or where client details are inaccessible during API startup.<br />
    /// Once resolved the options are cached in memory indefinitely and cannot be altered.
    /// </summary>
    public interface ICredentialsStore
    {
        /// <summary>
        /// Get client credentials options for a client name.
        /// </summary>
        /// <param name="clientName">The client name registed at startup using <see cref="ClientBuilder.AddClient(string)"></see>,
        /// or resolvable at runtime via the implementation of ICredentialsStore.</param>
        /// <returns><see cref="CredentialsClientOptions"/>.</returns>
        Task<CredentialsClientOptions?> GetCredentialsClientOptions(string clientName);
    }
}
