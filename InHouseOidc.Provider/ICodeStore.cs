// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Provider
{
    /// <summary>
    /// Interface to be implemented by the provider host API to store and retrieve codes used by flows.<br />
    /// Required for the authorization code flow.
    /// </summary>
    public interface ICodeStore
    {
        /// <summary>
        /// Consumes (logically deletes) a saved code.  Codes not found to consume should be ignored.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <param name="codeType">The code type of the code.</param>
        /// <param name="issuer">The issuer associated with the code.</param>
        /// <returns><see cref="Task"/>The task.</returns>
        Task ConsumeCode(string code, CodeType codeType, string issuer);

        /// <summary>
        /// Deletes a saved code.  Codes not found to delete should be ignored.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <param name="codeType">The code type of the code.</param>
        /// <param name="issuer">The issuer associated with the code.</param>
        /// <returns><see cref="Task"/>The task.</returns>
        Task DeleteCode(string code, CodeType codeType, string issuer);

        /// <summary>
        /// Retrieves a saved code.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <param name="codeType">The code type of the code.</param>
        /// <param name="issuer">The issuer associated with the code.</param>
        /// <returns><see cref="StoredCode"/> or null to indicate an unknown code.</returns>
        Task<StoredCode?> GetCode(string code, CodeType codeType, string issuer);

        /// <summary>
        /// Saves a code.
        /// </summary>
        /// <param name="storedCode"><see cref="StoredCode"/> the code details to save.</param>
        /// <returns><see cref="Task"/>The task.</returns>
        Task SaveCode(StoredCode storedCode);
    }
}
