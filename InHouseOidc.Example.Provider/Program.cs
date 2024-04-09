// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

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
// var signingCertificate = new X509Certificate2("InHouseOidcExample.pfx", "Internal");
builder
    .Services.AddOidcProvider()
    .EnableAuthorizationCodeFlow()
    // .EnableCheckSessionEndpoint()
    .EnableClientCredentialsFlow()
    .EnableRefreshTokenGrant()
    // .EnableUserInfoEndpoint()
    .LogFailuresAsInformation(false)
    // SetSigningCertificates(new[] { signingCertificate })
    .Build();

// Setup the stores the provider relies on
builder.Services.AddSingleton<ICertificateStore, InHouseOidc.Example.Provider.CertificateStore>();
builder.Services.AddSingleton<IClientStore, InHouseOidc.Example.Provider.ClientStore>();
builder.Services.AddSingleton<ICodeStore, InHouseOidc.Example.Provider.CodeStore>();
builder.Services.AddSingleton<IResourceStore, InHouseOidc.Example.Provider.ResourceStore>();
builder.Services.AddSingleton<IUserStore, InHouseOidc.Example.Provider.UserStore>();

// Setup OIDC Provider API
const string scope = "exampleproviderapiscope";
const string audience = "exampleproviderapiresource";
builder.Services.AddOidcProviderApi(audience, scope);

// Setup OIDC Razor page client (authenticates user access to this site)
const string providerAddress = "http://localhost:5100";
var clientOptions = new PageClientOptions
{
    AccessDeniedPath = "/AccessDenied",
    ClientId = "providerexample",
    // GetClaimsFromUserInfoEndpoint = true,
    IssueLocalAuthenticationCookie = false,
    OidcProviderAddress = providerAddress,
    Scope = "openid email phone profile role exampleapiscope",
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

// Add secure API endpoint
app.MapGet("/secure", () => $"UTC = {System.DateTime.UtcNow:u}").RequireAuthorization(scope);
app.Run();
