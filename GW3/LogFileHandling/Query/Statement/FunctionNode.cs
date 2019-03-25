using GWLogger.Backend.DataContext.Query.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GWLogger.Backend.DataContext.Query.Statement
{
    public class FunctionNode : QueryNode, INamedNode
    {
        public string Name { get; }
        public List<QueryNode> Parameters { get; } = new List<QueryNode>();

        internal FunctionNode(QueryParser parser)
        {
            Name = parser.Tokens.Next().Value.ToLower();
            var next = parser.Tokens.Next();
            if (!(next is TokenOpenParenthesis))
                throw new InvalidTokenException("Was expecting an open parenthesis and instead received " + next.GetType());

            if (!(parser.Tokens.Peek() is TokenCloseParenthesis))
                while (parser.Tokens.HasToken())
                {
                    var p = QueryNode.Get(parser, true);
                    Parameters.Add(p);
                    if (!(parser.Tokens.Peek() is TokenComa))
                        break;
                    parser.Tokens.Next(); // Skip the coma
                }
            next = parser.Tokens.Next();
            if (!(next is TokenCloseParenthesis))
                throw new InvalidTokenException("Was expecting an close parenthesis and instead received " + next.GetType());
        }

        public override bool CheckCondition(Context context, LogEntry entry) => throw new NotImplementedException();

        public override string Value(Context context, LogEntry entry)
        {
            return null;
        }

        internal object ExecuteGrouping(Context context, IEnumerable<LogEntry> data)
        {
            switch (Name)
            {
                case "count":
                    return data.Count();
                case "min":
                    {
                        var s = new SelectNode();
                        return data.Min(row => s.Value(context, row, ((INamedNode)this.Parameters[0]).Name));
                    }
                case "max":
                    {
                        var s = new SelectNode();
                        return data.Max(row => s.Value(context, row, ((INamedNode)this.Parameters[0]).Name));
                    }
                case "sum":
                    {
                        var s = new SelectNode();
                        return data.Sum(row => Convert.ToDouble(s.Value(context, row, ((INamedNode) this.Parameters[0]).Name)));
                    }
                case "avg":
                case "average":
                    {
                        var s = new SelectNode();
                        return data.Average(row => Convert.ToDouble(s.Value(context, row, ((INamedNode)this.Parameters[0]).Name)));
                    }
                default:
                    return null;
            }
        }
    }
}
