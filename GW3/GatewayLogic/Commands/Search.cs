using GatewayLogic.Connections;
using GatewayLogic.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GatewayLogic.Commands
{
    class Search : CommandHandler
    {
        public override void DoRequest(GatewayConnection connection, DataPacket packet)
        {
            // It's a response
            if (packet.PayloadSize == 8)
            {
                DoResponse(connection, packet);
                return;
            }

            string channelName = packet.GetDataAsString();
            connection.Gateway.Log.Write(Services.LogLevel.Detail, "Search for: " + channelName);

            var record = connection.Gateway.SearchInformation.Get(channelName);
            // Connection known, we answer we knows it
            if(record.Server != null)
            {
                var newPacket = (DataPacket)packet.Clone();

                if (connection == connection.Gateway.udpSideA)
                    newPacket.DataType = (UInt16)connection.Gateway.Configuration.SideBEndPoint.Port;
                else
                    newPacket.DataType = (UInt16)connection.Gateway.Configuration.SideAEndPoint.Port;
                newPacket.Parameter1 = 0xffffffff;
                newPacket.Parameter2 = packet.Parameter1;
                newPacket.Destination = packet.Sender;
                newPacket.SetUInt16(16, Gateway.CA_PROTO_VERSION);
                connection.Send(newPacket);

                return;
            }

            record.AddClient(new ClientId { Client = packet.Sender, Id = packet.Parameter1 });
            // ReSharper disable PossibleInvalidOperationException
            uint gwcid = record.GatewayId;
            // ReSharper restore PossibleInvalidOperationException
            record.Channel = channelName;

            // Diagnostic search
            /*var newPacket = (DataPacket)packet.Clone();
            newPacket.Parameter1 = gwcid;
            newPacket.Parameter2 = gwcid;
            newPacket.Destination = new IPEndPoint(chain.Gateway.Configuration.LocalSideB.Address, 7890);*/
            /*if (chain.Side == Workers.ChainSide.SIDE_B)
                newPacket.ReverseAnswer = true;*/
            //connection.Send(newPacket);

            // Send to all the destinations
            foreach (IPEndPoint dest in connection.Destinations)
            {
                var newPacket = (DataPacket)packet.Clone();
                newPacket.Parameter1 = gwcid;
                newPacket.Parameter2 = gwcid;
                newPacket.Destination = dest;
                connection.Send(newPacket);
            }
        }

        public override void DoResponse(GatewayConnection connection, DataPacket packet)
        {
            var search = connection.Gateway.SearchInformation.Get(packet.Parameter2);

            if (search == null)
            {
                connection.Gateway.Log.Write(Services.LogLevel.Error, "Search answer for nothing...");
                return;
            }

            connection.Gateway.Log.Write(Services.LogLevel.Detail, "Search answer for " + search.Channel + " from " + packet.Sender);
            search.Server = packet.Sender;

            foreach (var c in search.GetClients())
            {
                var newPacket = (DataPacket)packet.Clone();

                if (connection == connection.Gateway.udpSideA)
                    newPacket.DataType = (UInt16)connection.Gateway.Configuration.SideBEndPoint.Port;
                else
                    newPacket.DataType = (UInt16)connection.Gateway.Configuration.SideAEndPoint.Port;
                newPacket.Parameter1 = 0xffffffff;
                newPacket.Parameter2 = c.Id;
                newPacket.Destination = c.Client;
                newPacket.SetUInt16(16, Gateway.CA_PROTO_VERSION);
                connection.Send(newPacket);
            }
        }
    }
}
