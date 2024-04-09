// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InHouseOidc.Common.Test
{
    [TestClass]
    public class AsyncLockTest
    {
        [TestMethod]
        public void AsyncLock_LockUntilDisposed()
        {
            // Arrange
            var asyncLock = new AsyncLock<TestInstance>();
            // Act/Assert
            Assert.IsFalse(asyncLock.IsLocked);
            var releaser = asyncLock.Lock();
            Assert.IsTrue(asyncLock.IsLocked);
            releaser.Dispose();
            Assert.IsFalse(asyncLock.IsLocked);
        }

        [TestMethod]
        public async Task AsyncLock_WaitWhileLocked()
        {
            // Arrange
            var asyncLock = new AsyncLock<TestInstance>();
            // Act/Assert
            Assert.IsFalse(asyncLock.IsLocked);
            var releaser = asyncLock.Lock();
            Assert.IsTrue(asyncLock.IsLocked);
            // Start another task that waits on the lock
            var isStarted = false;
            var isDoneWaiting = false;
            var signalStarted = new AutoResetEvent(false);
            var waiter = Task.Run(() =>
            {
                isStarted = true;
                signalStarted.Set();
                var waiterReleaser = asyncLock.Lock();
                isDoneWaiting = true;
                waiterReleaser.Dispose();
            });
            WaitHandle.WaitAll(new[] { signalStarted });
            Assert.IsTrue(isStarted);
            Assert.IsFalse(isDoneWaiting);
            Assert.IsTrue(asyncLock.IsLocked);
            // Release the main thread lock
            releaser.Dispose();
            await Task.WhenAll([waiter]);
            Assert.IsTrue(waiter.IsCompleted);
            Assert.IsTrue(isDoneWaiting);
            Assert.IsFalse(asyncLock.IsLocked);
        }

        [TestMethod]
        public async Task AsyncLock_TryLock_AlreadyLocked()
        {
            // Arrange
            var asyncLock = new AsyncLock<TestInstance>();
            Assert.IsFalse(asyncLock.IsLocked);
            var signalStarted = new AutoResetEvent(false);
            var signalFinished = new AutoResetEvent(false);
            var waiter = Task.Run(() =>
            {
                // Lock the AsyncLock for 5 seconds, order until told to release
                var waiterRelease = asyncLock.Lock();
                signalStarted.Set();
                signalFinished.WaitOne(5000);
                waiterRelease.Dispose();
            });
            WaitHandle.WaitAll(new[] { signalStarted });
            // Act
            var releaser = asyncLock.TryLock(100, CancellationToken.None, out var locked);
            // Assert
            Assert.IsFalse(locked);
            releaser.Dispose();
            Assert.IsTrue(asyncLock.IsLocked);
            signalFinished.Set();
            await Task.WhenAll([waiter]);
            Assert.IsFalse(asyncLock.IsLocked);
        }

        [TestMethod]
        public void AsyncLock_TryLock_Lockable()
        {
            // Arrange
            var asyncLock = new AsyncLock<TestInstance>();
            Assert.IsFalse(asyncLock.IsLocked);
            // Act
            var releaser = asyncLock.TryLock(100, CancellationToken.None, out var locked);
            // Assert
            Assert.IsTrue(locked);
            Assert.IsTrue(asyncLock.IsLocked);
            releaser.Dispose();
            Assert.IsFalse(asyncLock.IsLocked);
        }

        private class TestInstance { }
    }
}
