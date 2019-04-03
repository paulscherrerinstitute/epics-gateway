using System;
using System.Linq;

namespace GWLogger.Backend.DataContext.Query.Statement
{
    internal class VariableNode : QueryNode, INamedNode
    {
        public string Name { get; }

        private int detailId = -1;
        private int secondId = -1;

        internal VariableNode(Tokens.Token token)
        {
            Name = token.Value.ToLower();
        }

        internal VariableNode(string name)
        {
            Name = name.ToLower();
        }

        public override bool CheckCondition(Context context, LogEntry entry)
        {
            throw new NotImplementedException();
        }

        public override string Value(Context context, LogEntry entry)
        {
            switch (Name)
            {
                case "class":
                    if (detailId == -1)
                        detailId = context.MessageDetailTypes.First(row => row.Value == "SourceMemberName").Id;
                    if (secondId == -1)
                        secondId = context.MessageDetailTypes.First(row => row.Value == "SourceFilePath").Id;
                    return (entry.LogEntryDetails.FirstOrDefault(row => row.DetailTypeId == secondId)?.Value ?? "") + "@" + (entry.LogEntryDetails.FirstOrDefault(row => row.DetailTypeId == detailId)?.Value ?? "");
                case "sourcefilepath":
                    if (detailId == -1)
                        detailId = context.MessageDetailTypes.First(row => row.Value == "SourceFilePath").Id;
                    return entry.LogEntryDetails.FirstOrDefault(row => row.DetailTypeId == detailId)?.Value ?? "";
                case "sourcemembername":
                    if (detailId == -1)
                        detailId = context.MessageDetailTypes.First(row => row.Value == "SourceMemberName").Id;
                    return entry.LogEntryDetails.FirstOrDefault(row => row.DetailTypeId == detailId)?.Value ?? "";
                case "line":
                case "sourcelinenumber":
                    if (detailId == -1)
                        detailId = context.MessageDetailTypes.First(row => row.Value == "SourceLineNumber").Id;
                    return entry.LogEntryDetails.FirstOrDefault(row => row.DetailTypeId == detailId)?.Value ?? "";
                case "channel":
                case "channelname":
                    if (detailId == -1)
                        detailId = context.MessageDetailTypes.First(row => row.Value == "ChannelName").Id;
                    return entry.LogEntryDetails.FirstOrDefault(row => row.DetailTypeId == detailId)?.Value ?? "";
                case "sid":
                    if (detailId == -1)
                        detailId = context.MessageDetailTypes.First(row => row.Value == "SID").Id;
                    return entry.LogEntryDetails.FirstOrDefault(row => row.DetailTypeId == detailId)?.Value ?? "";
                case "cid":
                    if (detailId == -1)
                        detailId = context.MessageDetailTypes.First(row => row.Value == "CID").Id;
                    return entry.LogEntryDetails.FirstOrDefault(row => row.DetailTypeId == detailId)?.Value ?? "";
                case "gwid":
                    if (detailId == -1)
                        detailId = context.MessageDetailTypes.First(row => row.Value == "GWID").Id;
                    return entry.LogEntryDetails.FirstOrDefault(row => row.DetailTypeId == detailId)?.Value ?? "";
                case "remote":
                    return entry.RemoteIpPoint;
                case "cmd":
                case "commandid":
                    if (detailId == -1)
                        detailId = context.MessageDetailTypes.First(row => row.Value == "CommandId").Id;
                    return entry.LogEntryDetails.FirstOrDefault(row => row.DetailTypeId == detailId)?.Value ?? "";
                case "ip":
                    if (detailId == -1)
                        detailId = context.MessageDetailTypes.First(row => row.Value == "Ip").Id;
                    return entry.LogEntryDetails.FirstOrDefault(row => row.DetailTypeId == detailId)?.Value ?? "";
                case "exception":
                    if (detailId == -1)
                        detailId = context.MessageDetailTypes.First(row => row.Value == "Exception").Id;
                    return entry.LogEntryDetails.FirstOrDefault(row => row.DetailTypeId == detailId)?.Value ?? "";
                case "datacount":
                    if (detailId == -1)
                        detailId = context.MessageDetailTypes.First(row => row.Value == "DataCount").Id;
                    return entry.LogEntryDetails.FirstOrDefault(row => row.DetailTypeId == detailId)?.Value ?? "";
                case "gatewaymonitorid":
                    if (detailId == -1)
                        detailId = context.MessageDetailTypes.First(row => row.Value == "GatewayMonitorId").Id;
                    return entry.LogEntryDetails.FirstOrDefault(row => row.DetailTypeId == detailId)?.Value ?? "";
                case "clientioid":
                    if (detailId == -1)
                        detailId = context.MessageDetailTypes.First(row => row.Value == "ClientIoId").Id;
                    return entry.LogEntryDetails.FirstOrDefault(row => row.DetailTypeId == detailId)?.Value ?? "";
                case "version":
                    if (detailId == -1)
                        detailId = context.MessageDetailTypes.First(row => row.Value == "Version").Id;
                    return entry.LogEntryDetails.FirstOrDefault(row => row.DetailTypeId == detailId)?.Value ?? "";
                case "origin":
                    if (detailId == -1)
                        detailId = context.MessageDetailTypes.First(row => row.Value == "Origin").Id;
                    return entry.LogEntryDetails.FirstOrDefault(row => row.DetailTypeId == detailId)?.Value ?? "";
                case "type":
                    return context.MessageTypes.First(row => row.Id == entry.MessageTypeId).Name;
                case "date":
                    return entry.EntryDate.ToString(@"yyyy\/MM\/dd HH:mm:ss.fff");
                case "reason":
                    if (detailId == -1)
                        detailId = context.MessageDetailTypes.First(row => row.Value == "Reason").Id;
                    return entry.LogEntryDetails.FirstOrDefault(row => row.DetailTypeId == detailId)?.Value ?? "";
                case "message":
                    if (detailId == -1)
                        detailId = context.MessageDetailTypes.First(row => row.Value == "Message").Id;
                    return entry.LogEntryDetails.FirstOrDefault(row => row.DetailTypeId == detailId)?.Value ?? "";
                default:
                    return Name;
            }
        }
    }
}
