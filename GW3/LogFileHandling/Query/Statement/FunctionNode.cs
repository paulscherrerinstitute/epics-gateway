using GWLogger.Backend.DataContext.Query.Tokens;
using System;
using System.Collections.Generic;

namespace GWLogger.Backend.DataContext.Query.Statement
{
    public class FunctionNode : QueryNode
    {
        public string FunctionName { get; }
        public List<QueryNode> Parameters { get; } = new List<QueryNode>();

        internal FunctionNode(QueryParser parser)
        {
            FunctionName = parser.Tokens.Next().Value.ToLower();
            var next = parser.Tokens.Next();
            if (!(next is TokenOpenParenthesis))
                throw new InvalidTokenException("Was expecting an open parenthesis and instead received " + next.GetType());

            if (!(parser.Tokens.Peek() is TokenCloseParenthesis))
                while (parser.Tokens.HasToken())
                {
                    var p = QueryNode.Get(parser);
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
    }
}
