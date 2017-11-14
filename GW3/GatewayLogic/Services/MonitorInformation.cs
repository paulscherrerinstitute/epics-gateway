using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GatewayLogic.Services
{
    class MonitorInformation : IDisposable
    {
        readonly object dictionaryLock = new object();
        readonly List<MonitorInformationDetail> monitors = new List<MonitorInformationDetail>();

        private uint nextId = 1;
        object counterLock = new object();

        public class MonitorInformationDetail
        {
            public ChannelInformation.ChannelInformationDetails ChannelInformation { get; }


            public uint GatewayId { get; }
            public ushort DataType { get; }
            public uint DataCount { get; }
            public bool FirstTime { get; set; } = true;
            public bool HasReceivedFirstResult { get; set; } = false;
            public ushort MonitorMask { get; internal set; }

            public List<ClientId> clients = new List<ClientId>();

            public MonitorInformationDetail(uint id, ChannelInformation.ChannelInformationDetails channelInformation, ushort dataType, uint dataCount, UInt16 monitorMask)
            {
                GatewayId = id;
                ChannelInformation = channelInformation;
                DataCount = dataCount;
                DataType = dataType;
                MonitorMask = monitorMask;
            }

            internal void AddClient(ClientId clientId)
            {
                lock (clients)
                {
                    if (!clients.Any(row => row.Client == clientId.Client && row.Id == row.Id))
                        clients.Add(clientId);
                }
            }

            internal void RemoveClient(Gateway gateway, IPEndPoint endPoint, uint clientId)
            {
                lock (gateway.MonitorInformation.dictionaryLock)
                {
                    lock (clients)
                    {
                        clients.RemoveAll(row => row.Id == clientId && row.Client == endPoint);
                        // No more clients, we should cancel the monitor
                        if (clients.Count == 0)
                        {
                            this.Drop();
                            gateway.MonitorInformation.monitors.Remove(this);
                        }
                    }
                }
            }

            internal IEnumerable<ClientId> GetClients()
            {
                lock (clients)
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
        }
        public MonitorInformationDetail Get(ChannelInformation.ChannelInformationDetails channelInformation, ushort dataType, uint dataCount, UInt16 monitorMask)
        {
            lock (dictionaryLock)
            {
                MonitorInformationDetail monitor = null;
                if (dataCount != 0)
                    monitor = monitors.FirstOrDefault(row => row.ChannelInformation == channelInformation
                        && row.DataType == dataType
                        && row.DataCount == dataCount
                        && row.MonitorMask == monitorMask);
                if (monitor == null)
                {
                    monitor = new MonitorInformationDetail(nextId++, channelInformation, dataType, dataCount, monitorMask);
                    monitors.Add(monitor);
                }
                return monitor;
            }
        }

        public MonitorInformationDetail GetByGatewayId(uint id)
        {
            lock (dictionaryLock)
            {
                return monitors.FirstOrDefault(row => row.GatewayId == id);
            }
        }

        public void Dispose()
        {
            lock (dictionaryLock)
            {
                monitors.Clear();
            }
        }

        internal void Drop(uint channelId, bool sendToServer = true)
        {
            List<MonitorInformationDetail> toDrop;
            lock (dictionaryLock)
            {
                toDrop = monitors.Where(row => row.ChannelInformation.GatewayId == channelId).ToList();
            }

            if (sendToServer)
                toDrop.ForEach(row => row.Drop());

            lock (dictionaryLock)
            { 
                monitors.RemoveAll(row => row.ChannelInformation.GatewayId == channelId);
            }
        }

        public int Count
        {
            get
            {
                lock (dictionaryLock)
                {
                    return monitors.Count;
                }
            }
        }
    }
}
