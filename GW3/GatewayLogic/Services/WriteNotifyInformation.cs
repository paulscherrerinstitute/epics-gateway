﻿using GatewayLogic.Connections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayLogic.Services
{
    class WriteNotifyInformation : IDisposable
    {
        readonly SafeLock dictionaryLock = new SafeLock();
        readonly List<WriteNotifyInformationDetail> writes = new List<WriteNotifyInformationDetail>();

        private uint nextId = 1;
        object counterLock = new object();

        public class WriteNotifyInformationDetail
        {
            public ChannelInformation.ChannelInformationDetails ChannelInformation { get; }
            public uint GatewayId { get; }
            public uint ClientId { get; }
            public TcpClientConnection Client { get; }
            public DateTime When { get; } = DateTime.UtcNow;

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
            using (dictionaryLock.Lock)
            {
                var result = new WriteNotifyInformationDetail(nextId++, channelInformation, clientId, client);
                writes.Add(result);
                return result;
            }
        }

        public WriteNotifyInformationDetail GetByGatewayId(uint id)
        {
            using (dictionaryLock.Lock)
            {
                writes.RemoveAll(row => (DateTime.UtcNow - row.When).TotalSeconds > 10);

                var result = writes.FirstOrDefault(row => row.GatewayId == id);
                writes.Remove(result);
                return result;
            }
        }

        public void Dispose()
        {
            using (dictionaryLock.Lock)
            {
                writes.Clear();
            }
            dictionaryLock.Dispose();
        }

        internal void Remove(WriteNotifyInformationDetail write)
        {
            using (dictionaryLock.Lock)
            {
                writes.Remove(write);
            }
        }
    }
}
