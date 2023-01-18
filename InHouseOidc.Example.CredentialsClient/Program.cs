// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.CredentialsClient;
using InHouseOidc.Example.CredentialsClient;
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
const string clientNameStartup = "exampleapistartup";
const string clientNameRuntime = "exampleapiruntime";
const string providerAddress = "http://localhost:5100";
const string clientId = "clientcredentialsexample";
const string clientSecret = "topsecret";
const string scope = "exampleapiscope";
serviceCollection
    .AddSingleton<ICredentialsStore, CredentialsStore>()
    .AddHttpClient(clientNameRuntime)
        .AddClientCredentialsToken();
serviceCollection
    .AddOidcCredentialsClient()
    .AddClient(
        clientNameStartup,
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
        var httpClientStartup = httpClientFactory.CreateClient(clientNameStartup);
        var responseStartup = await httpClientStartup.GetAsync(new Uri(new Uri(apiAddress), "/secure"));
        responseStartup.EnsureSuccessStatusCode();
        var responseContentStartup = await responseStartup.Content.ReadAsStringAsync();
        logger.LogInformation("[Startup HttpClient] /secure response: {responseContent}", responseContentStartup);
        var httpClientRuntime = httpClientFactory.CreateClient(clientNameRuntime);
        var responseRuntime = await httpClientRuntime.GetAsync(new Uri(new Uri(apiAddress), "/secure"));
        responseRuntime.EnsureSuccessStatusCode();
        var responseContentRuntime = await responseRuntime.Content.ReadAsStringAsync();
        logger.LogInformation("[Runtime HttpClient] /secure response: {responseContent}", responseContentRuntime);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Caught exception");
    }
}
