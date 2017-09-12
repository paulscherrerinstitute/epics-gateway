using GatewayLogic.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GatewayLogic.Commands
{
    class CreateChannel : CommandHandler
    {
        SemaphoreSlim locker = new SemaphoreSlim(1);

        public override void DoRequest(GatewayConnection connection, DataPacket packet)
        {
            string channelName = packet.GetDataAsString();
            if (!ChannelInformation.HasChannelInformation(channelName) && !SearchInformation.HasChannelServerInformation(channelName))
            {

                Console.WriteLine("Channel is not known");
                return;
            }
            Console.WriteLine("Create channel for " + channelName);

            locker.Wait();
            var searchInfo = SearchInformation.Get(channelName);
            var channelInfo = ChannelInformation.Get(channelName, searchInfo);

            lock (channelInfo.LockObject)
            {
                locker.Release();
                // We have all the info, we shall answer
                if (channelInfo.ServerId.HasValue)
                {
                    DataPacket resPacket = DataPacket.Create(0);
                    resPacket.Command = 22;
                    resPacket.DataType = 0;
                    resPacket.DataCount = 0;
                    resPacket.Parameter1 = packet.Parameter1;
                    resPacket.Parameter2 = (uint)(SecurityAccess.ALL);
                    resPacket.Destination = packet.Sender;
                    connection.Send(packet);

                    resPacket = (DataPacket)packet.Clone();
                    resPacket.Command = 18;
                    resPacket.Destination = packet.Sender;
                    resPacket.Parameter1 = packet.Parameter1;
                    resPacket.Parameter2 = channelInfo.GatewayId;
                    connection.Send(packet);
                }
                else
                {
                    channelInfo.AddClient(new ClientId { Client = packet.Sender, Id = packet.Parameter1 });
                    if (!channelInfo.ConnectionIsBuilding)
                    {
                        channelInfo.ConnectionIsBuilding = true;
                        if (channelInfo.TcpConnection == null)
                        {
                            ServerConnection.CreateConnection(connection.Gateway, searchInfo.Server, (tcpConnection) =>
                            {
                                channelInfo.TcpConnection = tcpConnection;
                                var newPacket = (DataPacket)packet.Clone();
                                newPacket.Parameter1 = channelInfo.GatewayId;
                                newPacket.Parameter2 = Gateway.CA_PROTO_VERSION;
                                //if(connection.Gateway.s)
                                //newPacket.Sender
                                newPacket.Destination = searchInfo.Server;
                                channelInfo.TcpConnection.Send(newPacket);
                            });
                        }
                    }
                }
            }
        }

        public override void DoResponse(GatewayConnection connection, DataPacket packet)
        {
            locker.Wait();
            var channelInfo = ChannelInformation.Get(packet.Parameter1);
            Console.WriteLine("Answer for create channel " + channelInfo.ChannelName);
            lock (channelInfo.LockObject)
            {
                locker.Release();
                foreach (var client in channelInfo.GetClients())
                {
                    var destConn = ClientConnection.Get(client.Client);

                    DataPacket resPacket = DataPacket.Create(0);
                    resPacket.Command = 22;
                    resPacket.DataType = 0;
                    resPacket.DataCount = 0;
                    resPacket.Parameter1 = client.Id;
                    resPacket.Parameter2 = (uint)(SecurityAccess.ALL);
                    resPacket.Destination = client.Client;
                    destConn.Send(packet);

                    resPacket = (DataPacket)packet.Clone();
                    resPacket.Command = 18;
                    resPacket.Destination = client.Client;
                    resPacket.Parameter1 = client.Id;
                    resPacket.Parameter2 = channelInfo.GatewayId;
                    destConn.Send(packet);
                }
            }
        }
    }
}
