// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Provider;

namespace InHouseOidc.Example.Provider
{
    public class CodeStore(ILogger<CodeStore> logger) : ICodeStore
    {
        private readonly ConcurrentDictionary<string, StoredCode> codes = new();
        private readonly ILogger<CodeStore> logger = logger;
        private readonly JsonSerializerOptions jsonSerializerOptions =
            new()
            {
                Converters = { new JsonStringEnumConverter() },
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true,
            };

        public Task ConsumeCode(string code, CodeType codeType, string issuer)
        {
            if (this.codes.TryGetValue(FormatKey(code, codeType, issuer), out var storedCode))
            {
                if (storedCode == null)
                {
                    this.logger.LogInformation("Code not found: {code} {codeType} {issuer}", code, codeType, issuer);
                    return Task.FromResult<StoredCode?>(null);
                }
                storedCode.ConsumeCount++;
                this.logger.LogInformation(
                    "Consume code: {code} {codeType} {issuer} {consumeCount}",
                    code,
                    codeType,
                    issuer,
                    storedCode.ConsumeCount
                );
            }
            else
            {
                this.logger.LogInformation(
                    "Code not found to consume: {code} {codeType} {issuer}",
                    code,
                    codeType,
                    issuer
                );
            }
            return Task.CompletedTask;
        }

        public Task DeleteCode(string code, CodeType codeType, string issuer)
        {
            if (this.codes.TryRemove(FormatKey(code, codeType, issuer), out var _))
            {
                this.logger.LogInformation("Deleted code: {code} {codeType} {issuer}", code, codeType, issuer);
            }
            else
            {
                this.logger.LogInformation(
                    "Code not found to remove: {code} {codeType} {issuer}",
                    code,
                    codeType,
                    issuer
                );
            }
            return Task.CompletedTask;
        }

        public Task<StoredCode?> GetCode(string code, CodeType codeType, string issuer)
        {
            if (this.codes.TryGetValue(FormatKey(code, codeType, issuer), out var storedCode))
            {
                if (storedCode == null)
                {
                    this.logger.LogInformation("Code not found: {code} {codeType} {issuer}", code, codeType, issuer);
                    return Task.FromResult<StoredCode?>(null);
                }
                this.logger.LogInformation("Loaded code: {code} {codeType} {issuer}", code, codeType, issuer);
                return Task.FromResult<StoredCode?>(storedCode);
            }
            else
            {
                this.logger.LogInformation("Code not found: {code} {codeType} {issuer}", code, codeType, issuer);
            }
            return Task.FromResult<StoredCode?>(null);
        }

        public Task SaveCode(StoredCode storedCode)
        {
            if (storedCode.Issuer == null)
            {
                throw new InvalidOperationException("StoredCode.Issuer is required");
            }
            if (storedCode.Code == null)
            {
                throw new InvalidOperationException("StoredCode.Code is required");
            }
            if (this.codes.TryAdd(FormatKey(storedCode.Code, storedCode.CodeType, storedCode.Issuer), storedCode))
            {
                this.logger.LogInformation(
                    "Code added: {StoredCode}",
                    JsonSerializer.Serialize(storedCode, this.jsonSerializerOptions)
                );
            }
            else
            {
                this.logger.LogInformation(
                    "Code already exists: {code} {codeType} {issuer}",
                    storedCode.Code,
                    storedCode.CodeType,
                    storedCode.Issuer
                );
            }
            return Task.CompletedTask;
        }

        private static string FormatKey(string code, CodeType codeType, string issuer)
        {
            return $"{code}:{codeType}:{issuer}";
        }
    }
}
