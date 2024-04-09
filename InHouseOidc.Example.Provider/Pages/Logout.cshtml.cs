// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Provider;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InHouseOidc.Example.Provider.Pages
{
    public class Logout(IProviderSession providerSession) : PageModel
    {
        public enum LogoutStatus
        {
            None = 0,
            ConfirmLogout = 1,
            InvalidLogoutCode = 2,
            LoggedOut = 3,
            NotLoggedIn = 4,
        }

        [ViewData]
        public LogoutStatus Status { get; set; }

        [ViewData]
        public string? IdTokenHint { get; set; }

        [ViewData]
        public string? PostLogoutRedirectUri { get; set; }

        [ViewData]
        public string? State { get; set; }

        private readonly IProviderSession providerSession = providerSession;

        public async Task OnGetAsync([FromQuery(Name = "logout_code")] string? logoutCode)
        {
            if (!(this.HttpContext.User.Identity?.IsAuthenticated ?? false))
            {
                this.Status = LogoutStatus.NotLoggedIn;
                return;
            }
            if (string.IsNullOrEmpty(logoutCode))
            {
                // Confirmation of intention to logout is required
                this.Status = LogoutStatus.ConfirmLogout;
                return;
            }
            var logoutRequest = await this.providerSession.GetLogoutRequest(logoutCode);
            if (logoutRequest == null)
            {
                // Bad logout code requires confirmation of intention to logout
                this.Status = LogoutStatus.InvalidLogoutCode;
                return;
            }
            // Good logout code, just log them out immediately
            this.IdTokenHint = logoutRequest?.IdTokenHint;
            this.PostLogoutRedirectUri = logoutRequest?.PostLogoutRedirectUri;
            this.State = logoutRequest?.State;
            this.Status = LogoutStatus.LoggedOut;
            await this.providerSession.Logout(this.HttpContext, logoutCode, logoutRequest);
            return;
        }

        public async Task OnPostAsync()
        {
            await this.providerSession.Logout(this.HttpContext);
            this.Status = LogoutStatus.LoggedOut;
        }
    }
}
