// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.CredentialsClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var serviceCollection = new ServiceCollection();
serviceCollection.AddLogging(
    configure =>
        configure.AddSimpleConsole(
            simpleConsoleFormatterOptions => simpleConsoleFormatterOptions.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] "
        )
);

// Setup the OIDC client
const string providerAddress = "http://localhost:5100";
const string clientName = "exampleapi";
const string clientId = "clientcredentialsexample";
const string clientSecret = "topsecret";
const string scope = "exampleapiscope";
serviceCollection
    .AddOidcCredentialsClient()
    .AddClient(
        clientName,
        new CredentialsClientOptions
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            OidcProviderAddress = providerAddress,
            Scope = scope,
        }
    )
    .Build();

var serviceProvider = serviceCollection.BuildServiceProvider();

const string apiAddress = "http://localhost:5102";
Console.WriteLine(
    "Get Discovery and post token should only occur on the first call, or when the access token or discovery has expired."
);
while (true)
{
    // Loop calling the secure endpoint every time the enter key is pressed
    Console.WriteLine("Press [enter] to make an API call, or [ctrl-c] to exit.");
    Console.ReadLine();
    var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger("main");
    try
    {
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient(clientName);
        var response = await httpClient.GetAsync(new Uri(new Uri(apiAddress), "/secure"));
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        logger.LogInformation("/secure response: {responseContent}", responseContent);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Caught exception");
    }
}
