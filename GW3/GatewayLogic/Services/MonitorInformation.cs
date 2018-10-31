using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace GatewayLogic.Services
{
    internal class MonitorInformation : IDisposable
    {
        private readonly SafeLock dictionaryLock = new SafeLock();
        private readonly List<MonitorInformationDetail> monitors = new List<MonitorInformationDetail>();
        private readonly Dictionary<uint, MonitorInformationDetail> monitorLookup = new Dictionary<uint, MonitorInformationDetail>();
        private readonly Dictionary<string, MonitorInformationDetail> clientLookup = new Dictionary<string, MonitorInformationDetail>();

        private uint nextId = 1;

        //object counterLock = new object();

        ~MonitorInformation()
        {
            dictionaryLock.Dispose();
        }

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

            internal SafeLock clientsLock = new SafeLock();
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
                using (clientsLock.Aquire())
                {
                    if (!clients.Any(row => row.Client == clientId.Client && row.Id == clientId.Id))
                        clients.Add(clientId);
                }
                using (MonitorInformation.dictionaryLock.Aquire())
                    MonitorInformation.clientLookup.Add(clientId.Client.ToString() + ";" + clientId.Id.ToString(), this);
            }

            internal void RemoveClient(Gateway gateway, IPEndPoint endPoint, uint clientId)
            {
                var needToDeleteThis = false;

                using (clientsLock.Aquire())
                {
                    clients.RemoveAll(row => row.Id == clientId && row.Client == endPoint);
                    // No more clients, we should cancel the monitor
                    if (clients.Count == 0)
                        needToDeleteThis = true;
                }

                using (MonitorInformation.dictionaryLock.Aquire())
                    MonitorInformation.clientLookup.Remove(endPoint.ToString() + ";" + clientId.ToString());

                if (needToDeleteThis)
                {
                    using (gateway.MonitorInformation.dictionaryLock.Aquire())
                    {
                        gateway.MonitorInformation.monitorLookup.Remove(this.GatewayId);
                        gateway.MonitorInformation.monitors.Remove(this);
                    }
                    this.Drop();
                }
            }

            internal IEnumerable<ClientId> GetClients()
            {
                using (clientsLock.Aquire())
                {
                    return clients.ToList();
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
                using (clientsLock.Aquire())
                    return clients.Any(condition);
            }
        }

        public MonitorInformationDetail Get(ChannelInformation.ChannelInformationDetails channelInformation, ushort dataType, uint dataCount, UInt16 monitorMask)
        {
            using (dictionaryLock.Aquire())
            {
                MonitorInformationDetail monitor = null;
                if (dataCount != 0)
                    monitor = monitors.FirstOrDefault(row => row.ChannelInformation == channelInformation
                        && row.DataType == dataType
                        && row.DataCount == dataCount
                        && row.MonitorMask == monitorMask);
                if (monitor == null)
                {
                    monitor = new MonitorInformationDetail(nextId++, channelInformation, dataType, dataCount, monitorMask, this);
                    monitors.Add(monitor);
                    monitorLookup.Add(monitor.GatewayId, monitor);
                }
                return monitor;
            }
        }

        public MonitorInformationDetail GetByGatewayId(uint id)
        {
            using (dictionaryLock.Aquire())
            {
                //return monitors.FirstOrDefault(row => row.GatewayId == id);
                if (monitorLookup.ContainsKey(id))
                    return monitorLookup[id];
                return null;
            }
        }

        public MonitorInformationDetail GetByClientId(IPEndPoint clientEndPoint, uint clientId)
        {
            var key = clientEndPoint.ToString() + ";" + clientId;
            using (dictionaryLock.Aquire())
            {
                if (clientLookup.ContainsKey(key))
                    return clientLookup[key];
                return null;
            }


            /*List<MonitorInformationDetail> copy;

        using (dictionaryLock.Aquire())
            copy = monitors.ToList();

        return copy.FirstOrDefault(row => row.Any(r2 => r2.Client == clientEndPoint && r2.Id == clientId));*/
        }

        public void Dispose()
        {
            using (dictionaryLock.Aquire())
            {
                foreach (var i in monitors)
                    i.Dispose();
                monitors.Clear();
                monitorLookup.Clear();
                clientLookup.Clear();
            }
            //dictionaryLock.Dispose();
        }

        internal void Drop(uint channelId, bool sendToServer = true)
        {
            List<MonitorInformationDetail> toDrop;
            using (dictionaryLock.Aquire())
            {
                toDrop = monitors.Where(row => row.ChannelInformation.GatewayId == channelId).ToList();
            }
            if (sendToServer)
                toDrop.ForEach(row =>
                {
                    row.Drop();
                    row.Dispose();
                });

            using (dictionaryLock.Aquire())
            {
                toDrop.ForEach(row => monitorLookup.Remove(row.GatewayId));                
                monitors.SelectMany(row => row.GetClients().Select(r2 => r2.Client.ToString() + ";" + r2.Id)).ToList().ForEach(row => clientLookup.Remove(row));
                monitors.RemoveAll(row => row.ChannelInformation.GatewayId == channelId);
            }
        }

        public int Count
        {
            get
            {
                using (dictionaryLock.Aquire())
                {
                    return monitors.Count;
                }
            }
        }
    }
}
