using GWLoggerSoap;
using System.Collections.Generic;

namespace GatewayLogic.Services
{
    internal class LogMessageDetail
    {
        public MessageDetail TypeId { get; set; }
        public string Value { get; set; }
    }

    internal class LogMessage
    {
        public string Gateway { get; set; }
        public string RemoteIpPoint { get; set; }
        public int MessageType { get; set; }
        public List<LogMessageDetail> Details { get; set; } = new List<LogMessageDetail>();
    }
}