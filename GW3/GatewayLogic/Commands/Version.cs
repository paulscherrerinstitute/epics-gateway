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
            if (!(connection is GatewayTcpConnection))
                return;
            connection.Gateway.Log.Write(Services.LogLevel.Detail, "Version received from " + ((GatewayTcpConnection)connection).Name + " => " + packet.DataCount);
        }

        public override void DoResponse(GatewayConnection connection, DataPacket packet)
        {
            connection.Gateway.Log.Write(Services.LogLevel.Detail, "Version answer from " + ((GatewayTcpConnection)connection).Name + " => " + packet.DataCount);
            if (connection is TcpServerConnection)
            {
                ((TcpServerConnection)connection).Version = packet.DataCount;
            }
        }
    }
}
