// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using System.Security.Cryptography.X509Certificates;
using InHouseOidc.PageClient;
using InHouseOidc.Provider;
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

// Setup the OIDC provider
var signingCertificate = new X509Certificate2("InHouseOidcCertify.pfx", "Internal");

builder
    .Services.AddOidcProvider()
    .EnableAuthorizationCodeFlow(false)
    .EnableCheckSessionEndpoint()
    .EnableClientCredentialsFlow()
    .EnableRefreshTokenGrant()
    .EnableUserInfoEndpoint()
    .LogFailuresAsInformation(false)
    .SetSigningCertificates([signingCertificate])
    .Build();

// Setup the stores the provider relies on
builder.Services.AddSingleton<IClientStore, InHouseOidc.Certify.Provider.ClientStore>();
builder.Services.AddSingleton<ICodeStore, InHouseOidc.Certify.Provider.CodeStore>();
builder.Services.AddSingleton<IResourceStore, InHouseOidc.Certify.Provider.ResourceStore>();
builder.Services.AddSingleton<IUserStore, InHouseOidc.Certify.Provider.UserStore>();

// Setup OIDC Provider API
const string scope = "certifyproviderapiscope";
const string audience = "certifyproviderapiresource";
builder.Services.AddOidcProviderApi(audience, scope);

// Setup OIDC Razor page client (authenticates user access to this site)
string providerAddress = builder.Configuration["ProviderAddress"] ?? string.Empty;
var clientOptions = new PageClientOptions
{
    AccessDeniedPath = "/AccessDenied",
    ClientId = "providercertify",
    GetClaimsFromUserInfoEndpoint = true,
    IssueLocalAuthenticationCookie = false,
    OidcProviderAddress = providerAddress,
    Scope = "openid address email phone profile",
    UniqueClaimMappings = new() { { "address", "address" }, { "phone_number", "phone_number" } },
};

// csharpier-ignore
builder.Services
    .AddOidcPageClient()
    .AddClient(clientOptions)
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

// Add secure API endpoint
app.MapGet("/secure", () => $"UTC = {System.DateTime.UtcNow:u}").RequireAuthorization(scope);
app.Run();
