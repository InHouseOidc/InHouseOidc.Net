// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Bff;
using Microsoft.AspNetCore.HttpLogging;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLogging(configure =>
    configure.AddSimpleConsole(simpleConsoleFormatterOptions =>
        simpleConsoleFormatterOptions.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] "
    )
);
builder.Services.AddHttpLogging(httpLogging =>
{
    httpLogging.LoggingFields = HttpLoggingFields.All;
});

// Setup OIDC BFF client (authenticates user access to this site)
// and an OIDC BFF API client (allows this site to call authenticated APIs)
const string providerAddress = "http://localhost:5100";
const string apiAddress = "http://localhost:5102";
var clientOptions = new BffClientOptions
{
    ClientId = "bffexample",
    ClientSecret = "topsecret",
    OidcProviderAddress = providerAddress,
    Scope = "openid offline_access email phone profile role exampleapiscope exampleproviderapiscope",
};
var clientName = "examplebff";

// csharpier-ignore
builder.Services
    .AddOidcBff()
    .AddApiClient(clientName)
    .SetOidcClient(clientOptions)
    .Build();

var app = builder.Build();

// InHouseOidc utilizes the standard authentication middleware, make sure you use it
app.UseAuthentication();

// InHouseOidc utilizes the standard authorization middleware, make sure you use it
app.UseAuthorization();

// Add API endpoints
app.MapGet(
        "/api/secure-api",
        async (IHttpClientFactory httpClientFactory) =>
        {
            var httpClient = httpClientFactory.CreateClient(clientName);
            var response = await httpClient.GetAsync(new Uri(new Uri(apiAddress), "/secure"));
            if (response.IsSuccessStatusCode)
            {
                return $"/secure-api response: {await response.Content.ReadAsStringAsync()}";
            }
            return $"/secure-api response: StatusCode = {(int)response.StatusCode} {response.StatusCode}";
        }
    )
    .RequireAuthorization(BffConstant.BffApiPolicy);
app.MapGet("/api/secure-bff", () => $"/secure-bff response: UTC = {DateTime.UtcNow:u}")
    .RequireAuthorization(BffConstant.BffApiPolicy);
app.MapGet(
        "/api/secure-provider",
        async (IHttpClientFactory httpClientFactory) =>
        {
            var httpClient = httpClientFactory.CreateClient(clientName);
            var response = await httpClient.GetAsync(new Uri(new Uri(providerAddress), "/secure"));
            if (response.IsSuccessStatusCode)
            {
                return $"/secure-provider response: {await response.Content.ReadAsStringAsync()}";
            }
            return $"/secure-provider response: StatusCode = {(int)response.StatusCode} {response.StatusCode}";
        }
    )
    .RequireAuthorization(BffConstant.BffApiPolicy);

app.Run();
