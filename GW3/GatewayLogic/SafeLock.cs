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
        static SemaphoreSlim lockLockers = new SemaphoreSlim(1);
        static List<SafeLock> lockers = new List<SafeLock>();
        static List<SafeLockCleanInfo> needToCleanup = new List<SafeLockCleanInfo>();
        static Thread cleanupThread;

        SemaphoreSlim openLockLocker = new SemaphoreSlim(1);

        SynchronizedCollection<LockInfo> openLocks = new SynchronizedCollection<LockInfo>();
        //List<LockInfo> openLocks = new List<LockInfo>();
        public LockInfo Holder { get; private set; }

        SemaphoreSlim semaphore = new SemaphoreSlim(1);
        private bool disposed = false;

        static SafeLock()
        {
            cleanupThread = new Thread(obj =>
              {
                  while (true)
                  {
                      Thread.Sleep(200);
                      List<SafeLockCleanInfo> list;
                      try
                      {
                          lockLockers.Wait();
                          var now = DateTime.UtcNow;
                          list = needToCleanup.Where(row => row.When >= now).ToList();
                          needToCleanup.RemoveAll(row => row.When >= now);
                      }
                      finally
                      {
                          lockLockers.Release();
                      }

                      foreach (var i in list)
                      {
                          i.Object.semaphore.Dispose();
                          i.Object.openLockLocker.Dispose();
                      }
                  }
              });
            cleanupThread.IsBackground = true;
            cleanupThread.Start();
        }

        public SafeLock()
        {
            try
            {
                lockLockers.Wait();
                lockers.Add(this);
            }
            finally
            {
                lockLockers.Release();
            }
        }

        public static List<LockInfo> DeadLockCheck(TimeSpan? span = null)
        {
            if (!span.HasValue)
                span = TimeSpan.FromSeconds(10);
            var now = DateTime.UtcNow;
            try
            {
                lockLockers.Wait();
                return lockers.Where(row => row.OpenLocks.Any() && row.Holder != null && (now - (row.Holder?.LockRequestedOn ?? now)) > span)
                    .Select(row => row.Holder).ToList();
            }
            finally
            {
                lockLockers.Release();
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
            try
            {
                openLockLocker.Wait();
                openLocks.Add(info);
            }
            finally
            {
                openLockLocker.Release();
            }
            semaphore.Wait();
            if (disposed)
                return;
            try
            {
                openLockLocker.Wait();
                openLocks.Remove(info);
                Holder = info;
            }
            finally
            {
                openLockLocker.Release();
            }
        }

        public void Release([System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
                            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
                            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
        {
            if (disposed)
                return;
            try
            {
                openLockLocker.Wait();
                Holder = null;
            }
            finally
            {
                openLockLocker.Release();
            }

            semaphore.Release();
        }

        public List<LockInfo> OpenLocks
        {
            get
            {
                try
                {
                    openLockLocker.Wait();
                    return openLocks.ToList();
                }
                finally
                {
                    openLockLocker.Release();
                }
            }
        }

        public void Dispose()
        {
            disposed = true;
            try
            {
                lockLockers.Wait();
                lockers.Remove(this);
                needToCleanup.Add(new SafeLockCleanInfo { When = DateTime.UtcNow.AddSeconds(2), Object = this });
            }
            finally
            {
                lockLockers.Release();
            }

            /*semaphore.Dispose();
            openLockLocker.Dispose();*/
        }

        private class SafeLockCleanInfo
        {
            public DateTime When { get; set; }
            public SafeLock Object { get; set; }
        }
    }
}
