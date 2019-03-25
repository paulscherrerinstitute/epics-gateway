using GWLogger.Backend.DataContext.Query.Tokens;
using System;
using System.Collections.Generic;

namespace GWLogger.Backend.DataContext.Query.Statement
{
    public class OrderNode : QueryNode
    {
        public List<OrderColumn> Columns { get; } = new List<OrderColumn>();

        internal OrderNode(QueryParser parser)
        {
            parser.Tokens.Next(); // Skip the group

            while (parser.Tokens.HasToken())
            {
                var next = parser.Tokens.Next();
                if (!(next is TokenName || next is TokenString))
                    throw new SyntaxException("Was expecting a TokenName or a TokenString and found a " + next.GetType().Name + " instead");

                var order = Direction.Ascending;
                if (parser.Tokens.Peek() is TokenAscending)
                    parser.Tokens.Next();
                else if (parser.Tokens.Peek() is TokenDescending)
                {
                    order = Direction.Descending;
                    parser.Tokens.Next();
                }

                Columns.Add(new OrderColumn { Name = next.Value.ToLower(), Direction = order });

                if (!parser.Tokens.HasToken())
                    break;
                next = parser.Tokens.Peek();
                if (next is TokenLimit)
                    break;
                else if (!(next is TokenComa))
                    throw new SyntaxException("Was expecting a TokenComa and found a " + next.GetType().Name + " instead");
                parser.Tokens.Next();
            }
        }

        public override bool CheckCondition(Context context, LogEntry entry) => throw new NotImplementedException();
        public override string Value(Context context, LogEntry entry) => throw new NotImplementedException();
    }
}
