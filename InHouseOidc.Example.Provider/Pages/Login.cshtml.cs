// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common.Constant;
using InHouseOidc.Provider;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InHouseOidc.Example.Provider.Pages
{
    public class Login(IProviderSession providerSession) : PageModel
    {
        private readonly IProviderSession providerSession = providerSession;

        [BindProperty]
        public string Email { get; set; } = "joe@bloggs.name";

        [BindProperty]
        public string Name { get; set; } = "Joe Bloggs";

        [BindProperty]
        public string Roles { get; set; } = "UserRole1,UserRole2";

        [BindProperty]
        public string Subject { get; set; } = "joe.bloggs";

        [BindProperty]
        public string Website { get; set; } = "www.bloggs.name";

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
            // Login
            var claims = new List<Claim>
            {
                new(JsonWebTokenClaim.AuthenticationMethodReference, AuthenticationMethodReference.Password),
                new(JsonWebTokenClaim.Email, this.Email),
                new(JsonWebTokenClaim.Website, this.Website),
            };
            if (!string.IsNullOrEmpty(this.Roles))
            {
                foreach (var role in this.Roles.Split(","))
                {
                    claims.Add(new Claim(JsonWebTokenClaim.Role, role));
                }
            }
            claims.Add(new Claim(JsonWebTokenClaim.Subject, this.Subject));
            claims.Add(new Claim(JsonWebTokenClaim.Name, this.Name));
            await this.providerSession.Login(this.HttpContext, claims, TimeSpan.FromMinutes(60));
            return this.Page();
        }
    }
}
