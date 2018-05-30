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

            Configuration.SecurityAccess access;
            if (connection == connection.Gateway.udpSideA)
                access = connection.Gateway.Configuration.Security.EvaluateSideA(channelName, "", "", packet.Sender.Address.ToString());
            else
                access = connection.Gateway.Configuration.Security.EvaluateSideB(channelName, "", "", packet.Sender.Address.ToString());

            // Rules prevent searching
            if (access == Configuration.SecurityAccess.NONE)
                return;

            connection.Gateway.DiagnosticServer.NbSearches++;

            connection.Gateway.MessageLogger.Write(packet.Sender.ToString(), Services.LogMessageType.SearchRequest, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = channelName } });
            //connection.Gateway.Log.Write(Services.LogLevel.Detail, "Search for: " + channelName + ", from client (" + packet.Sender.ToString() + ")");
            connection.Gateway.Search(channelName, packet.Sender.ToString());

            var record = connection.Gateway.SearchInformation.Get(channelName);

            DataPacket newPacket;

            // Connection known, we answer we knows it
            if (record.Server != null)
            {
                connection.Gateway.MessageLogger.Write(packet.Sender.ToString(), Services.LogMessageType.SearchRequestAnswerFromCache, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = channelName } });
                //connection.Gateway.Log.Write(Services.LogLevel.Detail, "Search cached for: " + channelName + ", from client (" + packet.Sender.ToString() + ")");
                newPacket = (DataPacket)packet.Clone();

                if (connection == connection.Gateway.udpSideA)
                    newPacket.DataType = (UInt16)connection.Gateway.Configuration.SideAEndPoint.Port;
                else
                    newPacket.DataType = (UInt16)connection.Gateway.Configuration.SideBEndPoint.Port;
                newPacket.Parameter1 = 0xffffffff;
                newPacket.Parameter2 = packet.Parameter1;
                newPacket.Destination = packet.Sender;
                newPacket.SetUInt16(16, Gateway.CA_PROTO_VERSION);
                newPacket.ReverseAnswer = true;
                connection.Send(newPacket);

                return;
            }
            if ((DateTime.UtcNow - record.LastSearch).TotalMilliseconds < connection.Gateway.Configuration.SearchPreventionTimeout)
            {
                connection.Gateway.MessageLogger.Write(packet.Sender.ToString(), Services.LogMessageType.SearchRequestTooNew, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = channelName } });
                //connection.Gateway.Log.Write(Services.LogLevel.Detail, "Search is too new, we drop it, from client (" + packet.Sender.ToString() + ")");
                return;
            }

            connection.Gateway.MessageLogger.Write(packet.Sender.ToString(), Services.LogMessageType.SearchRequestTooNew, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = channelName } });
            //connection.Gateway.Log.Write(Services.LogLevel.Detail, "Search sent for: " + channelName + ", from client (" + packet.Sender.ToString() + ")");
            record.AddClient(new ClientId { Client = packet.Sender, Id = packet.Parameter1, When = DateTime.UtcNow });
            // ReSharper disable PossibleInvalidOperationException
            uint gwcid = record.GatewayId;
            // ReSharper restore PossibleInvalidOperationException
            record.Channel = channelName;

            // Diagnostic search
            newPacket = (DataPacket)packet.Clone();
            newPacket.Parameter1 = gwcid;
            newPacket.Parameter2 = gwcid;
            newPacket.Destination = new IPEndPoint(connection.Gateway.Configuration.SideBEndPoint.Address, connection.Gateway.Configuration.DiagnosticPort);
            if (connection == connection.Gateway.udpSideB)
            {
                newPacket.ReverseAnswer = true;
                connection.Send(newPacket);
            }
            else
                connection.Send(newPacket);

            record.LastSearch = DateTime.UtcNow;
            // Send to all the destinations
            foreach (IPEndPoint dest in connection.Destinations)
            {
                newPacket = (DataPacket)packet.Clone();
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
                //connection.Gateway.Log.Write(Services.LogLevel.Error, "Search answer for nothing...");
                return;
            }

            var version = packet.GetUInt16(0 + (int)packet.HeaderSize);
            connection.Gateway.MessageLogger.Write(packet.Sender.ToString(), Services.LogMessageType.SearchAnswer, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = search.Channel } });
            //connection.Gateway.Log.Write(Services.LogLevel.Detail, "Search answer for " + search.Channel + " from " + packet.Sender + " version " + version);
            //if(packet.Parameter2 == 0xffffffff)
            search.Server = new IPEndPoint(packet.Sender.Address, packet.DataType);
            search.Version = version;
            /*else
                search.Server = new IPEndPoint(packet.Parameter2, packet.DataType);*/

            foreach (var c in search.GetClients())
            {
                connection.Gateway.MessageLogger.Write(c.Client.ToString(), Services.LogMessageType.SearchAnswerSent, new LogMessageDetail[] { new LogMessageDetail { TypeId = MessageDetail.ChannelName, Value = search.Channel } });
                //connection.Gateway.Log.Write(Services.LogLevel.Detail, "Search answer " + search.Channel + " sent to " + c.Client.ToString());
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
