using System;
using System.Collections.Generic;
using System.Linq;
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


            public List<ClientId> clients = new List<ClientId>();

            public MonitorInformationDetail(uint id, ChannelInformation.ChannelInformationDetails channelInformation, ushort dataType, uint dataCount)
            {
                GatewayId = id;
                ChannelInformation = channelInformation;
                DataCount = dataCount;
                DataType = dataType;
            }

            internal void AddClient(ClientId clientId)
            {
                lock (clients)
                {
                    if (!clients.Any(row => row.Client == clientId.Client && row.Id == row.Id))
                        clients.Add(clientId);
                }
            }

            internal IEnumerable<ClientId> GetClients()
            {
                lock (clients)
                {
                    return clients.ToList();
                }
            }
        }
        public MonitorInformationDetail Get(ChannelInformation.ChannelInformationDetails channelInformation, ushort dataType, uint dataCount)
        {
            lock (dictionaryLock)
            {
                MonitorInformationDetail monitor = null;
                if (dataCount != 0)
                    monitor = monitors.FirstOrDefault(row => row.ChannelInformation == channelInformation
                        && row.DataType == dataType
                        && row.DataCount == dataCount);
                if (monitor == null)
                {
                    monitor = new MonitorInformationDetail(nextId++, channelInformation, dataType, dataCount);
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
    }
}
