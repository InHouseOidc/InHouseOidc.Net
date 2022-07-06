// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Common
{
    public interface IUtcNow
    {
        /// <summary>
        /// Gets the current UTC date and time.
        /// </summary>
        /// <returns><see cref="DateTimeOffset"/>.</returns>
        DateTimeOffset UtcNow { get; }
    }
}
