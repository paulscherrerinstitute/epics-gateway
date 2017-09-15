using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GatewayLogic.Connections
{
    class UdpResponseReceiver : UdpReceiver
    {
        public UdpResponseReceiver(Gateway gateway, IPEndPoint endPoint) : base(gateway, endPoint)
        {
        }
    }
}
