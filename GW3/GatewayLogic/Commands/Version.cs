using GatewayLogic.Connections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayLogic.Commands
{
    /// <summary>
    /// 0 (0x00) CA_PROTO_VERSION
    /// </summary>
    class Version : CommandHandler
    {
        public override void DoRequest(GatewayConnection connection, DataPacket packet)
        {
        }

        public override void DoResponse(GatewayConnection connection, DataPacket packet)
        {
        }
    }
}
