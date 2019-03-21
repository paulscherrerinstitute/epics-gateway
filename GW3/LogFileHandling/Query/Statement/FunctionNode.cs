using GWLogger.Backend.DataContext.Query.Tokens;
using System;

namespace GWLogger.Backend.DataContext.Query.Statement
{
    public class FunctionNode : QueryNode
    {
        public string FunctionName { get; }

        internal FunctionNode(QueryParser parser)
        {
            FunctionName = parser.Tokens.Next().Value.ToLower();
            var next = parser.Tokens.Next();
            if (!(next is TokenOpenParenthesis))
                throw new InvalidTokenException("Was expecting an open parenthesis and instead received " + next.GetType());

        }

        public override bool CheckCondition(Context context, LogEntry entry) => throw new NotImplementedException();
        public override string Value(Context context, LogEntry entry) => throw new NotImplementedException();
    }
}
