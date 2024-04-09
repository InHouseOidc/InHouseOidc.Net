// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Provider
{
    public class StoredCode
    {
        public StoredCode(string code, CodeType codeType, string content, string issuer, string subject)
        {
            this.Code = code;
            this.CodeType = codeType;
            this.Content = content;
            this.Issuer = issuer;
            this.Subject = subject;
        }

        [StringLength(256)]
        [Required]
        public string Code { get; set; }

        [StringLength(256)]
        [Required]
        public CodeType CodeType { get; set; }
        public int ConsumeCount { get; set; }

        [Required]
        public string Content { get; set; }
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset Expiry { get; set; }

        [StringLength(256)]
        [Required]
        public string Issuer { get; set; }

        [StringLength(256)]
        [Required]
        public string Subject { get; set; }
    }
}
