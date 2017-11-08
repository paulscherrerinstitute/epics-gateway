using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GatewayLogic.Connections
{
    class ServerConnection : GatewayConnectionCollection<TcpServerConnection>, IDisposable
    {
        public delegate void TcpConnectionReady(TcpServerConnection connection);

        internal ServerConnection(Gateway gateway) : base(gateway)
        {
        }

        public void CreateConnection(Gateway gateway, IPEndPoint endPoint, TcpConnectionReady connectionReady)
        {
            if (endPoint == null)
            {
                gateway.Log.Write(Services.LogLevel.Error, "Endpoint cannot be null");
                return;
            }
            lockDictionary.Wait();
            if (dictionary.ContainsKey(endPoint))
            {
                var result = dictionary[endPoint];
                lockDictionary.Release();
                result.WhenConnected(() =>
                {
                    connectionReady(result);
                });
                return;
            }
            var conn = new TcpServerConnection(gateway, endPoint);
            dictionary.Add(endPoint, conn);
            lockDictionary.Release();

            conn.WhenConnected(() =>
            {
                connectionReady(conn);
            });
        }
    }
}
