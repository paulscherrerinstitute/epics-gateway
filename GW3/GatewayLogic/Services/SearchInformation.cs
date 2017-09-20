using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GatewayLogic.Services
{
    class SearchInformation : IDisposable
    {
        private uint nextId = 1;
        object counterLock = new object();
        object dictionaryLock = new object();
        Dictionary<string, SearchInformationDetail> dictionary = new Dictionary<string, SearchInformationDetail>();

        public class SearchInformationDetail
        {
            public uint GatewayId { get; }
            public List<ClientId> clients = new List<ClientId>();
            public string Channel { get; internal set; }
            public IPEndPoint Server { get; internal set; }


            public SearchInformationDetail(uint id)
            {
                GatewayId = id;
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
        }

        internal SearchInformationDetail Get(string channelName)
        {
            lock (dictionaryLock)
            {
                if (!dictionary.ContainsKey(channelName))
                {
                    var result = new SearchInformationDetail(nextId++);
                    result.Channel = channelName;
                    dictionary.Add(channelName, result);
                }

                return dictionary[channelName];
            }
        }

        internal bool HasChannelServerInformation(string channelName)
        {
            lock (dictionaryLock)
            {
                return dictionary.ContainsKey(channelName) && dictionary[channelName].Server != null;
            }
        }

        internal SearchInformationDetail Get(uint gatewayId)
        {
            lock (dictionaryLock)
            {
                var result = dictionary.Values.FirstOrDefault(row => row.GatewayId == gatewayId);
                return result;
            }
        }

        public void Dispose()
        {
            lock (dictionaryLock)
            {
                dictionary.Clear();
            }
        }

        public void Remove(string channelName)
        {
            lock (dictionaryLock)
            {
                if (dictionary.ContainsKey(channelName))
                    dictionary.Remove(channelName);
            }
        }
    }
}
