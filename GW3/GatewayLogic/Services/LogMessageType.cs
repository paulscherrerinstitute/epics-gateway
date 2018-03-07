using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayLogic.Services
{
    enum LogMessageType : int
    {
        [MessageDisplay("Loading configuration from {url}", LogLevel.Detail)]
        LoadingConfiguration,
        [MessageDisplay("Loading configuration from gateway.xml", LogLevel.Detail)]
        LoadingPreviousXmlConfiguration,
        [MessageDisplay("Starting debug server on {port} ip {ip}", LogLevel.Detail)]
        StartingDebugServer,
        [MessageDisplay("EndPoint cannot be null", LogLevel.Critical)]
        EndPointCannotBeNull,
        [MessageDisplay("Start TCP client connection on {endpoint}", LogLevel.Connection)]
        StartTcpClientConnection,
        [MessageDisplay("Exception: {exception}", LogLevel.Critical)]
        Exception,
        [MessageDisplay("Client {endpoint} disconnect", LogLevel.Connection)]
        ClientDisconnect,
        [MessageDisplay("Channel disconnect on {channelName}", LogLevel.Detail)]
        ChannelDisconnect,
        [MessageDisplay("Clear channel {channelName}", LogLevel.Detail)]
        ClearChannel,
        [MessageDisplay("Command not supported {CommandId}", LogLevel.Error)]
        CommandNotSupported,
        [MessageDisplay("Channel is not known: {channelName}", LogLevel.Error)]
        ChannelUnknown,
        [MessageDisplay("Create channel for {channelName}  from {endpoint} CID {cid}", LogLevel.Detail)]
        CreateChannel,
        [MessageDisplay("Create channel info is known ({channelName} => {sid}).", LogLevel.Detail)]
        CreateChannelInfoKnown,
        [MessageDisplay("Create channel for {channelName} info must be found.", LogLevel.Detail)]
        CreateChannelInfoRequired,
        [MessageDisplay("Connection for {channelName} must be made.", LogLevel.Detail)]
        CreateChannelConnectionRequired,
        [MessageDisplay("Connection for {channelName} created.", LogLevel.Detail)]
        CreateChannelConnectionMade,
        [MessageDisplay("Answer for unknown channel", LogLevel.Detail)]        
        CreateChannelAnswerForUnknown,
        [MessageDisplay("Answer for create channel {channelName}.", LogLevel.Detail)]
        CreateChannelAnswer,
        [MessageDisplay("Sending answer to {endpoint} GWID {gwid}", LogLevel.Detail)]
        CreateChannelSendingAnswer,
        [MessageDisplay("Echo answer received from {endpoint}", LogLevel.Detail)]        
        EchoAnswerReceived,
        [MessageDisplay("Echo request received from {endpoint}", LogLevel.Detail)]
        EchoRequestReceived
    }
}
