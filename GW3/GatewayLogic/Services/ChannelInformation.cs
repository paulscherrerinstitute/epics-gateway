using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayLogic.Services
{
    class ChannelInformation
    {
        readonly static object dictionaryLock = new object();
        readonly static Dictionary<string, ChannelInformation> dictionary = new Dictionary<string, ChannelInformation>();

        public object LockObject { get; } = new object();

        public SearchInformation SearchInformation { get; set; }
        public string ChannelName { get; }
        public TcpServerConnection TcpConnection { get; internal set; }
        public uint GatewayId { get; }
        public uint? ServerId { get; set; }
        public bool ConnectionIsBuilding { get; internal set; }

        static private uint nextId = 1;
        static object counterLock = new object();

        public List<ClientId> clients = new List<ClientId>();

        private ChannelInformation(string channelName, SearchInformation search)
        {
            lock (counterLock)
            {
                GatewayId = nextId++;
            }

            SearchInformation = search;
            ChannelName = channelName;
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

        public static ChannelInformation Get(uint id)
        {
            lock (dictionaryLock)
            {
                return dictionary.Values.FirstOrDefault(row => row.GatewayId == id);
            }
        }

        public static ChannelInformation Get(string channelName, SearchInformation search)
        {
            lock (dictionaryLock)
            {
                if (!dictionary.ContainsKey(channelName))
                {
                    var result = new ChannelInformation(channelName, search);
                    dictionary.Add(channelName, result);
                }
                return dictionary[channelName];
            }
        }

        internal static bool HasChannelInformation(string channelName)
        {
            lock (dictionaryLock)
            {
                return dictionary.ContainsKey(channelName);
            }
        }

    }
}
