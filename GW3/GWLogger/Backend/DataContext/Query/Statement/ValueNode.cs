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

        internal override bool CheckCondition()
        {
            throw new NotImplementedException();
        }

        internal override string Value()
        {
            return Token.Value;
        }
    }
}
