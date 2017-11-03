using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GatewayLogic.Connections
{
    class ClientConnection : GatewayConnectionCollection<GatewayTcpConnection>, IDisposable
    {
        internal ClientConnection(Gateway gateway) : base(gateway)
        {
        }

        public void Add(GatewayTcpConnection client)
        {
            lockDictionary.Wait();
            dictionary.Add(client.RemoteEndPoint, client);
            lockDictionary.Release();
        }

        public GatewayTcpConnection Get(IPEndPoint client)
        {
            lockDictionary.Wait();
            if (!dictionary.ContainsKey(client))
            {
                lockDictionary.Release();
                return null;
            }
            var result = dictionary[client];
            lockDictionary.Release();
            return result;
        }
    }
}
