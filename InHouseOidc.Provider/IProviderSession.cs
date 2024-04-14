// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using Microsoft.AspNetCore.Http;

namespace InHouseOidc.Provider
{
    /// <summary>
    /// Interface implemented by the OIDC Provider to allow control of sessions.<br />
    /// Available when the authorization code flow is enabled.
    /// </summary>
    public interface IProviderSession
    {
        /// <summary>
        /// Validates a logout request. <br />
        /// When a logout code cannot be validated the logout page should prompt for user confirmation.
        /// </summary>
        /// <param name="logoutCode">The code from the logout_code query parameter.</param>
        /// <returns><see cref="LogoutRequest"/> or null to indicate an unknown logout code.</returns>
        Task<LogoutRequest?> GetLogoutRequest(string logoutCode);

        /// <summary>
        /// Validates a return url. <br />
        /// Used by the login page to block attempts to login and redirect to unauthorized websites with credentials.
        /// </summary>
        /// <param name="returnUrl">The URL from the returnUrl query parameter passed to the login page.</param>
        /// <returns>True for a valid return url.</returns>
        Task<bool> IsValidReturnUrl(string returnUrl);

        /// <summary>
        /// Login a session in the OIDC Provider. <br />
        /// Used by the login page after the user credentials, MFA, etc. are validated. <br />
        /// Issues the authentication cookie, which triggers the completion of the authorization code flow,
        /// and the session cookie (when the check session endpoint is enabled).
        /// </summary>
        /// <param name="httpContext">The HttpContext of the login request.</param>
        /// <param name="claims">The list of claims to attach to the session.</param>
        /// <param name="sessionExpiry">The absolute time the session is active before automatically expiring.</param>
        /// <returns><see cref="ClaimsPrincipal"/> for the new session.</returns>
        Task<ClaimsPrincipal> Login(HttpContext httpContext, List<Claim> claims, TimeSpan sessionExpiry);

        /// <summary>
        /// Logout from the OIDC Provider.<br />
        /// Used by the logout page following verification of the logout code, or where verification fails then user confirmation.<br />
        /// Removes authentication cookie and any session cookie (when the check session endpoint is enabled).
        /// </summary>
        /// <param name="httpContext">The HttpContext of the logout request.</param>
        /// <param name="logoutCode">Any logout code to cleanup.</param>
        /// <param name="logoutRequest">Details of the logout attempt.</param>
        /// <returns><see cref="Task"/>The task.</returns>
        Task Logout(HttpContext httpContext, string? logoutCode = null, LogoutRequest? logoutRequest = null);
    }
}
