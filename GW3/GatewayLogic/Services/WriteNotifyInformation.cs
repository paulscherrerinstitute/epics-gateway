using GatewayLogic.Connections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayLogic.Services
{
    class WriteNotifyInformation : IDisposable
    {
        readonly object dictionaryLock = new object();
        readonly List<WriteNotifyInformationDetail> writes = new List<WriteNotifyInformationDetail>();

        private uint nextId = 1;
        object counterLock = new object();

        public class WriteNotifyInformationDetail
        {
            public ChannelInformation.ChannelInformationDetails ChannelInformation { get; }
            public uint GatewayId { get; }
            public uint ClientId { get; }
            public TcpClientConnection Client { get; }

            public WriteNotifyInformationDetail(uint id, ChannelInformation.ChannelInformationDetails channelInformation, uint clientId, TcpClientConnection client)
            {
                GatewayId = id;
                ChannelInformation = channelInformation;
                ClientId = clientId;
                Client = client;
            }
        }

        public WriteNotifyInformationDetail Get(ChannelInformation.ChannelInformationDetails channelInformation, uint clientId, TcpClientConnection client)
        {
            lock (dictionaryLock)
            {
                var result = new WriteNotifyInformationDetail(nextId++, channelInformation, clientId, client);
                writes.Add(result);
                return result;
            }
        }

        public WriteNotifyInformationDetail GetByGatewayId(uint id)
        {
            lock (dictionaryLock)
            {
                var result = writes.FirstOrDefault(row => row.GatewayId == id);
                writes.Remove(result);
                return result;
            }
        }

        public void Dispose()
        {
            lock (dictionaryLock)
            {
                writes.Clear();
            }
        }

        internal void Remove(WriteNotifyInformationDetail write)
        {
            lock (dictionaryLock)
            {
                writes.Remove(write);
            }
        }

    }
}
