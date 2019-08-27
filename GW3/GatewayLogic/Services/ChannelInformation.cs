using GatewayLogic.Connections;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace GatewayLogic.Services
{
    internal class ChannelInformation : IDisposable
    {
        private readonly ConcurrentDictionary<string, ChannelInformationDetails> dictionary = new ConcurrentDictionary<string, ChannelInformationDetails>();
        private readonly ConcurrentDictionary<uint, ChannelInformationDetails> uidDictionary = new ConcurrentDictionary<uint, ChannelInformationDetails>();

        private int nextId = 1;
        private object counterLock = new object();

        public Gateway Gateway { get; }

        public ChannelInformation(Gateway gateway)
        {
            Gateway = gateway;
            gateway.TenSecUpdate += (sender, evt) =>
              {
                  List<ChannelInformationDetails> toRebuild = dictionary.Select(row => row.Value).Where(row => row.ShouldRebuild).ToList();

                  // Let's drop, and let's hope it rebuilds
                  //toRebuild.ForEach(row => dictionary.Remove(row.ChannelName));
                  toRebuild.ForEach(row =>
                  {
                      //Console.WriteLine("Rebuild create channel for " + row.ChannelName);
                      row.Rebuild(Gateway);
                  });

                  List<ChannelInformationDetails> toDrop = dictionary.Select(row => row.Value).Where(row => row.ShouldDrop).ToList();
                  toDrop.ForEach(row =>
                      {
                          row.Drop(Gateway);
                          row.Dispose();
                          dictionary.Remove(row.ChannelName);
                          uidDictionary.Remove(row.GatewayId);
                      });
              };
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

            readonly public List<ClientId> clients = new List<ClientId>();
            readonly public List<Client> connectedClients = new List<Client>();

            public DateTime LastUse { get; internal set; } = DateTime.UtcNow;

            public ChannelInformationDetails(uint id, string channelName, SearchInformation.SearchInformationDetail search)
            {
                GatewayId = id;
                SearchInformation = search;
                ChannelName = channelName;
            }

            internal void RegisterClient(uint clientId, TcpClientConnection connection)
            {
                this.LastUse = DateTime.UtcNow;
                lock (connectedClients)
                    connectedClients.Add(new Client { Id = clientId, Connection = connection });
            }

            internal IEnumerable<Client> GetClientConnections()
            {
                lock (connectedClients)
                    return connectedClients.ToList();
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
                IEnumerable<ClientId> result;
                lock (clients)
                {
                    result = clients.ToList();
                    clients.Clear();
                }
                return result;
            }

            internal void DisconnectClient(TcpClientConnection connection)
            {
                if (connection == null)
                    return;
                try
                {
                    List<Client> toDrop;
                    lock (connectedClients)
                        toDrop = connectedClients.Where(row => row.Connection == connection).ToList();
                    foreach (var i in toDrop)
                        connection.Gateway?.MonitorInformation?.GetByClientId(i.Connection?.RemoteEndPoint, i.Id)?.RemoveClient(connection.Gateway, i.Connection?.RemoteEndPoint, i.Id);
                    lock (connectedClients)
                        toDrop.ForEach(row => connectedClients.Remove(row));
                    this.LastUse = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    try
                    {
                        connection.Gateway.MessageLogger.Write(connection.RemoteEndPoint.ToString(), Services.LogMessageType.Exception, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.Exception, Value = ex.ToString() + "\n" + ex.StackTrace } });
                        //connection.Gateway.Log.Write(LogLevel.Error, ex.ToString() + "\n" + ex.StackTrace);
                    }
                    catch
                    {
                    }
                }
            }

            internal void Rebuild(Gateway gateway)
            {
                try
                {
                    if (this.ChannelName == null)
                    {
                        shouldDropFlag = true;
                        return;
                    }

                    gateway.MessageLogger.Write("", LogMessageType.ChannelRebuild, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = this.ChannelName } });
                    this.StartBuilding = DateTime.UtcNow;
                    var size = this.ChannelName.Length + DataPacket.Padding(this.ChannelName.Length);
                    var newPacket = DataPacket.Create(size);
                    newPacket.Command = 18;
                    newPacket.PayloadSize = (uint)size;
                    newPacket.Parameter1 = this.GatewayId;
                    newPacket.Parameter2 = Gateway.CA_PROTO_VERSION;
                    newPacket.SetDataAsString(this.ChannelName);
                    TcpConnection?.Send(newPacket);
                }
                catch (Exception ex)
                {
                    gateway.MessageLogger.Write("", LogMessageType.Exception, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.Exception, Value = ex.ToString() + "\n" + ex.StackTrace } });
                    shouldDropFlag = true;
                }
            }

            internal void Drop(Gateway gateway)
            {
                try
                {
                    if (ServerId.HasValue && ConnectionIsBuilding == false)
                    {
                        gateway.MonitorInformation.Drop(GatewayId);

                        // Send clear channel
                        var newPacket = DataPacket.Create(0);
                        newPacket.Command = 12;
                        newPacket.Parameter1 = GatewayId;
                        newPacket.Parameter2 = ServerId.Value;
                        TcpConnection?.Send(newPacket);
                    }
                    else
                    {
                        // Drop the search information as well as it was certainly wrong
                        gateway.SearchInformation.Remove(this.ChannelName);

                        List<Client> clients;
                        lock (connectedClients)
                            clients = connectedClients.ToList();

                        // Send channel disconnected
                        var newPacket = DataPacket.Create(0);
                        newPacket.Command = 27;
                        foreach (var client in clients)
                        {
                            newPacket.Parameter1 = client.Id;
                            client.Connection.Send(newPacket);
                        }

                        TcpConnection?.RemoveChannel(this);
                        if (TcpConnection?.Channels.Count == 0)
                        {
                            // Drop server connection
                            try
                            {
                                TcpConnection?.Dispose(LogMessageType.DropChannel);
                                TcpConnection = null;
                            }
                            catch
                            {
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    gateway.MessageLogger.Write(null, Services.LogMessageType.Exception, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.Exception, Value = ex.ToString() + "\n" + ex.StackTrace } });
                }
            }

            public bool ShouldRebuild
            {
                get
                {
                    lock (connectedClients)
                        return (this.ConnectionIsBuilding == true && (DateTime.UtcNow - this.StartBuilding).TotalSeconds > 10 && this.TcpConnection != null && this.ChannelName != null);
                }
            }

            bool shouldDropFlag = false;
            public bool ShouldDrop
            {
                get
                {
                    lock (connectedClients)
                        return (shouldDropFlag || ((connectedClients.Count == 0 || this.TcpConnection == null) && (DateTime.UtcNow - this.LastUse).TotalMinutes > 30));
                }
            }

            public int NBConnected
            {
                get
                {
                    lock (connectedClients)
                        return connectedClients.Count;
                }
            }

            public uint DataCount { get; set; }
            public ushort DataType { get; set; }
            public DateTime StartBuilding { get; internal set; }

            public void Dispose()
            {
                List<Client> toDrop;
                lock (connectedClients)
                    toDrop = connectedClients.ToList();
                foreach (var i in toDrop)
                    i.Connection.Gateway.MonitorInformation.GetByClientId(i.Connection.RemoteEndPoint, i.Id)?.RemoveClient(i.Connection.Gateway, i.Connection.RemoteEndPoint, i.Id);
                lock (connectedClients)
                    connectedClients.Clear();
                this.LastUse = DateTime.UtcNow;
            }
        }

        public List<ChannelInformationDetails> GetChannelsInformation()
        {
            return dictionary.Values.ToList();
        }

        public ChannelInformationDetails Get(uint id)
        {
            ChannelInformationDetails res;
            if (uidDictionary.TryGetValue(id, out res))
                return res;
            return null;
        }

        public ChannelInformationDetails Get(string channelName, SearchInformation.SearchInformationDetail search)
        {
            if (!dictionary.ContainsKey(channelName))
            {
                var uid = System.Threading.Interlocked.Increment(ref nextId);
                var result = new ChannelInformationDetails((uint)uid, channelName, search);
                dictionary.Add(channelName, result);
                uidDictionary.Add(result.GatewayId, result);
            }
            return dictionary[channelName];
        }

        internal bool HasChannelInformation(string channelName)
        {
            return dictionary.ContainsKey(channelName);
        }

        internal void DisconnectClient(TcpClientConnection connection)
        {
            List<ChannelInformationDetails> list;
            list = dictionary.Values.ToList();
            foreach (var i in list)
                i?.DisconnectClient(connection);
        }

        public void Dispose()
        {
            foreach (var i in dictionary.Values)
                i.Dispose();
            dictionary.Clear();
            uidDictionary.Clear();
        }

        internal void Remove(Gateway gateway, ChannelInformationDetails channel)
        {
            gateway.SearchInformation.Remove(channel.ChannelName);

            channel.Dispose();
            ChannelInformationDetails res;
            if (dictionary.TryRemove(channel.ChannelName, out res))
                uidDictionary.Remove(res.GatewayId);
            gateway.MonitorInformation.Drop(channel.GatewayId);
        }

        internal void ForceDropUnused()
        {
            List<ChannelInformationDetails> toDrop;
            toDrop = dictionary.Values.Where(row => row.NBConnected == 0).ToList();

            toDrop.ForEach(channel =>
            {
                channel.Drop(Gateway);
                channel.Dispose();
            });

            toDrop.ForEach(row => { dictionary.Remove(row.ChannelName); uidDictionary.Remove(row.GatewayId); });
        }

        internal void ServerDrop(uint gatewayId)
        {
            IEnumerable<Client> clients;
            ChannelInformationDetails channel;
            if (!uidDictionary.TryGetValue(gatewayId, out channel))
                return;
            clients = channel.GetClientConnections();
            ChannelInformationDetails res;
            uidDictionary.TryRemove(gatewayId, out res);
            dictionary.TryRemove(channel.ChannelName, out res);

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
                data = dictionary.Values.ToDictionary(key => key.ChannelName, val => val.GetClientConnections().ToList());
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
                return dictionary.Count();
            }
        }
    }
}
