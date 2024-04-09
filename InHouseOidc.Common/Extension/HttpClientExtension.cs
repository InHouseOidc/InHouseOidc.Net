// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using Microsoft.Extensions.Logging;
using Polly;

namespace InHouseOidc.Common.Extension
{
    public static class HttpClientExtension
    {
        public static async Task<HttpResponseMessage> SendWithRetry(
            this HttpClient httpClient,
            HttpMethod httpMethod,
            Uri uri,
            object? content,
            ILogger logger,
            CancellationToken cancellationToken,
            int maxRetryAttempts = 1,
            int retryDelayMilliseconds = 50,
            [CallerMemberName] string? caller = null
        )
        {
            return await Policy
                .Handle<HttpClientRetryableException>()
                .Or<HttpRequestException>(e => e.IsRetryableHttpException())
                .Or<AggregateException>(e => e.IsRetryableAggregateException())
                .WaitAndRetryAsync(
                    maxRetryAttempts,
                    i => CalculateDelay(i, retryDelayMilliseconds),
                    (exception, retryCount, context) =>
                    {
                        logger.LogWarning(
                            exception,
                            "{caller} send retry {retryCount} of {context.PolicyKey} - uri: {targetUri}",
                            caller,
                            retryCount,
                            context.PolicyKey,
                            uri
                        );
                    }
                )
                .ExecuteAsync(async () =>
                {
                    var request = new HttpRequestMessage(httpMethod, uri);
                    if (content != null)
                    {
                        if (content is FormUrlEncodedContent formContent)
                        {
                            request.Content = formContent;
                        }
                        else
                        {
                            request.Content = new JsonContent(content);
                            request.Headers.Add("Accept", "application/json");
                        }
                    }
                    var response = await httpClient.SendAsync(
                        request,
                        HttpCompletionOption.ResponseContentRead,
                        cancellationToken
                    );
                    if (
                        response.StatusCode == HttpStatusCode.RequestTimeout // 408
                        || response.StatusCode == HttpStatusCode.BadGateway // 502
                        || response.StatusCode == HttpStatusCode.ServiceUnavailable // 503
                        || response.StatusCode == HttpStatusCode.GatewayTimeout // 504
                    )
                    {
                        throw new HttpClientRetryableException(
                            response.StatusCode,
                            $"Retryable status code {(int)response.StatusCode} was returned"
                        );
                    }
                    return response;
                });
        }

        internal static TimeSpan CalculateDelay(int retryAttempt, int retryDelayMilliseconds)
        {
            return TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt - 1) * retryDelayMilliseconds);
        }

        internal class JsonContent(object obj)
            : StringContent(JsonSerializer.Serialize(obj, JsonSerializerOptions), Encoding.UTF8, "application/json")
        {
            private static readonly JsonSerializerOptions JsonSerializerOptions =
                new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        }

        internal class HttpClientRetryableException(HttpStatusCode statusCode, string message) : Exception(message)
        {
            public HttpStatusCode StatusCode { get; private set; } = statusCode;
        }
    }
}
