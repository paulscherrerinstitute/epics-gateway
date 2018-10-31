using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace GatewayLogic
{
    internal class UsableLock : IDisposable
    {
        //private SemaphoreSlim lockObject = new SemaphoreSlim(1);
        private SafeLock locker;

        public UsableLock(SafeLock locker)
        {
            this.locker = locker;
        }

        public void Dispose()
        {
            if (locker == null)
                return;
            locker.Release();
            locker = null;
        }
    }

    internal class LockInfo
    {
        //public SemaphoreSlim RowLock { get; } = new SemaphoreSlim(1);
        public DateTime LockRequestedOn { get; } = DateTime.UtcNow;
        public string MemberName { get; set; }
        public string SourceFilePath { get; set; }
        public int SourceLineNumber { get; set; }
    }

    internal class SafeLock : IDisposable
    {
        private static List<SafeLock> lockers = new List<SafeLock>();
        private static SemaphoreSlim lockObject = new SemaphoreSlim(1);
        public SemaphoreSlim openLockLocker { get; } = new SemaphoreSlim(1);
        private List<LockInfo> openLocks = new List<LockInfo>();
        public LockInfo Holder { get; private set; }
        public static int TotalLocks
        {
            get
            {
                //lock (lockers)
                try
                {
                    lockObject.Wait();
                    return lockers.Count;
                }
                finally
                {
                    lockObject.Release();
                }
            }
        }

        private SemaphoreSlim semaphore = new SemaphoreSlim(1);
        private bool disposed = false;
        private static SemaphoreSlim needToCleanupLock = new SemaphoreSlim(1);
        private static List<SafeLockCleanInfo> needToCleanup = new List<SafeLockCleanInfo>();
        private readonly string memberName;

        public string sourceFilePath { get; }

        private readonly int sourceLineNumber;

        public SafeLock([System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
                    [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
                    [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
        {
            this.memberName = memberName;
            this.sourceFilePath = sourceFilePath;
            this.sourceLineNumber = sourceLineNumber;

            //lock (lockers)
            try
            {
                lockObject.Wait();
                lockers.Add(this);
            }
            finally
            {
                lockObject.Release();
            }
        }

        public static List<LockInfo> DeadLockCheck(TimeSpan? span = null)
        {
            if (!span.HasValue)
                span = TimeSpan.FromSeconds(10);
            var now = DateTime.UtcNow;
            List<SafeLock> copy;
            //lock (lockers)
            try
            {
                lockObject.Wait();
                copy = lockers.ToList();
            }
            finally
            {
                lockObject.Release();
            }
            return copy.Where((row) =>
            {
                List<LockInfo> lockCopy;
                //lock (row.openLocks)
                try
                {
                    row.openLockLocker.Wait();
                    lockCopy = row.openLocks.ToList();
                }
                finally
                {
                    row.openLockLocker.Release();
                }

                return lockCopy.Count > 0 && (now - (row.Holder?.LockRequestedOn ?? now)) > span;
            })
                .Select(row => row.Holder).ToList();
        }

        public UsableLock Aquire(int timeout, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
                    [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
                    [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
        {
            Wait(timeout, memberName, sourceFilePath, sourceLineNumber);
            return new UsableLock(this);
        }

        public UsableLock Aquire([System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
                            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
                            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
        {
            Wait(0, memberName, sourceFilePath, sourceLineNumber);
            return new UsableLock(this);
        }

        public void Wait(int timeout = 0, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
                            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
                            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
        {
            LockInfo info = new LockInfo
            {
                MemberName = memberName,
                SourceFilePath = sourceFilePath,
                SourceLineNumber = sourceLineNumber
            };
            //lock (openLocks)
            try
            {
                openLockLocker.Wait();
                openLocks.Add(info);
            }
            finally
            {
                openLockLocker.Release();
            }

            if (timeout != 0)
            {
                try
                {
                    semaphore.Wait(timeout);
                }
                catch
                {
                    openLocks.Remove(info);
                    throw;
                }
            }
            else
                semaphore.Wait();

            //lock (openLocks)
            try
            {
                openLockLocker.Wait();
                openLocks.Remove(info);
            }
            finally
            {
                openLockLocker.Release();
            }
            Holder = info;
        }

        public void Release([System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
                            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
                            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
        {
            if (!disposed)
                Holder = null;
            semaphore.Release();
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            //lock (lockers)
            try
            {
                lockObject.Wait();
                lockers.Remove(this);
            }
            finally
            {
                lockObject.Release();
            }
            //semaphore.Dispose();

            semaphore.Dispose();

            /*lock (needToCleanup)
                needToCleanup.Add(new SafeLockCleanInfo { When = DateTime.UtcNow.AddSeconds(2), Object = this });*/
        }

        private class SafeLockCleanInfo
        {
            public DateTime When { get; set; }
            public SafeLock Object { get; set; }
        }

        public static void Clean()
        {
            //lock (needToCleanup)
            try
            {
                needToCleanupLock.Wait();
                foreach (var i in needToCleanup)
                    i.Object.semaphore.Dispose();
                needToCleanup.Clear();
            }
            finally
            {
                needToCleanupLock.Release();
            }
        }

        public static string[] LocksCreatedBy
        {
            get
            {
                //lock (lockers)
                try
                {
                    SafeLock.lockObject.Wait();
                    return lockers.Select(row => row.sourceFilePath + "." + row.memberName + ":" + row.sourceLineNumber).ToArray();
                }
                finally
                {
                    SafeLock.lockObject.Release();
                }
            }
        }

        public bool IsDisposed => disposed;

        public static void Reset()
        {
            //lock (needToCleanup)
            try
            {
                needToCleanupLock.Wait();
                needToCleanup.Clear();
            }
            finally
            {
                needToCleanupLock.Release();
            }

            //lock (lockers)
            try
            {
                SafeLock.lockObject.Wait();
                lockers.Clear();
            }
            finally
            {
                SafeLock.lockObject.Release();
            }
        }
    }
}
