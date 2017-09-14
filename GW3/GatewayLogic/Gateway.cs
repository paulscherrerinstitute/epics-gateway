using GatewayLogic.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayLogic
{
    public class Gateway : IDisposable
    {
        public const int BUFFER_SIZE = 8192 * 30;
        public const UInt16 CA_PROTO_VERSION = 11;

        internal ChannelInformation ChannelInformation { get; private set; } = new ChannelInformation();

        internal UdpReceiver udpSideA;
        internal UdpReceiver udpSideB;
        internal TcpClientListener tcpSideA;
        internal TcpClientListener tcpSideB;

        public Configuration.Configuration Configuration { get; set; } = new GatewayLogic.Configuration.Configuration();

        public Gateway()
        {

        }

        public void Start()
        {
            tcpSideA = new TcpClientListener(this, this.Configuration.SideAEndPoint);
            tcpSideB = new TcpClientListener(this, this.Configuration.SideBEndPoint);

            udpSideA = new UdpReceiver(this, this.Configuration.SideAEndPoint);
            udpSideB = new UdpResponseReceiver(this, this.Configuration.SideBEndPoint);
        }

        public void Dispose()
        {
            tcpSideA.Dispose();
            tcpSideB.Dispose();

            udpSideA.Dispose();
            udpSideB.Dispose();

            this.ChannelInformation.Clear();
            Services.ClientConnection.Clear();
            Services.MonitorInformation.Clear();
            Services.ReadNotifyInformation.Clear();
            Services.SearchInformation.Clear();
            Services.ServerConnection.Clear();
        }
    }
}
