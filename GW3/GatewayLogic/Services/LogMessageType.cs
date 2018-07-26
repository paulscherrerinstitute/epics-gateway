using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatewayLogic.Services
{
    enum LogMessageType : int
    {
        [MessageDisplay("Loading configuration from {Url}", LogLevel.Detail)]
        LoadingConfiguration,
        [MessageDisplay("Loading configuration from gateway.xml", LogLevel.Detail)]
        LoadingPreviousXmlConfiguration,
        [MessageDisplay("Starting debug server on {Port} ip {Ip}", LogLevel.Detail)]
        StartingDebugServer,
        [MessageDisplay("EndPoint cannot be null", LogLevel.Critical)]
        EndPointCannotBeNull,
        [MessageDisplay("Start TCP client connection on {Endpoint}", LogLevel.Connection)]
        StartTcpClientConnection,
        [MessageDisplay("Exception: {Exception}", LogLevel.Critical)]
        Exception,
        [MessageDisplay("Client {Endpoint} disconnect", LogLevel.Connection)]
        ClientDisconnect,
        [MessageDisplay("Channel disconnect on {ChannelName}", LogLevel.Detail)]
        ChannelDisconnect,
        [MessageDisplay("Clear channel {ChannelName}", LogLevel.Detail)]
        ClearChannel,
        [MessageDisplay("Command not supported {CommandId}", LogLevel.Error)]
        CommandNotSupported,
        [MessageDisplay("Channel is not known: {ChannelName}", LogLevel.Error)]
        ChannelUnknown,
        [MessageDisplay("Create channel for {ChannelName}  from {Endpoint} CID {CID}", LogLevel.Detail)]
        CreateChannel,
        [MessageDisplay("Create channel info is known ({channelName} => {SID}).", LogLevel.Detail)]
        CreateChannelInfoKnown,
        [MessageDisplay("Create channel for {ChannelName} info must be found.", LogLevel.Detail)]
        CreateChannelInfoRequired,
        [MessageDisplay("Connection for {ChannelName} must be made.", LogLevel.Detail)]
        CreateChannelConnectionRequired,
        [MessageDisplay("Connection for {ChannelName} created.", LogLevel.Detail)]
        CreateChannelConnectionMade,
        [MessageDisplay("Answer for unknown channel", LogLevel.Detail)]
        CreateChannelAnswerForUnknown,
        [MessageDisplay("Answer for create channel {ChannelName}.", LogLevel.Detail)]
        CreateChannelAnswer,
        [MessageDisplay("Sending answer to {Endpoint} GWID {GWID}", LogLevel.Detail)]
        CreateChannelSendingAnswer,
        [MessageDisplay("Echo answer received from {Endpoint} / {Origin}", LogLevel.Detail)]
        EchoAnswerReceived,
        [MessageDisplay("Echo request received from {Endpoint} / {Origin}", LogLevel.Detail)]
        EchoRequestReceived,
        [MessageDisplay("Event add on wrong channel.", LogLevel.Error)]
        EventAddWrongChannel,
        [MessageDisplay("CA Version too old, must set the datacount for {ChannelName} to {DataCount}.", LogLevel.Detail)]
        EventAddDynOldIoc,
        [MessageDisplay("Event add on {ChannelName} client id {CID}.", LogLevel.Detail)]
        EventAdd,
        [MessageDisplay("Event add first event on {ChannelName} client id {CID} => Gateway monitor id {GatewayMonitorId}.", LogLevel.Detail)]
        EventAddFirstEvent,
        [MessageDisplay("First event result already sent. Sent ReadNotify on {ChannelName} client id {CID} => Gateway monitor id {GatewayMonitorId}.", LogLevel.Detail)]
        EventAddNotFirst,
        [MessageDisplay("Add client to the waiting list on {ChannelName} client id {CID} => Gateway monitor id {GatewayMonitorId}.", LogLevel.Detail)]
        EventAddMonitorList,
        [MessageDisplay("Event add response on unknown => Gateway monitor id {GatewayMonitorId}.", LogLevel.Error)]
        EventResponseOnUnknown,
        [MessageDisplay("Event add response on {ChannelName} nb clients {ClientCounts}.", LogLevel.Detail)]
        EventAddResponse,
        [MessageDisplay("Event waiting first response on {ChannelName}.", LogLevel.Detail)]
        EventAddResponseSkipForRead,
        [MessageDisplay("Event response for client which disappeared on {ChannelName}.", LogLevel.Detail)]
        EventAddResponseClientDisappeared,
        [MessageDisplay("Sending event response on {ChannelName} client {CID}.", LogLevel.Detail)]
        EventAddResponseSending,
        [MessageDisplay("Request cancel channel monitor with Monitor Id {GatewayMonitorId}.", LogLevel.Detail)]
        EventCancelRequest,
        [MessageDisplay("Cancel channel monitor on {ChannelName} with Monitor Id {GatewayMonitorId}", LogLevel.Detail)]
        EventCancel,
        [MessageDisplay("Read notify on wrong channel. {GWID}", LogLevel.Error)]
        ReadNotifyRequestWrongChannel,
        [MessageDisplay("Read notify on {ChannelName} SID {SID}.", LogLevel.Detail)]
        ReadNotifyRequest,
        [MessageDisplay("Read back for monitor => monitor client not found (id: {CID}).", LogLevel.Error)]
        ReadNotifyResponseMonitorClientNotFound,
        [MessageDisplay("Read notify response for event add on {ChannelName}.", LogLevel.Detail)]
        ReadNotifyResponseMonitor,
        [MessageDisplay("Read notify response on {ChannelName}.", LogLevel.Detail)]
        ReadNotifyResponse,
        [MessageDisplay("Search for: {ChannelName}, from client {Endpoint}.", LogLevel.Detail)]
        SearchRequest,
        [MessageDisplay("Search cached for: {ChannelName}, from client {Endpoint}.", LogLevel.Detail)]
        SearchRequestAnswerFromCache,
        [MessageDisplay("Search is too new, we drop it for: {ChannelName}, from client {Endpoint}.", LogLevel.Detail)]        
        SearchRequestTooNew,
        [MessageDisplay("Search answer for: {ChannelName}, from server {Endpoint}.", LogLevel.Detail)]
        SearchAnswer,
        [MessageDisplay("Search answer for: {ChannelName}, sent to {Endpoint}.", LogLevel.Detail)]
        SearchAnswerSent,
        [MessageDisplay("Version request from {Endpoint} => {Version}.", LogLevel.Detail)]
        VersionRequest,
        [MessageDisplay("Version answer from {Endpoint} => {Version}.", LogLevel.Detail)]
        VersionAnswer,
        [MessageDisplay("Write on wrong channel.",LogLevel.Error)]
        WriteWrongChannel,
        [MessageDisplay("Write on {ChannelName}, from {Endpoint}.", LogLevel.Detail)]
        Write,
        [MessageDisplay("Write Notify on wrong channel.", LogLevel.Error)]
        WriteNotifyRequestWrongChannel,
        [MessageDisplay("Write Notify request on {ChannelName}, from {Endpoint}.", LogLevel.Detail)]
        WriteNotifyRequest,
        [MessageDisplay("Write Notify answer on {ChannelName}, from {Endpoint}.", LogLevel.Detail)]
        WriteNotifyAnswer,
        [MessageDisplay("TCP Listener on {Endpoint}.", LogLevel.Connection)]
        StartClientTcpListener,
        [MessageDisplay("Cannot connect to {Endpoint}.", LogLevel.Error)]
        StartTcpServerConnectionFailed,
        [MessageDisplay("Server {Endpoint} disconnect.", LogLevel.Connection)]
        DiposeTcpServerConnection,
        [MessageDisplay("Disposing channel {ChannelName}.", LogLevel.Detail)]
        ChannelDispose,
        [MessageDisplay("Server {Endpoint} created.", LogLevel.Connection)]
        StartTcpServerConnection,
        [MessageDisplay("Deadlock detected.", LogLevel.Critical)]
        DeadLock,
        [MessageDisplay("Remove ChannelInformation {ChannelName}.", LogLevel.Detail)]
        RemoveChannelInfo,
        [MessageDisplay("Remove SearchInformation {ChannelName}.", LogLevel.Detail)]
        RemoveSearchInfo,
        [MessageDisplay("Created SearchInformation {ChannelName}.", LogLevel.Detail)]
        CreatedSearchInfo,
        [MessageDisplay("Recover SearchInformation {ChannelName}.", LogLevel.Detail)]
        RecoverSearchInfo,
        [MessageDisplay("Search answer for nothing.", LogLevel.Detail)]
        SearchAnswerForNothing,
        [MessageDisplay("Blocked WriteNotify to {ChannelName}.", LogLevel.Detail)]
        WriteNotifyRequestNoAccess,
        [MessageDisplay("Blocked Write to {ChannelName}.", LogLevel.Detail)]
        WriteRequestNoAccess,
        [MessageDisplay("WriteNotify wrong answer {ClientIoId}.", LogLevel.Error)]
        WriteNotifyAnswerWrong
    }
}
