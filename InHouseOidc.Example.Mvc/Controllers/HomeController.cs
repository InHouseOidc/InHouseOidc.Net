// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Example.Common;
using InHouseOidc.PageClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InHouseOidc.Example.Mvc.Controllers
{
    [Authorize(Roles = "UserRole1,UserRole2")]
    public class HomeController(IHttpClientFactory httpClientFactory) : Controller
    {
        private const string ClientName = "exampleapi";
        private const string ApiAddress = "http://localhost:5102";
        private const string ProviderAddress = "http://localhost:5100";

        private readonly IHttpClientFactory httpClientFactory = httpClientFactory;

        public async Task<IActionResult> Index()
        {
            return await this.RenderView();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CallApi()
        {
            var httpClient = this.httpClientFactory.CreateClient(ClientName);
            var apiResult = await ExampleHelper.CallApi(httpClient, ApiAddress);
            return await this.RenderView(apiResult, string.Empty);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CallProviderApi()
        {
            var httpClient = this.httpClientFactory.CreateClient(ClientName);
            var apiResultProvider = await ExampleHelper.CallApi(httpClient, ProviderAddress);
            return await this.RenderView(string.Empty, apiResultProvider);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await this.HttpContext.PageClientLogout(this.Url.ActionLink("Index", "Home"));
            return await this.RenderView();
        }

        private async Task<ViewResult> RenderView(string apiResult = "", string apiResultProvider = "")
        {
            return this.View(
                "Index",
                await ExampleHelper.GetExampleViewModel(this.HttpContext, apiResult, apiResultProvider)
            );
        }
    }
}
