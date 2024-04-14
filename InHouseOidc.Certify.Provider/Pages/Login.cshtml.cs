// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using System.Security.Claims;
using InHouseOidc.Common.Constant;
using InHouseOidc.Provider;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InHouseOidc.Certify.Provider
{
    public class Login(IConfiguration configuration, IProviderSession providerSession, IUserStore userStore) : PageModel
    {
        private readonly IProviderSession providerSession = providerSession;
        private readonly IUserStore userStore = userStore;
        private readonly string issuer = configuration["ProviderAddress"] ?? string.Empty;

        [BindProperty]
        public string Subject { get; set; } = string.Empty;

        [BindProperty]
        public string? ReturnUrl { get; set; }

        public IActionResult OnGet(string returnUrl)
        {
            if (returnUrl == null)
            {
                // No return URL supplied, redirect to Index page to trigger the authentication flow
                return this.RedirectToPage("Index");
            }
            this.ReturnUrl = returnUrl;
            return this.Page();
        }

        public async Task<IActionResult> OnPost()
        {
            // The return URL must be valid
            if (this.ReturnUrl == null || !await this.providerSession.IsValidReturnUrl(this.ReturnUrl))
            {
                return this.RedirectToPage("Error", new { error = "invalid_request" });
            }
            // Check subject
            if (!await this.userStore.IsUserActive(this.issuer, this.Subject))
            {
                return this.RedirectToPage("Error", new { error = "invalid_subject" });
            }
            // Login
            var claims = new List<Claim>
            {
                new(JsonWebTokenClaim.AuthenticationMethodReference, AuthenticationMethodReference.Password),
                new(JsonWebTokenClaim.Subject, this.Subject),
            };
            await this.providerSession.Login(this.HttpContext, claims, TimeSpan.FromMinutes(60));
            return this.Page();
        }
    }
}
