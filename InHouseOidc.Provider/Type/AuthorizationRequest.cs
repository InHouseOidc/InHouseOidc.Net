// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Provider.Type
{
    internal class AuthorizationRequest(string clientId, string redirectUri, ResponseType responseType, string scope)
    {
        public List<AuthorizationRequestClaim> AuthorizationRequestClaims { get; set; } = [];
        public string ClientId { get; set; } = clientId;
        public string? CodeChallenge { get; set; }
        public CodeChallengeMethod? CodeChallengeMethod { get; set; }
        public string? IdTokenHint { get; set; }
        public int? MaxAge { get; set; }
        public string? Nonce { get; set; }
        public Prompt? Prompt { get; set; }
        public string RedirectUri { get; set; } = redirectUri;
        public ResponseMode? ResponseMode { get; set; }
        public ResponseType ResponseType { get; set; } = responseType;
        public string Scope { get; set; } = scope;
        public DateTimeOffset? SessionExpiryUtc { get; set; }
        public string? SessionState { get; set; }
        public string? State { get; set; }

        internal string GetClaimValue(string claimType)
        {
            var claim = this.AuthorizationRequestClaims.Single(c => c.Type == claimType);
            return claim.Value;
        }
    }
}
