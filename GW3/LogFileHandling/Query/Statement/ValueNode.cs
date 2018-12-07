using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GWLogger.Backend.DataContext.Query.Tokens;

namespace GWLogger.Backend.DataContext.Query.Statement
{
    class ValueNode : QueryNode
    {
        public Token Token { get; }

        internal ValueNode(Tokens.Token token)
        {
            Token = token;
        }

        public override bool CheckCondition(Context context, LogEntry entry)
        {
            throw new NotImplementedException();
        }

        public override string Value(Context context, LogEntry entry)
        {
            return Token.Value;
        }
    }
}
