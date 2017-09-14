using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayLogic.Services
{
    class ReadNotifyInformation
    {
        readonly static object dictionaryLock = new object();
        readonly static List<ReadNotifyInformation> reads = new List<ReadNotifyInformation>();

        public ChannelInformation.ChannelInformationDetails ChannelInformation { get; }
        public uint GatewayId { get; }
        public uint ClientId { get; private set; }
        public TcpClientConnection Client { get; private set; }

        static private uint nextId = 1;
        static object counterLock = new object();

        private ReadNotifyInformation(ChannelInformation.ChannelInformationDetails channelInformation, uint clientId, TcpClientConnection client)
        {
            lock (counterLock)
            {
                GatewayId = nextId++;
            }

            ChannelInformation = channelInformation;
            ClientId = clientId;
            Client = client;
        }

        public static ReadNotifyInformation Get(ChannelInformation.ChannelInformationDetails channelInformation, uint clientId, TcpClientConnection client)
        {
            lock (dictionaryLock)
            {
                var result = new ReadNotifyInformation(channelInformation, clientId, client);
                reads.Add(result);
                return result;
            }
        }

        public static ReadNotifyInformation GetByGatewayId(uint id)
        {
            lock (dictionaryLock)
            {
                var result = reads.FirstOrDefault(row => row.GatewayId == id);
                reads.Remove(result);
                return result;
            }
        }

        internal static void Clear()
        {
            lock (dictionaryLock)
            {
                reads.Clear();
            }
        }
    }
}
