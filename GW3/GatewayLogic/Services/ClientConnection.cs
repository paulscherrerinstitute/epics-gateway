using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GatewayLogic.Services
{
    class ClientConnection
    {
        internal ClientConnection()
        {

        }

        readonly object lockDictionary = new object();
        readonly Dictionary<IPEndPoint, TcpClientConnection> dictionary = new Dictionary<IPEndPoint, TcpClientConnection>();

        public void Add(TcpClientConnection connection)
        {
            lock (lockDictionary)
            {
                dictionary.Add(connection.RemoteEndPoint, connection);
            }
        }

        public TcpClientConnection Get(IPEndPoint client)
        {
            lock (lockDictionary)
            {
                return dictionary[client];
            }
        }

        internal void Clear()
        {
            lock (lockDictionary)
            {
                dictionary.Clear();
            }
        }
    }
}
