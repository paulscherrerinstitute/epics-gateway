using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GatewayLogic.Services
{
    class EventsOnHold
    {
        SemaphoreSlim locker = new SemaphoreSlim(1);
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
        }
    }
}
