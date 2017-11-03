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
            Gateway.TenSecUpdate += Gateway_TenSecUpdate;
        }

        private void Gateway_TenSecUpdate(object sender, EventArgs e)
        {
            // Dispose old one
            List<TType> toDelete;
            lockDictionary.Wait();
            toDelete = dictionary.Values.Where(row => (DateTime.UtcNow - row.LastMessage).TotalSeconds > 90).ToList();
            lockDictionary.Release();

            toDelete.ForEach(row => row.Dispose());

            lockDictionary.Wait();
            var echoPacket = DataPacket.Create(0);
            echoPacket.Command = 23;

            foreach (var conn in dictionary.Values.Where(row => (DateTime.UtcNow - row.LastMessage).TotalSeconds > 30))
            {
                conn.HasSentEcho = true;
                conn.Send(echoPacket);
            }
            lockDictionary.Release();
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
