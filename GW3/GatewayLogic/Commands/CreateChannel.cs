using GatewayLogic.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayLogic.Commands
{
    class CreateChannel : CommandHandler
    {
        public override void DoRequest(GatewayConnection connection, DataPacket packet)
        {
            string channelName = packet.GetDataAsString();
            if (!ChannelInformation.HasChannelInformation(channelName) && !SearchInformation.HasChannelServerInformation(channelName))
            {
                Console.WriteLine("Channel is not known");
                return;
            }

            var searchInfo = SearchInformation.Get(channelName);
            var channelInfo = ChannelInformation.Get(channelName, searchInfo);            

            if (channelInfo.TcpConnection == null)
            {
                ServerConnection.CreateConnection(searchInfo.Server, (tcpConnection) =>
                {
                    channelInfo.TcpConnection = tcpConnection;
                    var newPacket=(DataPacket)packet.Clone();
                    //if(connection.Gateway.s)
                    //newPacket.Sender
                    newPacket.Destination = searchInfo.Server;      
                    channelInfo.TcpConnection.Send(newPacket);
                });
            }
        }

        public override void DoResponse(GatewayConnection connection, DataPacket packet)
        {
        }
    }
}
