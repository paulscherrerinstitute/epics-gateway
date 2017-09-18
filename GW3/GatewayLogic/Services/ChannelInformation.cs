using GatewayLogic.Connections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayLogic.Services
{
    class ChannelInformation : IDisposable
    {
        readonly object dictionaryLock = new object();
        readonly Dictionary<string, ChannelInformationDetails> dictionary = new Dictionary<string, ChannelInformationDetails>();

        private uint nextId = 1;
        object counterLock = new object();

        public class ChannelInformationDetails
        {
            public object LockObject { get; } = new object();

            public SearchInformation.SearchInformationDetail SearchInformation { get; }
            public string ChannelName { get; }
            public TcpServerConnection TcpConnection { get; internal set; }
            public uint GatewayId { get; }
            public uint? ServerId { get; set; }
            public bool ConnectionIsBuilding { get; internal set; }

            public List<ClientId> clients = new List<ClientId>();
            public List<TcpClientConnection> connectedClients = new List<TcpClientConnection>();

            public ChannelInformationDetails(uint id, string channelName, SearchInformation.SearchInformationDetail search)
            {
                GatewayId = id;
                SearchInformation = search;
                ChannelName = channelName;
            }

            internal void RegisterClient(TcpClientConnection connection)
            {
                lock (clients)
                {
                    connectedClients.Add(connection);
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

            internal void DisconnectClient(TcpClientConnection connection)
            {
                lock (clients)
                {
                    connectedClients.Remove(connection);
                }
            }
        }

        public ChannelInformationDetails Get(uint id)
        {
            lock (dictionaryLock)
            {
                return dictionary.Values.FirstOrDefault(row => row.GatewayId == id);
            }
        }

        public ChannelInformationDetails Get(string channelName, SearchInformation.SearchInformationDetail search)
        {
            lock (dictionaryLock)
            {
                if (!dictionary.ContainsKey(channelName))
                {
                    var result = new ChannelInformationDetails(nextId++, channelName, search);
                    dictionary.Add(channelName, result);
                }
                return dictionary[channelName];
            }
        }

        internal bool HasChannelInformation(string channelName)
        {
            lock (dictionaryLock)
            {
                return dictionary.ContainsKey(channelName);
            }
        }

        internal void DisconnectClient(TcpClientConnection connection)
        {
            lock (dictionaryLock)
            {
                foreach (var i in dictionary.Values)
                    i.DisconnectClient(connection);
            }
        }

        public void Dispose()
        {
            lock (dictionaryLock)
            {
                dictionary.Clear();
            }
        }
    }
}
