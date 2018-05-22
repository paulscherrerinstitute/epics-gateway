using System;
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

    class SafeLock : IDisposable
    {
        SemaphoreSlim semaphore = new SemaphoreSlim(1);

        public UsableLock Lock
        {
            get
            {
                Wait();
                return new UsableLock(this);
            }
        }

        public void Wait()
        {
            semaphore.Wait();
        }

        public void Release()
        {
            semaphore.Release();
        }

        public void Dispose()
        {
            semaphore.Dispose();
        }
    }
}
