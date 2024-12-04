// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.PageClient;
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
builder.Services.AddRazorPages();

// Setup OIDC Razor page client (authenticates user access to this site)
// and an OIDC Razor API client (allows this site to call authenticated APIs)
const string providerAddress = "http://localhost:5100";
var clientOptions = new PageClientOptions
{
    AccessDeniedPath = "/AccessDenied",
    ClientId = "razorexample",
    ClientSecret = "topsecret",
    CookieName = "InHouseOidc.Example.Razor",
    // GetClaimsFromUserInfoEndpoint = true,
    OidcProviderAddress = providerAddress,
    Scope = "openid offline_access email phone profile role exampleapiscope exampleproviderapiscope",
    UniqueClaimMappings = new() { { "phone_number", "phone_number" } },
};

const string clientName = "exampleapi";

// csharpier-ignore
builder.Services
    .AddOidcPageClient()
    .AddClient(clientOptions)
    .AddApiClient(clientName)
    .Build();

var app = builder.Build();
app.UseHttpLogging();
app.UseCookiePolicy(new CookiePolicyOptions { Secure = CookieSecurePolicy.Always });
app.UseStaticFiles();

// InHouseOidc utilizes the standard authentication middleware, make sure you use it
app.UseAuthentication();

// InHouseOidc utilizes the standard authorization middleware, make sure you use it
app.UseAuthorization();
app.MapRazorPages();

app.Run();
