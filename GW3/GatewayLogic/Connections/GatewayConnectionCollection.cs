﻿using System;
using System.Collections;
using System.Collections.Concurrent;
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

        protected readonly ConcurrentDictionary<IPEndPoint, TType> dictionary = new ConcurrentDictionary<IPEndPoint, TType>();

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
            var now = DateTime.UtcNow;

            toDelete = dictionary.Values.Where(row => (now - row.LastMessage).TotalSeconds > 90).ToList();
            toCheck = dictionary.Values.Where(row => (now - row.LastMessage).TotalSeconds > 35 && (now - row.LastEcho).TotalSeconds > 30 && !toDelete.Contains(row)).ToList();

            //toDelete.ForEach(row => row.Dispose(Services.LogMessageType.EchoNeverAnswered));

            var echoPacket = DataPacket.Create(0);
            echoPacket.Command = 23;

            foreach (var conn in toCheck)
            {
                conn.HasSentEcho = true;
                conn.LastEcho = DateTime.UtcNow;
                try
                {
                    conn.Send(echoPacket);
                }
                catch
                {
                }
            }
        }

        internal void Add(IPEndPoint endPoint, TType value)
        {
            dictionary.TryAdd(endPoint, value);
        }

        public void Dispose(Services.LogMessageType reason)
        {
            var toClean = dictionary.Values.ToList();
            dictionary.Clear();

            foreach (var i in toClean)
                i.Dispose(reason);
        }

        internal void Remove(TType tcpClientConnection)
        {
            if (tcpClientConnection.RemoteEndPoint == null)
                return;
            TType outVal;
            dictionary.TryRemove(tcpClientConnection.RemoteEndPoint, out outVal);
        }


        public IEnumerator<TType> GetEnumerator() => dictionary.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public int Count => dictionary.Count;

        /*public Gateway Gateway { get; }

        internal GatewayConnectionCollection(Gateway gateway)
        {
            Gateway = gateway;
            Gateway.OneSecUpdate += Gateway_OneSecUpdate;
        }

        ~GatewayConnectionCollection()
        {
            lockDictionary.Dispose();
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

                toCheck = dictionary.Values.Where(row => (DateTime.UtcNow - row.LastMessage).TotalSeconds > 35 && !toDelete.Contains(row)).ToList();
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
                conn.LastEcho = DateTime.UtcNow;
                try
                {
                    conn.Send(echoPacket);
                }
                catch
                {
                }
            }
        }

        protected readonly SafeLock lockDictionary = new SafeLock();
        protected readonly Dictionary<IPEndPoint, TType> dictionary = new Dictionary<IPEndPoint, TType>();

        public void Dispose()
        {
            lockDictionary.Wait();
            var toClean = dictionary.Values.ToList();
            dictionary.Clear();
            lockDictionary.Release();

            foreach (var i in toClean)
                i.Dispose();

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
        }*/
    }
}
