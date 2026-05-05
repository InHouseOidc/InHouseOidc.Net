// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

// Playwright tests must not run in parallel — each test gets its own browser page
// but sharing a browser context across concurrent tests causes flaky auth state.
[assembly: Parallelize(Scope = ExecutionScope.ClassLevel)]
