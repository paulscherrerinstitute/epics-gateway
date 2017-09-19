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

        public Gateway Gateway { get; }

        public ChannelInformation(Gateway gateway)
        {
            Gateway = Gateway;
            gateway.TenSecUpdate += (sender, evt) =>
              {
                  lock (dictionaryLock)
                  {
                      var toDrop = new List<string>();
                      foreach (var channel in dictionary.Values)
                      {
                          if (channel.ShouldDrop)
                              toDrop.Add(channel.ChannelName);
                          channel.Drop(gateway);
                      }
                      toDrop.ForEach(row => dictionary.Remove(row));
                  }
              };
        }

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
            public List<Client> connectedClients = new List<Client>();

            public DateTime LastUse { get; internal set; } = DateTime.Now;

            public ChannelInformationDetails(uint id, string channelName, SearchInformation.SearchInformationDetail search)
            {
                GatewayId = id;
                SearchInformation = search;
                ChannelName = channelName;
            }

            internal void RegisterClient(uint clientId, TcpClientConnection connection)
            {
                lock (clients)
                {
                    this.LastUse = DateTime.Now;
                    connectedClients.Add(new Client { Id = clientId, Connection = connection });
                }
            }

            internal IEnumerable<Client> GetClientConnections()
            {
                lock (clients)
                {
                    return connectedClients.ToList();
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
                    connectedClients.RemoveAll(row => row.Connection == connection);
                    this.LastUse = DateTime.Now;
                }
            }

            internal void Drop(Gateway gateway)
            {
                if (ServerId.HasValue)
                {
                    gateway.MonitorInformation.Drop(GatewayId);

                    // Send clear channel
                    var newPacket = DataPacket.Create(0);
                    newPacket.Command = 12;
                    newPacket.Parameter1 = GatewayId;
                    newPacket.Parameter2 = ServerId.Value;
                    TcpConnection.Send(newPacket);
                }
            }

            public bool ShouldDrop
            {
                get
                {
                    lock (clients)
                    {
                        return (connectedClients.Count == 0 && (DateTime.Now - this.LastUse).TotalMinutes > 30);
                    }
                }
            }

            public int NBConnected
            {
                get
                {
                    lock (clients)
                    {
                        return connectedClients.Count;
                    }
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

        internal void Remove(ChannelInformationDetails channel)
        {
            lock (dictionaryLock)
            {
                dictionary.Remove(channel.ChannelName);
            }
        }

        internal void ForceDropUnused()
        {
            lock (dictionaryLock)
            {
                var toDrop = new List<string>();
                foreach (var channel in dictionary.Values)
                {
                    if (channel.NBConnected == 0)
                        toDrop.Add(channel.ChannelName);
                    channel.Drop(Gateway);
                }
                toDrop.ForEach(row => dictionary.Remove(row));
            }
        }
    }
}
