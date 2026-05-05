// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Example.PlaywrightTest
{
    /// <summary>
    /// Authentication tests for InHouseOidc.Example.React (http://localhost:5105).
    /// The React SPA uses the BFF (http://localhost:5104) for authentication and API access.
    /// </summary>
    [TestClass]
    public class ReactAuthenticationTest : AuthenticationTestBase
    {
        [TestMethod]
        public async Task Authentication()
        {
            // Unauthenticated: login button enabled, logout button disabled
            await this.Page.GotoAsync("http://localhost:5105/");
            var loginButton = this.Page.Locator("button", new() { HasText = "Login" });
            await this.Expect(loginButton).ToBeVisibleAsync();
            await this.Expect(loginButton).ToBeEnabledAsync();
            var logoutButton = this.Page.Locator("button", new() { HasText = "Logout" });
            await this.Expect(logoutButton).ToBeDisabledAsync();

            // Login and see session info
            await loginButton.ClickAsync();
            await ProviderLogin.LoginAsync(this.Page);
            await this.Page.WaitForURLAsync("http://localhost:5105/");
            await this.Expect(this.Page.Locator("button", new() { HasText = "Logout" })).ToBeEnabledAsync();
            await this.Expect(this.Page.Locator("button", new() { HasText = "Login" })).ToBeDisabledAsync();
            await this.Expect(this.Page.Locator("table").First).ToBeVisibleAsync();

            // Call BFF while authenticated
            await this.Page.Locator("button", new() { HasText = "Call BFF" }).ClickAsync();
            var bffResult = this
                .Page.Locator("button", new() { HasText = "Call BFF" })
                .Locator("xpath=following-sibling::span");
            await this.Expect(bffResult).ToContainTextAsync("/secure-bff response:");

            // Call API while authenticated
            await this.Page.Locator("button", new() { HasText = "Call API" }).ClickAsync();
            var apiResult = this
                .Page.Locator("button", new() { HasText = "Call API" })
                .Locator("xpath=following-sibling::span");
            await this.Expect(apiResult).ToContainTextAsync("/secure-api response:");

            // Call Provider API while authenticated
            await this.Page.Locator("button", new() { HasText = "Call Provider API" }).ClickAsync();
            var providerApiResult = this
                .Page.Locator("button", new() { HasText = "Call Provider API" })
                .Locator("xpath=following-sibling::span");
            await this.Expect(providerApiResult).ToContainTextAsync("/secure-provider response:");

            // Open a second page in the same browser context — shares session cookies so already logged in
            var page2 = await this.Context.NewPageAsync();
            await page2.GotoAsync("http://localhost:5105/");
            await this.Expect(page2.Locator("button", new() { HasText = "Logout" })).ToBeEnabledAsync();
            await this.Expect(page2.Locator("button", new() { HasText = "Login" })).ToBeDisabledAsync();

            // Log out on page 2
            await page2.Locator("button", new() { HasText = "Logout" }).ClickAsync();
            await this.Expect(page2.Locator("button", new() { HasText = "Login" })).ToBeEnabledAsync();

            // Page 1 should automatically detect the session logout via the check session mechanism
            await this.Expect(this.Page.Locator("button", new() { HasText = "Login" }))
                .ToBeEnabledAsync(new() { Timeout = 30_000 });
        }
    }
}
