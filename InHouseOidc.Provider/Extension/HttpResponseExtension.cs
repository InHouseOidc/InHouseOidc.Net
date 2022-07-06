// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common;
using InHouseOidc.Common.Constant;
using InHouseOidc.Provider.Constant;
using InHouseOidc.Provider.Exception;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using System.Net;
using System.Text;
using System.Text.Json;

namespace InHouseOidc.Provider.Extension
{
    internal static class HttpResponseExtension
    {
        public static async Task WriteStreamJsonContent(this HttpResponse httpResponse, MemoryStream memoryStream)
        {
            httpResponse.ContentType = ContentTypeConstant.ApplicationJson;
            httpResponse.ContentLength = memoryStream.Length;
            memoryStream.Seek(0, SeekOrigin.Begin);
            await memoryStream.CopyToAsync(httpResponse.Body, 8192, httpResponse.HttpContext.RequestAborted);
        }

        public static void WriteRedirect(this HttpResponse httpResponse, RedirectErrorException redirectErrorException)
        {
            // Redirect to the error URI indicated with the error as a query parameter
            var errorCode = redirectErrorException.RedirectErrorType.GetEnumMember();
            if (errorCode == null)
            {
                throw new InvalidOperationException(
                    $"Unsupported ErrorCode value {redirectErrorException.RedirectErrorType}"
                );
            }
            var queryBuilder = new QueryBuilder { { AuthorizationEndpointConstant.Error, errorCode } };
            if (!string.IsNullOrEmpty(redirectErrorException.SessionState))
            {
                queryBuilder.Add(AuthorizationEndpointConstant.SessionState, redirectErrorException.SessionState);
            }
            if (!string.IsNullOrEmpty(redirectErrorException.State))
            {
                queryBuilder.Add(AuthorizationEndpointConstant.State, redirectErrorException.State);
            }
            var location = $"{redirectErrorException.Uri}{queryBuilder}";
            httpResponse.Redirect(location);
        }

        public static async Task WriteBadRequestContent(
            this HttpResponse httpResponse,
            BadRequestException badRequestException
        )
        {
            // Form the error JSON
            using var memoryStream = new MemoryStream();
            using var utf8JsonWriter = new Utf8JsonWriter(memoryStream, JsonHelper.JsonWriterOptions);
            utf8JsonWriter.WriteStartObject();
            utf8JsonWriter.WriteNameValue(ExceptionConstant.Error, badRequestException.Error);
            utf8JsonWriter.WriteEndObject();
            utf8JsonWriter.Flush();
            // Write response content & status code
            httpResponse.StatusCode = (int)HttpStatusCode.BadRequest;
            await httpResponse.WriteStreamJsonContent(memoryStream);
        }

        public static async Task WriteInternalErrorContent(this HttpResponse httpResponse)
        {
            // Write response content & status code
            httpResponse.StatusCode = (int)HttpStatusCode.InternalServerError;
            httpResponse.ContentType = ContentTypeConstant.ApplicationJson;
            httpResponse.ContentLength = ExceptionConstant.InternalError.Length;
            await httpResponse.WriteAsync(ExceptionConstant.InternalError);
        }

        public static async Task WriteHtmlContent(this HttpResponse httpResponse, string content)
        {
            httpResponse.ContentType = ContentTypeConstant.TextHtml;
            httpResponse.StatusCode = (int)HttpStatusCode.OK;
            await httpResponse.WriteAsync(content, Encoding.UTF8);
        }

        public static void AppendSessionCookie(
            this HttpResponse httpResponse,
            string checkSessionCookieName,
            bool isSecure,
            string sessionId
        )
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = false,
                IsEssential = true,
                Path = "/",
                SameSite = SameSiteMode.None,
                Secure = isSecure,
            };
            httpResponse.Cookies.Append(checkSessionCookieName, sessionId, cookieOptions);
        }

        public static void DeleteSessionCookie(this HttpResponse httpResponse, string checkSessionCookieName)
        {
            httpResponse.Cookies.Delete(checkSessionCookieName);
        }
    }
}
