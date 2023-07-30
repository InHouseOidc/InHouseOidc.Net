// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common;
using InHouseOidc.Provider.Exception;
using InHouseOidc.Provider.Extension;
using InHouseOidc.Provider.Type;
using Microsoft.Extensions.DependencyInjection;

namespace InHouseOidc.Provider.Handler
{
    internal class SigningKeyHandler : ISigningKeyHandler
    {
        private readonly IAsyncLock<SigningKeyHandler> asyncLock;
        private readonly ProviderOptions providerOptions;
        private readonly IUtcNow utcNow;
        private readonly ICertificateStore? certificateStore;
        private readonly List<SigningKey> signingKeys = new();
        private DateTimeOffset? expiry = null;

        public SigningKeyHandler(
            IAsyncLock<SigningKeyHandler> asyncLock,
            ProviderOptions providerOptions,
            IServiceProvider serviceProvider,
            IUtcNow utcNow
        )
        {
            this.asyncLock = asyncLock;
            this.providerOptions = providerOptions;
            this.utcNow = utcNow;
            this.certificateStore = serviceProvider.GetService<ICertificateStore>();
        }

        public async Task<List<SigningKey>> Resolve()
        {
            // Check for previously resolved keys are still applicable
            if (this.signingKeys.Any() && this.expiry.HasValue && this.expiry.Value > this.utcNow.UtcNow)
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
            if (!this.signingKeys.Any())
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
