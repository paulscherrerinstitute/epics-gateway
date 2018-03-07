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
        EchoRequestReceived,
        [MessageDisplay("Event add on wrong channel.", LogLevel.Error)]
        EventAddWrongChannel,
        [MessageDisplay("CA Version too old, must set the datacount for {channelName} to {dataCount}.", LogLevel.Detail)]
        EventAddDynOldIoc,
        [MessageDisplay("Event add on {channelName} client id {cid}.", LogLevel.Detail)]
        EventAdd,
        [MessageDisplay("Event add first event on {channelName} client id {cid} => Gateway monitor id {gatewayMonitorId}.", LogLevel.Detail)]
        EventAddFirstEvent,
        [MessageDisplay("First event result already sent. Sent ReadNotify on {channelName} client id {cid} => Gateway monitor id {gatewayMonitorId}.", LogLevel.Detail)]
        EventAddNotFirst,
        [MessageDisplay("Add client to the waiting list on {channelName} client id {cid} => Gateway monitor id {gatewayMonitorId}.", LogLevel.Detail)]
        EventAddMonitorList,
        [MessageDisplay("Event add response on unknown => Gateway monitor id {gatewayMonitorId}.", LogLevel.Error)]
        EventResponseOnUnknown,
        [MessageDisplay("Event add response on {channelName} clients {clientCount}.", LogLevel.Detail)]
        EventAddResponse,
        [MessageDisplay("Event waiting first response on {channelName}.", LogLevel.Detail)]
        EventAddResponseSkipForRead,
        [MessageDisplay("Event response for client which disappeared on {channelName}.", LogLevel.Detail)]
        EventAddResponseClientDisappeared,
        [MessageDisplay("Sending event response on {channelName} client {cid}.", LogLevel.Detail)]
        EventAddResponseSending
    }
}
