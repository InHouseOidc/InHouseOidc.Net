// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Example.Common;
using InHouseOidc.PageClient;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InHouseOidc.Certify.Provider
{
    [Authorize(AuthenticationSchemes = OpenIdConnectDefaults.AuthenticationScheme)]
    public class Index : PageModel
    {
        public ExampleViewModel ViewModel { get; set; }

        public Index()
        {
            this.ViewModel = new ExampleViewModel();
        }

        public async Task<IActionResult> OnGet()
        {
            return await this.RenderPage();
        }

        public async Task<IActionResult> OnPostLogoutAsync()
        {
            await this.HttpContext.ProviderPageClientLogout(this.Url.PageLink("Index"));
            return await this.RenderPage();
        }

        private async Task<PageResult> RenderPage()
        {
            this.ViewModel = await ExampleHelper.GetExampleViewModel(this.HttpContext);
            return this.Page();
        }
    }
}
