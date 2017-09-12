using GatewayLogic.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayLogic.Commands
{
    class EventAdd : CommandHandler
    {
        public override void DoRequest(GatewayConnection connection, DataPacket packet)
        {
            var channel = ChannelInformation.Get(packet.Parameter1);
            if (channel == null)
            {
                Console.WriteLine("Event add on wrong channel.");
                return;
            }
            Console.WriteLine("Event add on " + channel.ChannelName);

        }

        public override void DoResponse(GatewayConnection connection, DataPacket packet)
        {
        }
    }
}
