// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Example.PlaywrightTest
{
    /// <summary>
    /// Base class for Playwright authentication tests.
    /// Handles trace capture: traces are saved to TestResults/{ClassName}/{TestName}.zip for every test run.
    /// Open with: bin\Debug\net10.0\playwright.ps1 show-trace TestResults\{ClassName}\{TestName}.zip.
    /// </summary>
    public class AuthenticationTestBase : PageTest
    {
        [TestInitialize]
        public async Task StartTracingAsync()
        {
            await this.Context.Tracing.StartAsync(
                new()
                {
                    Title = this.TestContext.TestName,
                    Screenshots = true,
                    Snapshots = true,
                    Sources = true,
                }
            );
        }

        [TestCleanup]
        public async Task StopTracingAsync()
        {
            // AppContext.BaseDirectory = bin\Debug\net10.0 — go up 3 levels to the project directory
            var projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
            var tracePath = Path.Combine(
                projectDir,
                "TestResults",
                this.GetType().Name,
                $"{this.TestContext.TestName ?? "unknown"}.zip"
            );
            Directory.CreateDirectory(Path.GetDirectoryName(tracePath)!);
            await this.Context.Tracing.StopAsync(new() { Path = tracePath });
        }
    }
}
