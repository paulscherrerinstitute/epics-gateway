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
        static SynchronizedCollection<SafeLock> lockers = new SynchronizedCollection<SafeLock>();

        SynchronizedCollection<LockInfo> openLocks = new SynchronizedCollection<LockInfo>();
        public LockInfo Holder { get; private set; }

        SemaphoreSlim semaphore = new SemaphoreSlim(1);
        private bool disposed = false;

        static SynchronizedCollection<SafeLockCleanInfo> needToCleanup = new SynchronizedCollection<SafeLockCleanInfo>();
        static Thread cleanupThread;

        static SafeLock()
        {
            cleanupThread = new Thread(obj =>
            {
                while (true)
                {
                    Thread.Sleep(200);
                    var now = DateTime.UtcNow;
                    var list = needToCleanup.ToList().Where(row => row.When >= now).ToList();

                    foreach (var i in list)
                    { 
                        i.Object.semaphore.Dispose();
                        needToCleanup.Remove(i);
                    }
                }
            });
            cleanupThread.IsBackground = true;
            cleanupThread.Start();
        }

        public SafeLock()
        {
            lockers.Add(this);
        }

        public static List<LockInfo> DeadLockCheck(TimeSpan? span = null)
        {
            if (!span.HasValue)
                span = TimeSpan.FromSeconds(10);
            var now = DateTime.UtcNow;
            try
            {
                return lockers.ToList().Where(row => row.openLocks.Any() && row.Holder != null && (now - (row.Holder?.LockRequestedOn ?? now)) > span)
                    .Select(row => row.Holder).ToList();
            }
            catch
            {
                return new List<LockInfo>();
            }
        }

        public UsableLock Aquire([System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
                            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
                            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
        {
            Wait(memberName, sourceFilePath, sourceLineNumber);
            return new UsableLock(this);
        }

        public void Wait([System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
                            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
                            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
        {
            LockInfo info = new LockInfo
            {
                MemberName = memberName,
                SourceFilePath = sourceFilePath,
                SourceLineNumber = sourceLineNumber
            };
            openLocks.Add(info);

            semaphore.Wait();

            openLocks.Remove(info);
            Holder = info;
        }

        public void Release([System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
                            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
                            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
        {
            if (disposed)
                return;
            Holder = null;

            semaphore.Release();
        }

        public void Dispose()
        {
            disposed = true;
            lockers.Remove(this);
            //semaphore.Dispose();

            needToCleanup.Add(new SafeLockCleanInfo { When = DateTime.UtcNow.AddSeconds(2), Object = this });
        }

        private class SafeLockCleanInfo
        {
            public DateTime When { get; set; }
            public SafeLock Object { get; set; }
        }
    }
}
