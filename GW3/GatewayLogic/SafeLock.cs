using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GatewayLogic
{
    class UsableLock : IDisposable
    {
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

    class LockInfo
    {
        public DateTime LockRequestedOn { get; } = DateTime.UtcNow;
        public string MemberName { get; set; }
        public string SourceFilePath { get; set; }
        public int SourceLineNumber { get; set; }
    }

    class SafeLock : IDisposable
    {
        static List<SafeLock> lockers = new List<SafeLock>();

        List<LockInfo> openLocks = new List<LockInfo>();
        public LockInfo Holder { get; private set; }
        public static int TotalLocks
        {
            get
            {
                lock (lockers)
                    return lockers.Count;
            }
        }

        SemaphoreSlim semaphore = new SemaphoreSlim(1);
        private bool disposed = false;

        static List<SafeLockCleanInfo> needToCleanup = new List<SafeLockCleanInfo>();
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

            lock (lockers)
                lockers.Add(this);
        }

        public static List<LockInfo> DeadLockCheck(TimeSpan? span = null)
        {
            if (!span.HasValue)
                span = TimeSpan.FromSeconds(10);
            var now = DateTime.UtcNow;
            List<SafeLock> copy;
            lock (lockers)
                copy = lockers.ToList();
            return copy.Where((row) =>
            {
                List<LockInfo> lockCopy;
                lock (row.openLocks)
                    lockCopy = row.openLocks.ToList();

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
            lock (openLocks)
                openLocks.Add(info);

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

            lock (openLocks)
                openLocks.Remove(info);
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
            lock (lockers)
                lockers.Remove(this);
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
            lock (needToCleanup)
            {
                foreach (var i in needToCleanup)
                    i.Object.semaphore.Dispose();
                needToCleanup.Clear();
            }
        }

        public static string[] LocksCreatedBy
        {
            get
            {
                lock (lockers)
                {
                    return lockers.Select(row => row.sourceFilePath + "." + row.memberName + ":" + row.sourceLineNumber).ToArray();
                }
            }
        }

        public bool IsDisposed => disposed;

        public static void Reset()
        {
            lock (needToCleanup)
                needToCleanup.Clear();

            lock (lockers)
                lockers.Clear();
        }
    }
}
