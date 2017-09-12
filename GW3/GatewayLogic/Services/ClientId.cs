using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GatewayLogic.Services
{
    class ClientId
    {
        public uint Id { get; internal set; }
        public IPEndPoint Client { get; internal set; }
    }
}
