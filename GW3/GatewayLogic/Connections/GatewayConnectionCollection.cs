using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GatewayLogic.Connections
{
    class GatewayConnectionCollection<TType> : IEnumerable<TType> where TType : GatewayTcpConnection
    {
        public Gateway Gateway { get; }

        internal GatewayConnectionCollection(Gateway gateway)
        {
            Gateway = gateway;
            Gateway.OneSecUpdate += Gateway_OneSecUpdate;
        }

        private void Gateway_OneSecUpdate(object sender, EventArgs e)
        {
            // Dispose old one
            List<TType> toDelete;
            List<TType> toCheck;
            try
            {
                lockDictionary.Wait();
                toDelete = dictionary.Values.Where(row => (DateTime.UtcNow - row.LastMessage).TotalSeconds > 90).ToList();

                toCheck = dictionary.Values.Where(row => (DateTime.UtcNow - row.LastMessage).TotalSeconds > 5 && !toDelete.Contains(row)).ToList();
            }
            finally
            {
                lockDictionary.Release();
            }

            toDelete.ForEach(row => row.Dispose());

            var echoPacket = DataPacket.Create(0);
            echoPacket.Command = 23;

            foreach (var conn in toCheck)
            {
                conn.HasSentEcho = true;
                try
                {
                    conn.Send(echoPacket);
                }
                catch
                {
                }
            }
        }

        protected readonly SemaphoreSlim lockDictionary = new SemaphoreSlim(1);
        protected readonly Dictionary<IPEndPoint, TType> dictionary = new Dictionary<IPEndPoint, TType>();

        public void Dispose()
        {
            lockDictionary.Wait();
            dictionary.Clear();
            lockDictionary.Release();

            //lockDictionary.Dispose();
        }

        internal void Remove(TType tcpClientConnection)
        {
            if (tcpClientConnection.RemoteEndPoint == null)
                return;
            lockDictionary.Wait();
            dictionary.Remove(tcpClientConnection.RemoteEndPoint);
            lockDictionary.Release();
        }


        public IEnumerator<TType> GetEnumerator()
        {
            lockDictionary.Wait();
            var result = dictionary.Values.ToList();
            lockDictionary.Release();
            return result.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get
            {
                lockDictionary.Wait();
                var result = dictionary.Count;
                lockDictionary.Release();
                return result;
            }
        }
    }
}
