using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;

namespace GatewayLogic.Services
{
    internal class MonitorInformation : IDisposable
    {
        //private readonly SafeLock dictionaryLock = new SafeLock();
        //private readonly ConcurrentBag<MonitorInformationDetail> monitors = new ConcurrentBag<MonitorInformationDetail>();
        private readonly ConcurrentDictionary<uint, MonitorInformationDetail> monitorLookup = new ConcurrentDictionary<uint, MonitorInformationDetail>();
        private readonly ConcurrentDictionary<string, MonitorInformationDetail> clientLookup = new ConcurrentDictionary<string, MonitorInformationDetail>();

        private uint nextId = 1;

        //object counterLock = new object();

        /*~MonitorInformation()
        {
            dictionaryLock.Dispose();
        }*/

        public class MonitorInformationDetail : IDisposable
        {
            public ChannelInformation.ChannelInformationDetails ChannelInformation { get; }


            public uint GatewayId { get; }
            public ushort DataType { get; }
            public uint DataCount { get; }
            public bool FirstTime { get; set; } = true;
            public bool HasReceivedFirstResult { get; set; } = false;
            public ushort MonitorMask { get; internal set; }
            public MonitorInformation MonitorInformation { get; }

            //internal SafeLock clientsLock = new SafeLock();
            SemaphoreSlim clientsLock = new SemaphoreSlim(1);
            public List<ClientId> clients = new List<ClientId>();

            public MonitorInformationDetail(uint id, ChannelInformation.ChannelInformationDetails channelInformation, ushort dataType, uint dataCount, UInt16 monitorMask, MonitorInformation monitorInformation)
            {
                GatewayId = id;
                ChannelInformation = channelInformation;
                DataCount = dataCount;
                DataType = dataType;
                MonitorMask = monitorMask;
                MonitorInformation = monitorInformation;
            }

            ~MonitorInformationDetail()
            {
                clientsLock.Dispose();
            }

            internal void AddClient(ClientId clientId)
            {
                //using (clientsLock.Aquire())
                try
                {
                    clientsLock.Wait();
                    if (!clients.Any(row => row.Client == clientId.Client && row.Id == clientId.Id))
                        clients.Add(clientId);
                }
                finally
                {
                    clientsLock.Release();
                }
                //using (MonitorInformation.dictionaryLock.Aquire())
                MonitorInformation.clientLookup.TryAdd(clientId.Client.ToString() + ";" + clientId.Id.ToString(), this);
            }

            internal void RemoveClient(Gateway gateway, IPEndPoint endPoint, uint clientId)
            {
                var needToDeleteThis = false;

                //using (clientsLock.Aquire())
                try
                {
                    clientsLock.Wait();
                    clients.RemoveAll(row => row.Id == clientId && row.Client == endPoint);
                    // No more clients, we should cancel the monitor
                    if (clients.Count == 0)
                        needToDeleteThis = true;
                }
                finally
                {
                    clientsLock.Release();
                }

                //using (MonitorInformation.dictionaryLock.Aquire())
                MonitorInformationDetail outVal;
                MonitorInformation.clientLookup.TryRemove(endPoint.ToString() + ";" + clientId.ToString(), out outVal);

                if (needToDeleteThis)
                {
                    //using (gateway.MonitorInformation.dictionaryLock.Aquire())
                    {
                        gateway.MonitorInformation.monitorLookup.TryRemove(this.GatewayId, out outVal);
                        //gateway.MonitorInformation.monitors.TryRemove(this, out outVal);
                    }
                    this.Drop();
                }
            }

            internal IEnumerable<ClientId> GetClients()
            {
                //using (clientsLock.Aquire())
                try
                {
                    clientsLock.Wait();
                    return clients.ToList();
                }
                finally
                {
                    clientsLock.Release();
                }
            }

            internal void Drop()
            {
                var newPacket = DataPacket.Create(0);
                newPacket.Command = 2;
                newPacket.DataType = DataType;
                newPacket.DataCount = DataCount;
                newPacket.Parameter1 = ChannelInformation.ServerId.Value;
                newPacket.Parameter2 = GatewayId;
                try
                {
                    this.ChannelInformation.TcpConnection.Send(newPacket);
                }
                catch
                {
                }
            }

            public void Dispose()
            {
                //clientsLock.Dispose();
            }

            internal bool Any(Func<ClientId, bool> condition)
            {
                //using (clientsLock.Aquire())
                try
                {
                    clientsLock.Wait();
                    return clients.Any(condition);
                }
                finally
                {
                    clientsLock.Release();
                }
            }
        }

        public MonitorInformationDetail Get(ChannelInformation.ChannelInformationDetails channelInformation, ushort dataType, uint dataCount, UInt16 monitorMask)
        {
            /*using (dictionaryLock.Aquire())
            {*/
            MonitorInformationDetail monitor = null;
            if (dataCount != 0)
                monitor = monitorLookup.Values.FirstOrDefault(row => row.ChannelInformation == channelInformation
                    && row.DataType == dataType
                    && row.DataCount == dataCount
                    && row.MonitorMask == monitorMask);
            if (monitor == null)
            {
                monitor = new MonitorInformationDetail(nextId++, channelInformation, dataType, dataCount, monitorMask, this);
                //monitors.Add(monitor);
                monitorLookup.TryAdd(monitor.GatewayId, monitor);
            }
            return monitor;
            //}
        }

        public MonitorInformationDetail GetByGatewayId(uint id)
        {
            /*using (dictionaryLock.Aquire())
            {*/
            //return monitors.FirstOrDefault(row => row.GatewayId == id);
            /*if (monitorLookup.ContainsKey(id))
                return monitorLookup[id];*/
            MonitorInformationDetail result = null;
            if (monitorLookup.TryGetValue(id, out result))
                return result;
            return null;
            //}
        }

        public MonitorInformationDetail GetByClientId(IPEndPoint clientEndPoint, uint clientId)
        {
            var key = clientEndPoint.ToString() + ";" + clientId;
            /*using (dictionaryLock.Aquire())
            {*/
            MonitorInformationDetail result = null;
            if (clientLookup.TryGetValue(key, out result))
                return result;
            return null;
            /*}*/


            /*List<MonitorInformationDetail> copy;

        using (dictionaryLock.Aquire())
            copy = monitors.ToList();

        return copy.FirstOrDefault(row => row.Any(r2 => r2.Client == clientEndPoint && r2.Id == clientId));*/
        }

        public void Dispose()
        {
            /*using (dictionaryLock.Aquire())
            {*/
            foreach (var i in monitorLookup.Values)
                i.Dispose();
            //monitors.Clear();
            monitorLookup.Clear();
            clientLookup.Clear();
            //}
            //dictionaryLock.Dispose();
        }

        internal void Drop(uint channelId, bool sendToServer = true)
        {
            List<MonitorInformationDetail> toDrop;
            /*using (dictionaryLock.Aquire())
            {*/
            toDrop = monitorLookup.Values.Where(row => row.ChannelInformation.GatewayId == channelId).ToList();
            /*}*/
            if (sendToServer)
                toDrop.ForEach(row =>
                {
                    row.Drop();
                    row.Dispose();
                });

            /*using (dictionaryLock.Aquire())
            {*/
            MonitorInformationDetail outVal;
            monitorLookup.Values.SelectMany(row => row.GetClients().Select(r2 => r2.Client.ToString() + ";" + r2.Id)).ToList().ForEach(row => clientLookup.TryRemove(row, out outVal));
            toDrop.ForEach(row => monitorLookup.TryRemove(row.GatewayId, out outVal));
            /*}*/
        }

        public int Count => monitorLookup.Count;
        /*{
            get
            {
                using (dictionaryLock.Aquire())
                {
                    return monitors.Count;
                }
            }
        }*/
    }
}
