using GWLogger.Backend.DataContext.Query.Tokens;
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
        public OrderNode Orders { get; } = null;
        public GroupNode Group { get; } = null;
        public int? Limit { get; } = null;
        public QueryNode Where { get; set; } = null;
        private static DateTime _jan1st1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private Dictionary<int, int> logLevels = null;
        private Dictionary<int, string> convertion = null;
        private Dictionary<int, string> detailTypes = null;
        private Dictionary<int, string> messageTypes = null;

        internal SelectNode()
        {
        }

        internal SelectNode(QueryParser parser)
        {
            parser.Tokens.Next(); // Skip the select
            Token next;

            while (parser.Tokens.HasToken())
            {
                var node = SelectNode.Get(parser, true);
                //var next = parser.Tokens.Next();
                if (!(node is FunctionNode) && !(node is VariableNode))
                    throw new SyntaxException("Was expecting a function or a variable and found a " + node.GetType().Name + " instead");

                var c = new QueryColumn { Field = (INamedNode)node, DisplayTitle = ((INamedNode)node).Name };
                if (c.Field.Name == "channel")
                    c.Field = new VariableNode("channelname");
                Columns.Add(c);

                if (!parser.Tokens.HasToken())
                    break;
                next = parser.Tokens.Peek();
                if (next is TokenString || next is TokenName)
                {
                    c.DisplayTitle = parser.Tokens.Next().Value;
                    next = parser.Tokens.Peek();
                }

                if (next is TokenWhere)
                {
                    parser.Tokens.Next(); // Skip next
                    Where = QueryNode.Get(parser, true);

                    next = parser.Tokens.Peek();
                    if (next != null && next is TokenGroup)
                        Group = new GroupNode(parser);
                    break;
                }
                else if (next is TokenGroup)
                {
                    Group = new GroupNode(parser);
                    break;
                }
                else if (next is TokenOrder)
                    break;
                else if (next is TokenLimit)
                    break;
                else if (!(next is TokenComa))
                    throw new SyntaxException("Was expecting a TokenComa and found a " + next.GetType().Name + " instead");
                parser.Tokens.Next();
            }

            // Add order by
            if (parser.Tokens.HasToken() && parser.Tokens.Peek() is TokenOrder)
            {
                Orders = new OrderNode(parser);
            }

            if (parser.Tokens.HasToken() && parser.Tokens.Peek() is TokenLimit)
            {
                parser.Tokens.Next();
                next = parser.Tokens.Next();
                if (!(next is TokenNumber))
                    throw new SyntaxException("Was expecting a TokenNumber and found a " + next.GetType().Name + " instead");
                Limit = int.Parse(next.Value);
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

        private Dictionary<string, string> knownIps = null;
        private string Hostname(string remoteIpPoint)
        {
            if (knownIps == null)
                knownIps = new Dictionary<string, string>();
            var ip = remoteIpPoint.Split(new char[] { ':' }).First();
            if (!knownIps.ContainsKey(ip))
            {
                string host = ip;
                try
                {
                    host = System.Net.Dns.GetHostEntry(ip).HostName;
                }
                catch
                {
                }
                knownIps.Add(ip, host);
            }
            return knownIps[ip];
        }

        private object ColumnValue(Context context, QueryColumn column, LogEntry entry)
        {
            switch (column.Field.Name)
            {
                case "entrydate":
                case "date":
                    return entry.EntryDate.ToUniversalTime();
                case "remoteippoint":
                case "remote":
                    return entry.RemoteIpPoint;
                case "hostname":
                    return Hostname(entry.RemoteIpPoint);
                case "messagetypeid":
                    return entry.MessageTypeId;
                case "type":
                    return (messageTypes.ContainsKey(entry.MessageTypeId) ? messageTypes[entry.MessageTypeId] : entry.MessageTypeId.ToString());
                case "level":
                    return (logLevels.ContainsKey(entry.MessageTypeId) ? logLevels[entry.MessageTypeId] : 0);
                case "display":
                    return Convert(entry.RemoteIpPoint, entry.MessageTypeId, entry.LogEntryDetails);
                case "position":
                    return entry.Position;
                case "currentfile":
                    return entry.CurrentFile;
                default:
                    return entry.LogEntryDetails.FirstOrDefault(d => detailTypes.ContainsKey(d.DetailTypeId) && detailTypes[d.DetailTypeId].ToLower() == column.Field.Name)?.Value;
            }
        }

        public object Value(Context context, LogEntry entry, string column)
        {
            if (logLevels == null && context != null)
            {
                logLevels = context.MessageTypes.ToDictionary(key => key.Id, val => val.LogLevel);
                convertion = context.MessageTypes.ToDictionary(key => key.Id, val => val.DisplayMask);
                messageTypes = context.MessageTypes.ToDictionary(key => key.Id, val => val.Name);
                detailTypes = context.MessageDetailTypes.ToDictionary(key => key.Id, val => val.Value);
            }

            return ColumnValue(context, column, entry);
        }

        public object[] Values(Context context, LogEntry entry)
        {
            if (logLevels == null && context != null)
            {
                logLevels = context.MessageTypes.ToDictionary(key => key.Id, val => val.LogLevel);
                convertion = context.MessageTypes.ToDictionary(key => key.Id, val => val.DisplayMask);
                messageTypes = context.MessageTypes.ToDictionary(key => key.Id, val => val.Name);
                detailTypes = context.MessageDetailTypes.ToDictionary(key => key.Id, val => val.Value);
            }

            return Columns.Select(c => ColumnValue(context, c, entry)).ToArray();
        }

        public override bool CheckCondition(Context context, LogEntry entry)
        {
            return Where.CheckCondition(context, entry);
        }

        public override string Value(Context context, LogEntry entry) => throw new System.NotImplementedException();
        internal List<object> GroupedValues(Context context, IEnumerable<IGrouping<string, LogEntry>> grouped)
        {
            var result = new List<object>();
            foreach (var row in grouped)
            {
                var rowOut = new object[this.Columns.Count];
                for (var i = 0; i < this.Columns.Count; i++)
                {
                    var c = this.Columns[i];
                    if (c.Field is FunctionNode)
                        rowOut[i] = ((FunctionNode)c.Field).ExecuteGrouping(context, row.AsEnumerable());
                    else
                        rowOut[i] = row.Key;
                }
                result.Add(rowOut);
            }
            return result;
        }
    }
}
