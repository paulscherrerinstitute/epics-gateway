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
        private ClientConnection()
        {

        }

        readonly static object lockDictionary = new object();
        readonly static Dictionary<IPEndPoint, TcpClientConnection> dictionary = new Dictionary<IPEndPoint, TcpClientConnection>();

        public static void Add(TcpClientConnection connection)
        {
            lock (lockDictionary)
            {
                dictionary.Add(connection.RemoteEndPoint, connection);
            }
        }

        public static TcpClientConnection Get(IPEndPoint client)
        {
            lock (lockDictionary)
            {
                return dictionary[client];
            }
        }

        internal static void Clear()
        {
            lock (lockDictionary)
            {
                dictionary.Clear();
            }
        }
    }
}
