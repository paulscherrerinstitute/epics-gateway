using GatewayLogic.Connections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GatewayLogic.Services
{
    internal class ReadNotifyInformation : IDisposable
    {
        private readonly List<ReadNotifyInformationDetail> reads = new List<ReadNotifyInformationDetail>();

        private int nextId = 1;

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
            var uid = System.Threading.Interlocked.Increment(ref nextId);
            var result = new ReadNotifyInformationDetail((uint)uid, channelInformation, clientId, client);
            lock (reads)
            {
                reads.Add(result);
            }
            return result;
        }

        public ReadNotifyInformationDetail GetByGatewayId(uint id)
        {
            ReadNotifyInformationDetail result;

            lock (reads)
            {
                result = reads.FirstOrDefault(row => row.GatewayId == id);
                reads.Remove(result);
                reads.RemoveAll(row => (DateTime.UtcNow - row.When).TotalSeconds > 3600);
            }
            return result;
        }

        public void Dispose()
        {
            lock (reads)
            {
                reads.Clear();
            }
        }

        internal void Remove(ReadNotifyInformationDetail read)
        {
            lock (reads)
            {
                reads.Remove(read);
            }
        }
    }
}
