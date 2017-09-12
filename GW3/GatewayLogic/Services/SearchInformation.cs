using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GatewayLogic.Services
{
    class SearchInformation
    {
        public uint GatewayId { get; private set; }
        public List<ClientId> clients = new List<ClientId>();
        public string Channel { get; internal set; }
        public IPEndPoint Server { get; internal set; }

        static private uint nextId = 1;
        static object counterLock = new object();
        static object dictionaryLock = new object();
        static Dictionary<string, SearchInformation> dictionary = new Dictionary<string, SearchInformation>();

        private SearchInformation()
        {
            lock (counterLock)
            {
                GatewayId = nextId++;
            }
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

        internal static SearchInformation Get(string channelName)
        {
            lock (dictionaryLock)
            {
                if (!dictionary.ContainsKey(channelName))
                {
                    var result = new SearchInformation();
                    result.Channel = channelName;
                    dictionary.Add(channelName, result);
                }

                return dictionary[channelName];
            }
        }

        internal static bool HasChannelServerInformation(string channelName)
        {
            lock (dictionaryLock)
            {
                return dictionary.ContainsKey(channelName) && dictionary[channelName].Server != null;
            }
        }

        internal static SearchInformation Get(uint gatewayId)
        {
            lock (dictionaryLock)
            {
                var result = dictionary.Values.FirstOrDefault(row => row.GatewayId == gatewayId);
                return result;
            }
        }
    }
}
