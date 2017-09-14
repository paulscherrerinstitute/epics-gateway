using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GatewayLogic.Services
{
    class ServerConnection
    {
        public delegate void TcpConnectionReady(TcpServerConnection connection);

        private ServerConnection()
        {
        }

        static SemaphoreSlim connectionLock = new SemaphoreSlim(1);
        readonly static Dictionary<IPEndPoint, TcpServerConnection> connections = new Dictionary<IPEndPoint, TcpServerConnection>();

        public static void CreateConnection(Gateway gateway, IPEndPoint endPoint, TcpConnectionReady connectionReady)
        {
            connectionLock.Wait();
            if (connections.ContainsKey(endPoint))
            {
                var result = connections[endPoint];
                connectionLock.Release();
                result.WhenConnected(() =>
                {
                    connectionReady(result);
                });
                return;
            }
            var conn = new TcpServerConnection(gateway, endPoint);
            connections.Add(endPoint, conn);
            connectionLock.Release();

            conn.WhenConnected(() =>
            {
                connectionReady(conn);
            });            
        }

        internal static void Clear()
        {
            connectionLock.Wait();
            connections.Clear();
            connectionLock.Release();
        }
    }
}
