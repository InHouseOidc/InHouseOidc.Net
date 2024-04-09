// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Common.Constant
{
    public static class JsonWebTokenClaim
    {
        public const string Address = "address";
        public const string Audience = "aud";
        public const string AuthenticationMethodReference = "amr";
        public const string AuthenticationTime = "auth_time";
        public const string BirthDate = "birthdate";
        public const string ClientId = "client_id";
        public const string Email = "email";
        public const string EmailVerified = "email_verified";
        public const string Exp = "exp";
        public const string FamilyName = "family_name";
        public const string Gender = "gender";
        public const string GivenName = "given_name";
        public const string IdentityProvider = "idp";
        public const string IssuedAt = "iat";
        public const string Locale = "locale";
        public const string MiddleName = "middle_name";
        public const string Name = "name";
        public const string Nickname = "nickname";
        public const string Nonce = "nonce";
        public const string PhoneNumber = "phone_number";
        public const string PhoneNumberVerified = "phone_number_verified";
        public const string Picture = "picture";
        public const string PreferredUsername = "preferred_username";
        public const string Profile = "profile";
        public const string Role = "role";
        public const string Scope = "scope";
        public const string SessionId = "sid";
        public const string Subject = "sub";
        public const string Typ = "typ";
        public const string UpdatedAt = "updated_at";
        public const string Website = "website";
        public const string X5t = "x5t";
        public const string ZoneInfo = "zoneinfo";

        public static readonly List<string> AddressClaims = new([Address]);

        public static readonly List<string> EmailClaims = new([Email, EmailVerified]);

        public static readonly List<string> PhoneClaims = new([PhoneNumber, PhoneNumberVerified]);

        public static readonly List<string> ProfileClaims =
            new(
                [
                    Name,
                    FamilyName,
                    GivenName,
                    MiddleName,
                    Nickname,
                    PreferredUsername,
                    Profile,
                    Picture,
                    Website,
                    Gender,
                    BirthDate,
                    ZoneInfo,
                    Locale,
                    UpdatedAt,
                ]
            );

        public static readonly List<string> RoleClaims = new([Role]);

        public static readonly List<string> StandardClaims = AddressClaims
            .Concat(EmailClaims)
            .Concat(PhoneClaims)
            .Concat(ProfileClaims)
            .ToList();
    }
}
