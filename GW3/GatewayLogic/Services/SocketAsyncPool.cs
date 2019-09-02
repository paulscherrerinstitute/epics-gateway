using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace GatewayLogic.Services
{
    class SocketAsyncPool
    {
        ConcurrentStack<SocketAsyncEventArgs> pool;
        public SocketAsyncPool(int capacity,BufferManager bufferManager)
        {
            pool = new ConcurrentStack<SocketAsyncEventArgs>();

            SocketAsyncEventArgs readWriteEventArg;

            for (int i = 0; i < capacity; i++)
            {
                //Pre-allocate a set of reusable SocketAsyncEventArgs
                readWriteEventArg = new SocketAsyncEventArgs();
                readWriteEventArg.UserToken = new AsyncUserToken();

                // assign a byte buffer from the buffer pool to the SocketAsyncEventArg object
                bufferManager.SetBuffer(readWriteEventArg);

                // add SocketAsyncEventArg to the pool
                pool.Push(readWriteEventArg);
            }
        }

        public void Push(SocketAsyncEventArgs item)
        {
            item.RemoveEvents(nameof(item.Completed));
            pool.Push(item);
        }

        public SocketAsyncEventArgs Pop()
        {
            SocketAsyncEventArgs result;
            if (pool.TryPop(out result))
                return result;
            return null;
        }

        public int Count
        {
            get
            {
                return pool.Count;
            }
        }
    }
}
