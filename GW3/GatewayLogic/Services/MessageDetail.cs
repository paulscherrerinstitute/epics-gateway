using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayLogic.Services
{
    enum MessageDetail : int
    {
        Url,
        Port,
        Ip,
        SourceMemberName,
        SourceFilePath,
        SourceLineNumber,
        Exception,
        ChannelName,
        CommandId,
        CID,
        SID,
        GWID,
        DataCount,
        GatewayMonitorId,
        ClientIoId,
        Version
    }
}
