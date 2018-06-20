using GatewayLogic.Connections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GatewayLogic.Services
{
    class ReadNotifyInformation : IDisposable
    {
        readonly SafeLock dictionaryLock = new SafeLock();
        readonly List<ReadNotifyInformationDetail> reads = new List<ReadNotifyInformationDetail>();

        private uint nextId = 1;

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
            using (dictionaryLock.Aquire())
            {
                var result = new ReadNotifyInformationDetail(nextId++, channelInformation, clientId, client);
                reads.Add(result);
                return result;
            }
        }

        public ReadNotifyInformationDetail GetByGatewayId(uint id)
        {
            using (dictionaryLock.Aquire())
            {
                var result = reads.FirstOrDefault(row => row.GatewayId == id);
                reads.Remove(result);

                reads.RemoveAll(row => (DateTime.UtcNow - row.When).TotalSeconds > 3600);

                return result;
            }
        }

        ~ReadNotifyInformation()
        {
            dictionaryLock.Dispose();
        }

        public void Dispose()
        {
            using (dictionaryLock.Aquire())
            {
                reads.Clear();
            }
        }

        internal void Remove(ReadNotifyInformationDetail read)
        {
            using (dictionaryLock.Aquire())
            {
                reads.Remove(read);
            }
        }
    }
}
