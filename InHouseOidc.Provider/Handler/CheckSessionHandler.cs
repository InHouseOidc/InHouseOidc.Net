// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Provider.Extension;
using InHouseOidc.Provider.Type;
using Microsoft.AspNetCore.Http;

namespace InHouseOidc.Provider.Handler
{
    internal class CheckSessionHandler : IEndpointHandler<CheckSessionHandler>
    {
        private const string Iframe1 =
            @"<!DOCTYPE html>
<html>
    <title>InHouseOidc CheckSession iframe</title>
<head>
</head>
<body>
";
        private const string Iframe2 =
            @"    <script>
        var cookieName = document.getElementById('checksession-cookie-name')?.textContent;
        var lastPostResult = null;
        function postChangedResult(event, result) {
            // Don't repetitively send 'changed' or 'error' notifications
            if (result === 'unchanged' || result !== lastPostResult) {
                lastPostResult = result;
                event.source.postMessage(result, event.origin);
            }
        }
        if (cookieName) {
            window.addEventListener('message', (event) => {
                if (event.source === window || typeof event.data !== 'string') {
                    return;
                }
                // Parse session hash parts passed via the event
                var spaceParts = event.data.split(' ');
                if (spaceParts.length !== 2) {
                    postChangedResult(event, 'error');
                    return;
                }
                var clientId = spaceParts[0];
                var dotParts = spaceParts[1].split('.');
                if (dotParts.length !== 2) {
                    postChangedResult(event, 'error');
                    return;
                }
                var sessionState = dotParts[0];
                var salt = dotParts[1];
                if (clientId.length === 0 || sessionState.length === 0 || salt.length === 0) {
                    postChangedResult(event, 'error');
                    return;
                }
                // Get the session id out of the cookie
                var currentCookie = document.cookie.split(';').find((cookie) => cookie.trim().startsWith(cookieName + '='));
                var sessionId = currentCookie === undefined ? '' : currentCookie.split('=')[1].trim();
                // Rehash
                var sessionStateRaw = (new TextEncoder()).encode(clientId + event.origin + sessionId + salt);
                try {
                    window.crypto.subtle.digest('SHA-256', sessionStateRaw)
                        .then(hashed => {
                            // Convert the hashed result to Base64Url
                            var hashedUint8 = new Uint8Array(hashed);
                            var hashedLength = hashedUint8.length;
                            var binaryString = '';
                            for (var i = 0; i < hashedLength; i++) {
                                binaryString += String.fromCharCode(hashedUint8[i]);
                            }
                            var sessionStateHashed = window.btoa(binaryString).replace(/\+/g, '-').replace(/\//g, '_').replace(/=/g, '');
                            // Post comparison result back to origin
                            var postResult = sessionState === sessionStateHashed ? 'unchanged' : 'changed';
                            postChangedResult(event, postResult);
                        }).catch(() => {;
                            postChangedResult(event, 'error');
                        });
                } catch {
                    postChangedResult(event, 'error');
                }
            });
        }
    </script>
</body>
</html>";

        private readonly ProviderOptions providerOptions;
        private readonly string iframeFull;

        public CheckSessionHandler(ProviderOptions providerOptions)
        {
            this.providerOptions = providerOptions;
            var cookieNameScript =
                $"    <script id='checksession-cookie-name' type='application/json'>{this.providerOptions.CheckSessionCookieName}</script>\n";
            this.iframeFull = Iframe1 + cookieNameScript + Iframe2;
        }

        public async Task<bool> HandleRequest(HttpRequest httpRequest)
        {
            await httpRequest.HttpContext.Response.WriteHtmlContent(this.iframeFull);
            return true;
        }
    }
}
