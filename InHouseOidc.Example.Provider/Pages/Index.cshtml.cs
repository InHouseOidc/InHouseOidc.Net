// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Example.Common;
using InHouseOidc.PageClient;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InHouseOidc.Example.Provider
{
    [Authorize(AuthenticationSchemes = OpenIdConnectDefaults.AuthenticationScheme, Roles = "UserRole1,UserRole2")]
    public class Index(IHttpClientFactory httpClientFactory) : PageModel
    {
        public ExampleViewModel ViewModel { get; set; } = new ExampleViewModel();

        private const string ClientName = "exampleapi";
        private const string ApiAddress = "http://localhost:5102";

        private readonly IHttpClientFactory httpClientFactory = httpClientFactory;

        public async Task<IActionResult> OnGet()
        {
            return await this.RenderPage();
        }

        public async Task<IActionResult> OnPostCallApiAsync()
        {
            var httpClient = this.httpClientFactory.CreateClient(ClientName);
            var apiResult = await ExampleHelper.CallApi(httpClient, ApiAddress);
            return await this.RenderPage(apiResult, string.Empty);
        }

        public async Task<IActionResult> OnPostLogoutAsync()
        {
            await this.HttpContext.ProviderPageClientLogout(this.Url.PageLink("Index"));
            return await this.RenderPage();
        }

        private async Task<PageResult> RenderPage(string apiResult = "", string apiResultProvider = "")
        {
            this.ViewModel = await ExampleHelper.GetExampleViewModel(this.HttpContext, apiResult, apiResultProvider);
            return this.Page();
        }
    }
}
