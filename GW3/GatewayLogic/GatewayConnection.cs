using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GatewayLogic
{
    abstract class GatewayConnection : IDisposable
    {
        public IList<IPEndPoint> Destinations { get; set; }
        public Gateway Gateway { get; }
        public GatewayConnection(Gateway gateway)
        {
            this.Gateway = gateway;
        }

        abstract public void Send(DataPacket packet);

        public abstract void Dispose();
    }
}
