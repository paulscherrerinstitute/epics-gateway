using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayLogic
{
    public class Gateway
    {
        public const int BUFFER_SIZE = 8192 * 30;
        public const UInt16 CA_PROTO_VERSION = 11;

        internal UdpReceiver udpSideA;
        internal UdpReceiver udpSideB;

        public Configuration.Configuration Configuration { get; set; } = new GatewayLogic.Configuration.Configuration();

        public Gateway()
        {

        }

        public void Start()
        {
            udpSideA = new UdpReceiver(this, this.Configuration.SideAEndPoint);
            udpSideB = new UdpResponseReceiver(this, this.Configuration.SideBEndPoint);
        }
    }
}
