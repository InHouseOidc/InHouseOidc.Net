// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Provider
{
    public class StoredCode(string code, CodeType codeType, string content, string issuer, string subject)
    {
        [StringLength(256)]
        [Required]
        public string Code { get; set; } = code;

        [StringLength(256)]
        [Required]
        public CodeType CodeType { get; set; } = codeType;
        public int ConsumeCount { get; set; }

        [Required]
        public string Content { get; set; } = content;
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset Expiry { get; set; }

        [StringLength(256)]
        [Required]
        public string Issuer { get; set; } = issuer;

        [StringLength(256)]
        [Required]
        public string Subject { get; set; } = subject;
    }
}
