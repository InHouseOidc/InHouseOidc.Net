// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common.Constant;
using InHouseOidc.Provider.Constant;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace InHouseOidc.Provider.Extension
{
    internal static class HttpRequestExtension
    {
        public static string GetBaseUriString(this HttpRequest httpRequest)
        {
            // Resolve from host header
            if (!httpRequest.Host.HasValue)
            {
                throw new InvalidOperationException("Unable to resolve from host header");
            }
            return $"{httpRequest.Scheme}{Uri.SchemeDelimiter}{httpRequest.Host}";
        }

        public static async Task<(ClaimsPrincipal?, AuthenticationProperties?)> GetClaimsPrincipal(
            this HttpRequest httpRequest,
            IServiceProvider serviceProvider
        )
        {
            var authenticationHandlerProvider = serviceProvider.GetRequiredService<IAuthenticationHandlerProvider>();
            var handler =
                await authenticationHandlerProvider.GetHandlerAsync(
                    httpRequest.HttpContext,
                    ProviderConstant.AuthenticationSchemeCookie
                )
                ?? throw new InvalidOperationException(
                    "Unable to resolve authentication handler for configured scheme"
                );
            var authenticate = await handler.AuthenticateAsync();
            return (authenticate.Principal, authenticate.Properties);
        }

        public static async Task<Dictionary<string, string>?> GetFormDictionary(this HttpRequest httpRequest)
        {
            if (!IsFormContent(httpRequest))
            {
                return null;
            }
            var formCollection = await httpRequest.ReadFormAsync();
            if (formCollection.Count == 0)
            {
                return null;
            }
            var formDictionary = new Dictionary<string, string>();
            foreach (var item in formCollection)
            {
                formDictionary.Add(item.Key, item.Value.ToString());
            }
            return formDictionary;
        }

        public static Dictionary<string, string>? GetQueryDictionary(this HttpRequest httpRequest)
        {
            var queryCollection = httpRequest.Query;
            if (queryCollection.Count == 0)
            {
                return null;
            }
            var queryDictionary = new Dictionary<string, string>();
            foreach (var item in queryCollection)
            {
                queryDictionary.Add(item.Key, item.Value.ToString());
            }
            return queryDictionary;
        }

        private static bool IsFormContent(HttpRequest request)
        {
            if (request.ContentType == null)
            {
                return false;
            }
            if (!MediaTypeHeaderValue.TryParse(request.ContentType, out var value))
            {
                return false;
            }
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            return value.MediaType.Equals(
                ContentTypeConstant.ApplicationForm,
                StringComparison.InvariantCultureIgnoreCase
            );
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        }
    }
}
