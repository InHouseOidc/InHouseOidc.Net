﻿// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Example.Common;
using InHouseOidc.PageClient;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InHouseOidc.Example.Razor
{
    [Authorize(Roles = "UserRole1,UserRole2")]
    public class Index : PageModel
    {
        public ExampleViewModel ViewModel { get; set; }

        private const string ClientName = "exampleapi";
        private const string ApiAddress = "http://localhost:5102";
        private const string ProviderAddress = "http://localhost:5100";

        private readonly IHttpClientFactory httpClientFactory;

        public Index(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
            this.ViewModel = new ExampleViewModel();
        }

        public async Task<IActionResult> OnGetAsync()
        {
            return await this.RenderPage();
        }

        public async Task<IActionResult> OnPostCallApiAsync()
        {
            var httpClient = this.httpClientFactory.CreateClient(ClientName);
            var apiResult = await ExampleHelper.CallApi(httpClient, ApiAddress);
            return await this.RenderPage(apiResult, string.Empty);
        }

        public async Task<IActionResult> OnPostCallProviderApiAsync()
        {
            var httpClient = this.httpClientFactory.CreateClient(ClientName);
            var apiResultProvider = await ExampleHelper.CallApi(httpClient, ProviderAddress);
            return await this.RenderPage(string.Empty, apiResultProvider);
        }

        public async Task OnPostLogoutAsync()
        {
            await this.HttpContext.PageClientLogout(this.Url.PageLink("Index"));
            await this.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await this.HttpContext.SignOutAsync(
                OpenIdConnectDefaults.AuthenticationScheme,
                new AuthenticationProperties { RedirectUri = this.Url.PageLink("Index") }
            );
        }

        private async Task<PageResult> RenderPage(string apiResult = "", string apiResultProvider = "")
        {
            this.ViewModel = await ExampleHelper.GetExampleViewModel(this.HttpContext, apiResult, apiResultProvider);
            return this.Page();
        }
    }
}
