using GatewayLogic.Connections;
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

        public Configuration.Configuration Configuration { get; private set; } = new GatewayLogic.Configuration.Configuration();

        internal UdpReceiver udpSideA;
        internal UdpReceiver udpSideB;
        internal TcpClientListener tcpSideA;
        internal TcpClientListener tcpSideB;

        internal ChannelInformation ChannelInformation { get; private set; } = new ChannelInformation();
        internal MonitorInformation MonitorInformation { get; private set; } = new MonitorInformation();
        internal ReadNotifyInformation ReadNotifyInformation { get; private set; } = new ReadNotifyInformation();
        internal SearchInformation SearchInformation { get; private set; } = new SearchInformation();
        internal ClientConnection ClientConnection { get; private set; } = new ClientConnection();
        internal ServerConnection ServerConnection { get; private set; } = new ServerConnection();
        internal Log Log { get; private set; } = new Log();

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

            this.ChannelInformation.Dispose();
            this.MonitorInformation.Dispose();
            this.ReadNotifyInformation.Dispose();
            this.SearchInformation.Dispose();

            this.ClientConnection.Dispose();
            this.ServerConnection.Dispose();
        }
    }
}
