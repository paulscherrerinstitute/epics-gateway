using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayLogic.Services
{
    class MonitorInformation
    {
        readonly static object dictionaryLock = new object();
        readonly static List<MonitorInformation> monitors = new List<MonitorInformation>();

        public ChannelInformation.ChannelInformationDetails ChannelInformation { get; }
        public uint GatewayId { get; }
        public ushort DataType { get; private set; }
        public uint DataCount { get; private set; }

        static private uint nextId = 1;
        static object counterLock = new object();

        public List<ClientId> clients = new List<ClientId>();

        private MonitorInformation(ChannelInformation.ChannelInformationDetails channelInformation, ushort dataType, uint dataCount)
        {
            lock (counterLock)
            {
                GatewayId = nextId++;
            }

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
        public static MonitorInformation Get(ChannelInformation.ChannelInformationDetails channelInformation, ushort dataType, uint dataCount)
        {
            lock (dictionaryLock)
            {
                MonitorInformation monitor = null;
                if (dataCount != 0)
                    monitor = monitors.FirstOrDefault(row => row.ChannelInformation == channelInformation
                        && row.DataType == dataType
                        && row.DataCount == dataCount);
                if (monitor == null)
                {
                    monitor = new MonitorInformation(channelInformation, dataType, dataCount);
                    monitors.Add(monitor);
                }
                return monitor;
            }
        }

        public static MonitorInformation GetByGatewayId(uint id)
        {
            lock (dictionaryLock)
            {
                return monitors.FirstOrDefault(row => row.GatewayId == id);
            }
        }

        internal static void Clear()
        {
            lock (dictionaryLock)
            {
                monitors.Clear();
            }
        }
    }
}
