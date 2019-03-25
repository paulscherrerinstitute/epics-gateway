using GWLogger.Backend.DataContext.Query.Tokens;
using System;
using System.Collections.Generic;

namespace GWLogger.Backend.DataContext.Query.Statement
{
    public class GroupNode : QueryNode
    {
        public List<string> Fields { get; } = new List<string>();

        internal GroupNode(QueryParser parser)
        {
            parser.Tokens.Next(); // Skip the group
            // Skip a possible "by" keyword
            if (parser.Tokens.Peek() is TokenName && parser.Tokens.Peek().Value.ToLower() == "by")
                parser.Tokens.Next();

            while (parser.Tokens.HasToken())
            {
                var next = parser.Tokens.Next();
                if (!(next is TokenName))
                    throw new SyntaxException("Was expecting a TokenName and found a " + next.GetType().Name + " instead");

                Fields.Add(next.Value == "channel" ? "channelname" : next.Value);

                if (!parser.Tokens.HasToken())
                    break;
                next = parser.Tokens.Peek();
                if (!(next is TokenComa))
                    throw new SyntaxException("Was expecting a TokenComa and found a " + next.GetType().Name + " instead");
                parser.Tokens.Next();
            }
        }

        public override bool CheckCondition(Context context, LogEntry entry) => throw new NotImplementedException();
        public override string Value(Context context, LogEntry entry) => throw new NotImplementedException();
    }
}
