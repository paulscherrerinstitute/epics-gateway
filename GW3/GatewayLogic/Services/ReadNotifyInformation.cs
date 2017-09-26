﻿using GatewayLogic.Connections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayLogic.Services
{
    class ReadNotifyInformation : IDisposable
    {
        readonly object dictionaryLock = new object();
        readonly List<ReadNotifyInformationDetail> reads = new List<ReadNotifyInformationDetail>();

        private uint nextId = 1;
        object counterLock = new object();

        public class ReadNotifyInformationDetail
        {
            public ChannelInformation.ChannelInformationDetails ChannelInformation { get; }
            public uint GatewayId { get; }
            public uint ClientId { get; }
            public TcpClientConnection Client { get; }
            public bool IsEventAdd { get; set; } = false;
            public uint EventClientId { get; internal set; }
            public MonitorInformation.MonitorInformationDetail Monitor { get; set; }
            public DateTime When { get; } = DateTime.UtcNow;

            public ReadNotifyInformationDetail(uint id, ChannelInformation.ChannelInformationDetails channelInformation, uint clientId, TcpClientConnection client)
            {
                GatewayId = id;
                ChannelInformation = channelInformation;
                ClientId = clientId;
                Client = client;
            }
        }

        public ReadNotifyInformationDetail Get(ChannelInformation.ChannelInformationDetails channelInformation, uint clientId, TcpClientConnection client)
        {
            lock (dictionaryLock)
            {
                var result = new ReadNotifyInformationDetail(nextId++, channelInformation, clientId, client);
                reads.Add(result);
                return result;
            }
        }

        public ReadNotifyInformationDetail GetByGatewayId(uint id)
        {
            lock (dictionaryLock)
            {
                reads.RemoveAll(row => (DateTime.UtcNow - row.When).TotalSeconds > 10);

                var result = reads.FirstOrDefault(row => row.GatewayId == id);
                reads.Remove(result);
                return result;
            }
        }

        public void Dispose()
        {
            lock (dictionaryLock)
            {
                reads.Clear();
            }
        }

        internal void Remove(ReadNotifyInformationDetail read)
        {
            lock (dictionaryLock)
            {
                reads.Remove(read);
            }
        }
    }
}
