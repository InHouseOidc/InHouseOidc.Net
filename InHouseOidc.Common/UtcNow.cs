// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Common
{
    public class UtcNow : IUtcNow
    {
        DateTimeOffset IUtcNow.UtcNow => DateTimeOffset.UtcNow;
    }
}
