using System;
using System.Linq;

namespace GWLogger.Backend.DataContext.Query.Statement
{
    internal class VariableNode : QueryNode
    {
        public string Name { get; }

        private int detailId = -1;
        private int secondId = -1;

        internal VariableNode(Tokens.Token token)
        {
            Name = token.Value.ToLower();
        }

        internal override bool CheckCondition(Context context, LogEntry entry)
        {
            throw new NotImplementedException();
        }

        internal override string Value(Context context, LogEntry entry)
        {
            switch (Name)
            {
                case "class":
                    if (detailId == -1)
                        detailId = context.MessageDetailTypes.First(row => row.Value == "SourceMemberName").Id;
                    if (secondId == -1)
                        secondId = context.MessageDetailTypes.First(row => row.Value == "SourceFilePath").Id;
                    return (entry.LogEntryDetails.FirstOrDefault(row => row.DetailTypeId == secondId)?.Value ?? "") + "@" + (entry.LogEntryDetails.FirstOrDefault(row => row.DetailTypeId == detailId)?.Value ?? "");
                case "line":
                    if (detailId == -1)
                        detailId = context.MessageDetailTypes.First(row => row.Value == "SourceLineNumber").Id;
                    return entry.LogEntryDetails.FirstOrDefault(row => row.DetailTypeId == detailId)?.Value ?? "";
                case "channel":
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
                    if (detailId == -1)
                        detailId = context.MessageDetailTypes.First(row => row.Value == "CommandId").Id;
                    return entry.LogEntryDetails.FirstOrDefault(row => row.DetailTypeId == detailId)?.Value ?? "";
                case "type":
                    return entry.MessageTypeId.ToString();
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
