// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Common
{
    public class AsyncLock<TInstance> : IAsyncLock<TInstance>
    {
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);
        private readonly Releaser<TInstance> releaser;

        public AsyncLock()
        {
            this.releaser = new Releaser<TInstance>(this);
        }

        public bool IsLocked
        {
            get { return this.semaphore.CurrentCount <= 0; }
        }

        public IDisposable Lock()
        {
            this.semaphore.Wait();
            return this.releaser;
        }

        public IDisposable TryLock(int millisecondsTimeout, CancellationToken cancellationToken, out bool locked)
        {
            if (this.semaphore.Wait(millisecondsTimeout, cancellationToken))
            {
                locked = true;
                return this.releaser;
            }
            locked = false;
            return new NotLocked();
        }

        internal void Release()
        {
            this.semaphore.Release();
        }

        internal class Releaser<TReleaser> : IDisposable
        {
            private readonly AsyncLock<TReleaser> asyncLock;

            public Releaser(AsyncLock<TReleaser> asyncLock)
            {
                this.asyncLock = asyncLock;
            }

            public void Dispose()
            {
                this.asyncLock.Release();
            }
        }

        internal class NotLocked : IDisposable
        {
            public void Dispose()
            {
                // Nothing to dispose
            }
        }
    }
}
