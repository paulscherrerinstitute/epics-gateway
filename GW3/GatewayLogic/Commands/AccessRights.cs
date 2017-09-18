using GatewayLogic.Connections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayLogic.Commands
{
    /// <summary>
    /// This class will simple "drop" the access right command as we overwrite the access via the configuration
    /// </summary>
    class AccessRights : CommandHandler
    {
        public override void DoRequest(GatewayConnection connection, DataPacket packet)
        {
        }

        public override void DoResponse(GatewayConnection connection, DataPacket packet)
        {
        }
    }
}
