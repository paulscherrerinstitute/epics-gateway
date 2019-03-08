using System.Collections.Concurrent;
using System.Net;

namespace GatewayLogic.Services
{
    internal class EventsOnHold
    {
        private ConcurrentDictionary<IPEndPoint, bool> disabledEndPoints = new ConcurrentDictionary<IPEndPoint, bool>();
        internal void Remove(IPEndPoint sender)
        {
            bool outVal;
            disabledEndPoints.TryRemove(sender, out outVal);
        }

        internal void Add(IPEndPoint sender)
        {
            disabledEndPoints.TryAdd(sender, true);
        }

        internal bool Contains(IPEndPoint sender)
        {
            return disabledEndPoints.ContainsKey(sender);
        }


        /*SemaphoreSlim locker = new SemaphoreSlim(1);
        HashSet<IPEndPoint> disabledEndPoints = new HashSet<IPEndPoint>();

        internal void Remove(IPEndPoint sender)
        {
            try
            {
                locker.Wait();
                disabledEndPoints.Remove(sender);
            }
            finally
            {
                locker.Release();
            }
        }

        internal void Add(IPEndPoint sender)
        {
            try
            {
                locker.Wait();
                disabledEndPoints.Add(sender);
            }
            finally
            {
                locker.Release();
            }
        }

        internal bool Contains(IPEndPoint sender)
        {
            try
            {
                locker.Wait();
                return disabledEndPoints.Contains(sender);
            }
            finally
            {
                locker.Release();
            }
        }*/
    }
}
