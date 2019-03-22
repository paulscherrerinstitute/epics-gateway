﻿using GWLogger.Backend.DataContext.Query.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace GWLogger.Backend.DataContext.Query.Statement
{
    public class SelectNode : QueryNode
    {
        public List<QueryColumn> Columns { get; } = new List<QueryColumn>();
        public GroupNode Group { get; } = null;
        public QueryNode Where { get; set; } = null;
        private static DateTime _jan1st1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private Dictionary<int, int> logLevels = null;
        private Dictionary<int, string> convertion = null;
        private Dictionary<int, string> detailTypes = null;

        internal SelectNode(QueryParser parser)
        {
            parser.Tokens.Next(); // Skip the select

            while (parser.Tokens.HasToken())
            {
                var next = parser.Tokens.Next();
                if (!(next is TokenName))
                    throw new SyntaxException("Was expecting a TokenName and found a " + next.GetType().Name + " instead");

                var c = new QueryColumn { Field = next.Value.ToLower(), DisplayTitle = next.Value };
                Columns.Add(c);

                if (!parser.Tokens.HasToken())
                    break;
                next = parser.Tokens.Peek();
                if (next is TokenString || next is TokenName)
                    c.DisplayTitle = next.Value;
                else if (next is TokenWhere)
                {
                    parser.Tokens.Next(); // Skip next
                    Where = QueryNode.Get(parser);

                    next = parser.Tokens.Peek();
                    if (next != null && next is TokenGroup)
                        Group = new GroupNode(parser);
                    break;
                }
                else if(next is TokenGroup)
                {
                    Group = new GroupNode(parser);
                    break;
                }
                else if (!(next is TokenComa))
                    throw new SyntaxException("Was expecting a TokenComa and found a " + next.GetType().Name + " instead");
                parser.Tokens.Next();
            }
        }

        public SelectNode(QueryNode whereNode)
        {
            Columns = new List<QueryColumn> { "date", "remote", "type", "level", "message", "position", "currentfile" };
            Where = whereNode;
        }

        private string Convert(string remoteIpPoint, int messageType, IEnumerable<Backend.DataContext.LogEntryDetail> details)
        {
            if (!convertion.ContainsKey(messageType) || convertion[messageType] == null)
            {
                var result = new StringBuilder();
                result.Append(messageType.ToString());
                foreach (var i in details)
                {
                    result.Append(",");
                    result.Append(detailTypes.ContainsKey(i.DetailTypeId) ? detailTypes[i.DetailTypeId] : i.DetailTypeId.ToString());
                    result.Append("=");
                    result.Append(i.Value);
                }

                return result.ToString();
            }
            else
            {
                var line = convertion[messageType];
                var typesId = detailTypes.Keys.ToList();
                if (details != null)
                {
                    foreach (var i in details.Where(row => typesId.Contains(row.DetailTypeId)))
                        line = Regex.Replace(line, "\\{" + (detailTypes.ContainsKey(i.DetailTypeId) ? detailTypes[i.DetailTypeId] : i.DetailTypeId.ToString()) + "\\}", i.Value, RegexOptions.IgnoreCase);
                }
                if (remoteIpPoint != null)
                    line = Regex.Replace(line, "\\{endpoint\\}", remoteIpPoint, RegexOptions.IgnoreCase);
                return line;
            }
        }

        private object ColumnValue(Context context, QueryColumn column, LogEntry entry)
        {
            switch (column.Field)
            {
                case "entrydate":
                case "date":
                    return entry.EntryDate.ToUniversalTime();
                case "remoteippoint":
                case "remote":
                    return entry.RemoteIpPoint;
                case "messagetypeid":
                case "type":
                    return entry.MessageTypeId;
                case "level":
                    return (logLevels.ContainsKey(entry.MessageTypeId) ? logLevels[entry.MessageTypeId] : 0);
                case "message":
                    return Convert(entry.RemoteIpPoint, entry.MessageTypeId, entry.LogEntryDetails);
                case "position":
                    return entry.Position;
                case "currentfile":
                    return entry.CurrentFile;
                default:
                    return entry.LogEntryDetails.FirstOrDefault(d => detailTypes.ContainsKey(d.DetailTypeId) && detailTypes[d.DetailTypeId].ToLower() == column.Field).Value;
            }
        }

        public object[] Values(Context context, LogEntry entry)
        {
            if (logLevels == null && context != null)
            {
                logLevels = context.MessageTypes.ToDictionary(key => key.Id, val => val.LogLevel);
                convertion = context.MessageTypes.ToDictionary(key => key.Id, val => val.DisplayMask);
                detailTypes = context.MessageDetailTypes.ToDictionary(key => key.Id, val => val.Value);
            }

            return Columns.Select(c => ColumnValue(context, c, entry)).ToArray();
        }

        public override bool CheckCondition(Context context, LogEntry entry)
        {
            return Where.CheckCondition(context, entry);
        }

        public override string Value(Context context, LogEntry entry) => throw new System.NotImplementedException();
    }
}
