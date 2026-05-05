// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Example.PlaywrightTest
{
    /// <summary>
    /// Authentication tests for InHouseOidc.Example.Provider (http://localhost:5100).
    /// The provider site itself is an OIDC client (Razor Pages) that authenticates against its own provider endpoint.
    /// </summary>
    [TestClass]
    public partial class ProviderAuthenticationTest : AuthenticationTestBase
    {
        [GeneratedRegex("Login")]
        public static partial Regex LoginRegex();

        [GeneratedRegex("Home")]
        public static partial Regex HomeRegex();

        [TestMethod]
        public async Task Authentication()
        {
            // Unauthenticated: redirects to login
            await this.Page.GotoAsync("http://localhost:5100/");
            await this.Page.WaitForURLAsync(url =>
                url.StartsWith("http://localhost:5100/login", StringComparison.OrdinalIgnoreCase)
            );
            await this.Expect(this.Page).ToHaveTitleAsync(LoginRegex());

            // Login and see user info
            await this.Page.GotoAsync("http://localhost:5100/");
            await ProviderLogin.LoginAsync(this.Page);
            await this.Page.WaitForURLAsync("http://localhost:5100/");
            await this.Expect(this.Page).ToHaveTitleAsync(HomeRegex());
            var nameCell = this.Page.Locator("table").First.Locator("tbody tr td:first-child");
            await this.Expect(nameCell).ToHaveTextAsync(ProviderLogin.DefaultName);

            // Call API while authenticated
            await this.Page.ClickAsync("#callApi");
            var apiResult = this.Page.Locator("form:has(#callApi) + span");
            await this.Expect(apiResult).ToContainTextAsync("/secure response:");

            // Logout and verify redirect to login
            await this.Page.ClickAsync("#logout");
            await this.Page.GotoAsync("http://localhost:5100/");
            await this.Page.WaitForURLAsync(url =>
                url.StartsWith("http://localhost:5100/login", StringComparison.OrdinalIgnoreCase)
            );
            await this.Expect(this.Page).ToHaveTitleAsync(LoginRegex());
        }
    }
}
