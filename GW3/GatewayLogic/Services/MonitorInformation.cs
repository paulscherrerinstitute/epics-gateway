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

        public object LockObject { get; } = new object();

        public ChannelInformation ChannelInformation { get; }
        public TcpServerConnection TcpConnection { get; internal set; }
        public uint GatewayId { get; }
        public uint? ServerId { get; set; }
        public bool ConnectionIsBuilding { get; internal set; }
        public ushort DataType { get; private set; }
        public uint DataCount { get; private set; }

        static private uint nextId = 1;
        static object counterLock = new object();

        public List<ClientId> clients = new List<ClientId>();

        private MonitorInformation(ChannelInformation channelInformation,ushort dataType,uint dataCount)
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
                var result = clients.ToList();
                clients.Clear();
                return result;
            }
        }
        public static MonitorInformation Get(ChannelInformation channelInformation, ushort dataType, uint dataCount)
        {
            lock (dictionaryLock)
            {
                var monitor = monitors.FirstOrDefault(row => row.ChannelInformation == channelInformation && row.DataType == dataType && row.DataCount == dataCount);
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
    }
}
