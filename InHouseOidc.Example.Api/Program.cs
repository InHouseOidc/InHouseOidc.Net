// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Api;
using Microsoft.AspNetCore.HttpLogging;

var builder = WebApplication.CreateBuilder(args);

// Setup logging
builder.Services.AddLogging(configure =>
    configure.AddSimpleConsole(simpleConsoleFormatterOptions =>
        simpleConsoleFormatterOptions.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] "
    )
);
builder.Services.AddHttpLogging(httpLogging =>
{
    httpLogging.LoggingFields = HttpLoggingFields.All;
});

// Setup OIDC API
const string providerAddress = "http://localhost:5100";
const string scope = "exampleapiscope";
const string audience = "exampleapiresource";
builder.Services.AddOidcApi(providerAddress, audience, [scope]);

// Build the services
var app = builder.Build();

// InHouseOidc utilizes the standard authentication middleware, make sure you use it
app.UseAuthentication();

// InHouseOidc utilizes the standard authorization middleware, make sure you use it
app.UseAuthorization();

// Add API endpoints
app.MapGet("/secure", () => $"UTC = {System.DateTime.UtcNow:u}").RequireAuthorization(scope);

// Go
app.Run();
