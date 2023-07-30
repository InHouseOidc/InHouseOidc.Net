// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Common
{
    public interface IAsyncLock<TInstance>
    {
        IDisposable Lock();
        IDisposable TryLock(int millisecondsTimeout, CancellationToken cancellationToken, out bool locked);
    }
}
