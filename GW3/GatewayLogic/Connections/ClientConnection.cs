using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GatewayLogic.Connections
{
    class ClientConnection : GatewayConnectionCollection<GatewayTcpConnection>
    {
        internal ClientConnection(Gateway gateway) : base(gateway)
        {
        }

        public void Add(GatewayTcpConnection client)
        {
            if (client.RemoteEndPoint == null)
                return;
            //lockDictionary.Wait();
            if(dictionary.ContainsKey(client.RemoteEndPoint))
            {
                //lockDictionary.Release();
                return;
            }
            base.Add(client.RemoteEndPoint, client);
            //lockDictionary.Release();
        }

        public GatewayTcpConnection Get(IPEndPoint client)
        {
            //lockDictionary.Wait();
            if (!dictionary.ContainsKey(client))
            {
                //lockDictionary.Release();
                return null;
            }
            var result = dictionary[client];
            //lockDictionary.Release();
            return result;
        }
    }
}
