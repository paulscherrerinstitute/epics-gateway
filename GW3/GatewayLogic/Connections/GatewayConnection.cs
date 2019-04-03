using GatewayLogic.Services;
using System.Collections.Generic;
using System.Net;

namespace GatewayLogic.Connections
{
    internal abstract class GatewayConnection
    {
        public IList<IPEndPoint> Destinations { get; set; }
        public Gateway Gateway { get; }
        public GatewayConnection(Gateway gateway)
        {
            this.Gateway = gateway;
        }

        abstract public void Send(DataPacket packet);

        protected void Dispose() => Dispose(LogMessageType.UnknownReason);

        public abstract void Dispose(LogMessageType commandReason, string message = null);
    }
}
