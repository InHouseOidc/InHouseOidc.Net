// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Example.PlaywrightTest
{
    /// <summary>
    /// Performs the provider login flow: navigates to the provider login page and submits credentials.
    /// All example apps use the provider at http://localhost:5100 for authentication.
    /// The login form accepts any subject/email/name and logs the user in unconditionally.
    /// </summary>
    internal static class ProviderLogin
    {
        internal const string DefaultSubject = "joe.bloggs";
        internal const string DefaultEmail = "joe@bloggs.name";
        internal const string DefaultName = "Joe Bloggs";

        internal static async Task LoginAsync(IPage page)
        {
            // Wait for redirect to the provider login page
            await page.WaitForURLAsync(url =>
                url.StartsWith("http://localhost:5100/login", StringComparison.OrdinalIgnoreCase)
            );
            // Fill in credentials (provider login accepts any values)
            await page.FillAsync("#Subject", DefaultSubject);
            await page.FillAsync("#Email", DefaultEmail);
            await page.FillAsync("#Name", DefaultName);
            await page.ClickAsync("button[value='login']");
        }
    }
}
