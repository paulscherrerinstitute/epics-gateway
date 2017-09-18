using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GatewayLogic.Connections
{
    abstract class GatewayTcpConnection : GatewayConnection
    {
        public GatewayTcpConnection(Gateway gateway) : base(gateway)
        {
        }

        public IPEndPoint RemoteEndPoint { get; set; }
        public bool HasSentEcho { get; set; }

        public DateTime LastMessage { get; set; }
    }
}
