// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Provider
{
    /// <summary>
    /// Interface to be implemented by the provider host API to provide access to user information.<br />
    /// Required for the authorization code flow.
    /// </summary>
    public interface IUserStore
    {
        /// <summary>
        /// Checks if the user associated with a subject is still active.
        /// </summary>
        /// <param name="issuer">The issuer associated with the user.</param>
        /// <param name="subject">The subject identifier associated with the user.</param>
        /// <returns>True if the subject is currently active.</returns>
        Task<bool> IsUserActive(string issuer, string subject);

        /// <summary>
        /// Gets the additional claims to include in the authentication cookie and identity token.<br />
        /// Must be implemented if the user info endpoint is enabled.
        /// </summary>
        /// <param name="issuer">The issuer address associated with the user.</param>
        /// <param name="subject">The subject identifier associated with the user.</param>
        /// <param name="scopes">The list of scopes from the access token used to request user claims.</param>
        /// <returns><see cref="Dictionary{TKey, TValue}"/> of user claims as key (claim name) value (claim value),
        /// or null if the user is not found.</returns>
        Task<List<Claim>?> GetUserClaims(string issuer, string subject, List<string> scopes);
    }
}
