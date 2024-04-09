// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common;
using InHouseOidc.Provider.Exception;
using InHouseOidc.Provider.Extension;
using InHouseOidc.Provider.Type;
using Microsoft.Extensions.DependencyInjection;

namespace InHouseOidc.Provider.Handler
{
    internal class SigningKeyHandler(
        IAsyncLock<SigningKeyHandler> asyncLock,
        ProviderOptions providerOptions,
        IServiceProvider serviceProvider,
        IUtcNow utcNow
    ) : ISigningKeyHandler
    {
        private readonly IAsyncLock<SigningKeyHandler> asyncLock = asyncLock;
        private readonly ProviderOptions providerOptions = providerOptions;
        private readonly IUtcNow utcNow = utcNow;
        private readonly ICertificateStore? certificateStore = serviceProvider.GetService<ICertificateStore>();
        private readonly List<SigningKey> signingKeys = [];
        private DateTimeOffset? expiry = null;

        public async Task<List<SigningKey>> Resolve()
        {
            // Check for previously resolved keys are still applicable
            if (this.signingKeys.Count > 0 && this.expiry.HasValue && this.expiry.Value > this.utcNow.UtcNow)
            {
                return this.signingKeys.ToList();
            }
            // Resolve new keys
            using var locker = this.asyncLock.Lock();
            if (this.expiry.HasValue && this.expiry.Value > this.utcNow.UtcNow)
            {
                // Signing keys were resolved on another thread while we waited
                return this.signingKeys.ToList();
            }
            this.signingKeys.Clear();
            this.signingKeys.AddRange(this.providerOptions.SigningKeys);
            if (this.certificateStore != null)
            {
                var storeSigningCertificates = await this.certificateStore.GetSigningCertificates();
                this.signingKeys.StoreSigningKeys(storeSigningCertificates);
            }
            if (this.signingKeys.Count == 0)
            {
                throw new InternalErrorException(
                    "No signing keys available.  Set via ProviderBuilder.SetSigningCertificates"
                        + " or implement ICertificateStore.GetSigningCertificates"
                );
            }
            this.expiry = this.utcNow.UtcNow.Add(this.providerOptions.StoreSigningKeyExpiry);
            return this.signingKeys.ToList();
        }
    }
}
