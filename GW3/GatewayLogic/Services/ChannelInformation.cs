using GatewayLogic.Connections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GatewayLogic.Services
{
    internal class ChannelInformation : IDisposable
    {
        private readonly SafeLock dictionaryLock = new SafeLock();
        private readonly Dictionary<string, ChannelInformationDetails> dictionary = new Dictionary<string, ChannelInformationDetails>();
        private readonly Dictionary<uint, ChannelInformationDetails> uidDictionary = new Dictionary<uint, ChannelInformationDetails>();

        private uint nextId = 1;
        private object counterLock = new object();

        public Gateway Gateway { get; }

        public ChannelInformation(Gateway gateway)
        {
            Gateway = gateway;
            gateway.TenSecUpdate += (sender, evt) =>
              {
                  List<ChannelInformationDetails> toDrop;
                  using (dictionaryLock.Aquire())
                  {
                      toDrop = dictionary.Select(row => row.Value).Where(row => (row.ConnectionIsBuilding && (DateTime.UtcNow - row.StartBuilding).TotalSeconds > 5) || row.ShouldDrop).ToList();
                  }
                  toDrop.ForEach(row =>
                      {
                          row.Drop(Gateway);
                          row.Dispose();
                          dictionary.Remove(row.ChannelName);
                          uidDictionary.Remove(row.GatewayId);
                      });
              };
        }

        ~ChannelInformation()
        {
            dictionaryLock.Dispose();
        }

        public class ChannelInformationDetails : IDisposable
        {
            //public SafeLock LockObject { get; } = new SafeLock();

            public SearchInformation.SearchInformationDetail SearchInformation { get; }
            public string ChannelName { get; }
            public TcpServerConnection TcpConnection { get; internal set; }
            public uint GatewayId { get; }
            public uint? ServerId { get; set; }
            public bool ConnectionIsBuilding { get; internal set; }

            private SafeLock clientsLock = new SafeLock();
            public List<ClientId> clients = new List<ClientId>();
            public List<Client> connectedClients = new List<Client>();

            public DateTime LastUse { get; internal set; } = DateTime.UtcNow;

            public ChannelInformationDetails(uint id, string channelName, SearchInformation.SearchInformationDetail search)
            {
                GatewayId = id;
                SearchInformation = search;
                ChannelName = channelName;
            }

            ~ChannelInformationDetails()
            {
                clientsLock.Dispose();
            }

            internal void RegisterClient(uint clientId, TcpClientConnection connection)
            {
                using (clientsLock.Aquire())
                {
                    this.LastUse = DateTime.UtcNow;
                    connectedClients.Add(new Client { Id = clientId, Connection = connection });
                }
            }

            internal IEnumerable<Client> GetClientConnections()
            {
                using (clientsLock.Aquire())
                {
                    return connectedClients.ToList();
                }
            }

            internal void AddClient(ClientId clientId)
            {
                using (clientsLock.Aquire())
                {
                    if (!clients.Any(row => row.Client == clientId.Client && row.Id == row.Id))
                        clients.Add(clientId);
                }
            }

            internal IEnumerable<ClientId> GetClients()
            {
                using (clientsLock.Aquire())
                {
                    var result = clients.ToList();
                    clients.Clear();
                    return result;
                }
            }

            internal void DisconnectClient(TcpClientConnection connection)
            {
                if (connection == null)
                    return;
                try
                {
                    List<Client> toDrop;
                    using (clientsLock.Aquire())
                    {
                        toDrop = connectedClients.Where(row => row.Connection == connection).ToList();
                    }
                    foreach (var i in toDrop)
                        connection.Gateway?.MonitorInformation?.GetByClientId(i.Connection?.RemoteEndPoint, i.Id)?.RemoveClient(connection.Gateway, i.Connection?.RemoteEndPoint, i.Id);
                    using (clientsLock.Aquire())
                    {
                        connectedClients.RemoveAll(row => row.Connection == connection);
                        this.LastUse = DateTime.UtcNow;
                    }
                }
                catch (Exception ex)
                {
                    try
                    {
                        connection.Gateway.Log.Write(LogLevel.Error, ex.ToString() + "\n" + ex.StackTrace);
                    }
                    catch
                    {
                    }
                }
            }

            internal void Drop(Gateway gateway)
            {
                if (ServerId.HasValue && ConnectionIsBuilding == false)
                {
                    gateway.MonitorInformation.Drop(GatewayId);

                    // Send clear channel
                    var newPacket = DataPacket.Create(0);
                    newPacket.Command = 12;
                    newPacket.Parameter1 = GatewayId;
                    newPacket.Parameter2 = ServerId.Value;
                    TcpConnection.Send(newPacket);
                }
                else
                {
                    // Drop the search information as well as it was certainly wrong
                    gateway.SearchInformation.Remove(this.ChannelName);

                    List<Client> clients;
                    using (clientsLock.Aquire())
                    {
                        clients = connectedClients.ToList();
                    }

                    // Send channel disconnected
                    var newPacket = DataPacket.Create(0);
                    newPacket.Command = 27;
                    foreach (var client in clients)
                    {
                        newPacket.Parameter1 = client.Id;
                        client.Connection.Send(newPacket);
                    }

                    /*// Drop client connection
                    try
                    {
                        toDrop.ForEach(row => row.Connection.Dispose());
                    }
                    catch
                    {
                    }*/
                    // Drop server connection
                    try
                    {
                        TcpConnection?.Dispose();
                        TcpConnection = null;
                    }
                    catch
                    {
                    }
                }
            }

            public bool ShouldDrop
            {
                get
                {
                    using (clientsLock.Aquire())
                    {
                        //return (connectedClients.Count == 0 && (DateTime.UtcNow - this.LastUse).TotalMinutes > 30);
                        return (connectedClients.Count == 0 && (DateTime.UtcNow - this.LastUse).TotalMinutes > 30) || (this.ConnectionIsBuilding == true && (DateTime.UtcNow - this.StartBuilding).TotalSeconds > 2);
                    }
                }
            }

            public int NBConnected
            {
                get
                {
                    using (clientsLock.Aquire())
                    {
                        return connectedClients.Count;
                    }
                }
            }

            public uint DataCount { get; set; }
            public ushort DataType { get; set; }
            public DateTime StartBuilding { get; internal set; }

            public void Dispose()
            {
                List<Client> toDrop;
                using (clientsLock.Aquire())
                {
                    toDrop = connectedClients.ToList();
                }
                foreach (var i in toDrop)
                    i.Connection.Gateway.MonitorInformation.GetByClientId(i.Connection.RemoteEndPoint, i.Id)?.RemoveClient(i.Connection.Gateway, i.Connection.RemoteEndPoint, i.Id);
                using (clientsLock.Aquire())
                {
                    connectedClients.Clear();
                    this.LastUse = DateTime.UtcNow;
                }

                //clientsLock.Dispose();
            }
        }

        public ChannelInformationDetails Get(uint id)
        {
            using (dictionaryLock.Aquire())
            {
                if (uidDictionary.ContainsKey(id))
                    return uidDictionary[id];
                return null;
                //return dictionary.Values.FirstOrDefault(row => row.GatewayId == id);
            }
        }

        public ChannelInformationDetails Get(string channelName, SearchInformation.SearchInformationDetail search)
        {
            using (dictionaryLock.Aquire())
            {
                if (!dictionary.ContainsKey(channelName))
                {
                    var result = new ChannelInformationDetails(nextId++, channelName, search);
                    dictionary.Add(channelName, result);
                    uidDictionary.Add(result.GatewayId, result);
                }
                return dictionary[channelName];
            }
        }

        internal bool HasChannelInformation(string channelName)
        {
            using (dictionaryLock.Aquire())
            {
                return dictionary.ContainsKey(channelName);
            }
        }

        internal void DisconnectClient(TcpClientConnection connection)
        {
            List<ChannelInformationDetails> list;
            using (dictionaryLock.Aquire())
                list = dictionary.Values.ToList();
            foreach (var i in list)
                i.DisconnectClient(connection);
        }

        public void Dispose()
        {
            using (dictionaryLock.Aquire())
            {
                foreach (var i in dictionary.Values)
                    i.Dispose();
                dictionary.Clear();
                uidDictionary.Clear();
            }
            //dictionaryLock.Dispose();
        }

        internal void Remove(Gateway gateway, ChannelInformationDetails channel)
        {
            //gateway.MessageLogger.Write(null, LogMessageType.RemoveChannelInfo, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = channel.ChannelName } });
            gateway.SearchInformation.Remove(channel.ChannelName);

            channel.Dispose();
            using (dictionaryLock.Aquire())
            {
                if (dictionary.ContainsKey(channel.ChannelName))
                    uidDictionary.Remove(dictionary[channel.ChannelName].GatewayId);
                dictionary.Remove(channel.ChannelName);
            }
            gateway.MonitorInformation.Drop(channel.GatewayId);
        }

        internal void ForceDropUnused()
        {
            List<ChannelInformationDetails> toDrop;
            using (dictionaryLock.Aquire())
            {
                toDrop = dictionary.Values.Where(row => row.NBConnected == 0).ToList();
            }

            toDrop.ForEach(channel =>
            {
                channel.Drop(Gateway);
                channel.Dispose();
            });

            using (dictionaryLock.Aquire())
            {
                toDrop.ForEach(row => { dictionary.Remove(row.ChannelName); uidDictionary.Remove(row.GatewayId); });
            }
        }

        internal void ServerDrop(uint gatewayId)
        {
            IEnumerable<Client> clients;
            using (dictionaryLock.Aquire())
            {
                //var channel = dictionary.Values.FirstOrDefault(row => row.GatewayId == gatewayId);
                if (!uidDictionary.ContainsKey(gatewayId))
                    return;
                var channel = uidDictionary[gatewayId];
                clients = channel.GetClientConnections();
                uidDictionary.Remove(gatewayId);
                dictionary.Remove(channel.ChannelName);
            }

            var newPacket = DataPacket.Create(0);
            newPacket.Command = 27;
            foreach (var client in clients)
            {
                newPacket.Parameter1 = client.Id;
                client.Connection.Send(newPacket);
            }
            Gateway.MonitorInformation.Drop(gatewayId, false);
        }

        public Dictionary<string, List<string>> KnownClients
        {
            get
            {
                Dictionary<string, List<Client>> data;
                using (dictionaryLock.Aquire())
                {
                    data = dictionary.Values.ToDictionary(key => key.ChannelName, val => val.GetClientConnections().ToList());
                }
                var result = new Dictionary<string, List<string>>();
                foreach (var i in data)
                {
                    foreach (var j in i.Value.Select(row => row.Connection.Name))
                    {
                        if (!result.ContainsKey(j))
                            result.Add(j, new List<string>());
                        result[j].Add(i.Key);
                    }
                }
                return result;
            }
        }

        public int Count
        {
            get
            {
                using (dictionaryLock.Aquire())
                {
                    return dictionary.Count;
                }
            }
        }
    }
}
