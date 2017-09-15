using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GatewayLogic.Connections
{
    class ServerConnection : IDisposable
    {
        public delegate void TcpConnectionReady(TcpServerConnection connection);

        internal ServerConnection()
        {
        }

        SemaphoreSlim connectionLock = new SemaphoreSlim(1);
        readonly Dictionary<IPEndPoint, TcpServerConnection> connections = new Dictionary<IPEndPoint, TcpServerConnection>();

        public void CreateConnection(Gateway gateway, IPEndPoint endPoint, TcpConnectionReady connectionReady)
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

        public void Dispose()
        {
            connectionLock.Wait();
            connections.Clear();
            connectionLock.Release();
        }

        internal void Remove(TcpServerConnection tcpServerConnection)
        {
            connectionLock.Wait();
            connections.Remove(tcpServerConnection.RemoteEndPoint);
            connectionLock.Release();
        }
    }
}
