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
        [MessageDisplay("Event add response on {channelName} nb clients {clientCounts}.", LogLevel.Detail)]
        EventAddResponse,
        [MessageDisplay("Event waiting first response on {channelName}.", LogLevel.Detail)]
        EventAddResponseSkipForRead,
        [MessageDisplay("Event response for client which disappeared on {channelName}.", LogLevel.Detail)]
        EventAddResponseClientDisappeared,
        [MessageDisplay("Sending event response on {channelName} client {cid}.", LogLevel.Detail)]
        EventAddResponseSending,
        [MessageDisplay("Request cancel channel monitor with Monitor Id {gatewayMonitorId}.", LogLevel.Detail)]
        EventCancelRequest,
        [MessageDisplay("Cancel channel monitor on {channelName} with Monitor Id {gatewayMonitorId}", LogLevel.Detail)]
        EventCancel,
        [MessageDisplay("Read notify on wrong channel. {gwid}", LogLevel.Error)]
        ReadNotifyRequestWrongChannel,
        [MessageDisplay("Read notify on {channelName} SID {sid}.", LogLevel.Detail)]
        ReadNotifyRequest,
        [MessageDisplay("Read back for monitor => monitor client not found (id: {cid}).", LogLevel.Error)]
        ReadNotifyResponseMonitorClientNotFound,
        [MessageDisplay("Read notify response for event add on {channelName}.", LogLevel.Detail)]
        ReadNotifyResponseMonitor,
        [MessageDisplay("Read notify response on {channelName}.", LogLevel.Detail)]
        ReadNotifyResponse,
        [MessageDisplay("Search for: {channelName}, from client {endpoint}.", LogLevel.Detail)]
        SearchRequest,
        [MessageDisplay("Search cached for: {channelName}, from client {endpoint}.", LogLevel.Detail)]
        SearchRequestAnswerFromCache,
        [MessageDisplay("Search is too new, we drop it for: {channelName}, from client {endpoint}.", LogLevel.Detail)]        
        SearchRequestTooNew,
        [MessageDisplay("Search answer for: {channelName}, from server {endpoint}.", LogLevel.Detail)]
        SearchAnswer,
        [MessageDisplay("Search answer for: {channelName}, sent to {endpoint}.", LogLevel.Detail)]
        SearchAnswerSent,
        [MessageDisplay("Version request from {endpoint} => {version}.", LogLevel.Detail)]
        VersionRequest,
        [MessageDisplay("Version answer from {endpoint} => {version}.", LogLevel.Detail)]
        VersionAnswer,
        [MessageDisplay("Write on wrong channel.",LogLevel.Error)]
        WriteWrongChannel,
        [MessageDisplay("Write on {channelName}, from {endpoint}.", LogLevel.Detail)]
        Write,
        [MessageDisplay("Write Notify on wrong channel.", LogLevel.Error)]
        WriteNotifyRequestWrongChannel,
        [MessageDisplay("Write Notify request on {channelName}, from {endpoint}.", LogLevel.Detail)]
        WriteNotifyRequest,
        [MessageDisplay("Write Notify answer on {channelName}, from {endpoint}.", LogLevel.Detail)]
        WriteNotifyAnswer,
        [MessageDisplay("TCP Listener on {endpoint}.", LogLevel.Connection)]
        StartClientTcpListener,
        [MessageDisplay("Cannot connect to {endpoint}.", LogLevel.Error)]
        StartTcpServerConnectionFailed,
        [MessageDisplay("Server {endpoint} disconnect.", LogLevel.Connection)]
        DiposeTcpServerConnection,
        [MessageDisplay("Disposing channel {channelName}.", LogLevel.Detail)]
        ChannelDispose,
        [MessageDisplay("Server {endpoint} created.", LogLevel.Connection)]
        StartTcpServerConnection,
        [MessageDisplay("Deadlock detected.", LogLevel.Critical)]
        DeadLock
    }
}
