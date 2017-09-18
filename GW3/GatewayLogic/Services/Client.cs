using GatewayLogic.Connections;

namespace GatewayLogic.Services
{
    internal class Client
    {
        public uint Id { get; set; }
        public TcpClientConnection Connection { get; set; }
    }
}