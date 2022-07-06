// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Common.Constant
{
    public class AuthorizationEndpointConstant
    {
        public const string ClientId = "client_id";
        public const string Code = "code";
        public const string CodeChallenge = "code_challenge";
        public const string CodeChallengeMethod = "code_challenge_method";
        public const string Error = "error";
        public const string IdTokenHint = "id_token_hint";
        public const string MaxAge = "max_age";
        public const string Nonce = "nonce";
        public const string None = "none";
        public const string Prompt = "prompt";
        public const string RedirectUri = "redirect_uri";
        public const string Request = "request";
        public const string ResponseMode = "response_mode";
        public const string ResponseType = "response_type";
        public const string ReturnUrl = "ReturnUrl";
        public const string S256 = "S256";
        public const string Scope = "scope";
        public const string SessionState = "session_state";
        public const string State = "state";

        public static readonly List<string> AuthorizationCodeRequiredFields =
            new(new[] { ClientId, CodeChallenge, CodeChallengeMethod, RedirectUri, ResponseType, Scope });

        public static readonly List<string> AuthorizationCodeWithoutPkceRequiredFields =
            new(new[] { ClientId, RedirectUri, ResponseType, Scope });
    }
}
