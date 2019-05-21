using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using GatewayLogic.Connections;

namespace GatewayLogic.Services
{
    class ClientId
    {
        public uint Id { get; set; }
        public IPEndPoint Client { get; set; }
        public bool WaitingReadyNotify { get; set; } = false;
        public DateTime When { get; internal set; }
        public GatewayConnection Connection { get; internal set; }
    }
}
