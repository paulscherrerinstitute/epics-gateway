using GWLogger.Backend.DataContext.Query.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GWLogger.Backend.DataContext.Query.Statement
{
    class VariableNode : QueryNode
    {
        public Token Token { get; }

        internal VariableNode(Tokens.Token token)
        {
            Token = token;
        }

        internal override bool CheckCondition()
        {
            throw new NotImplementedException();
        }

        internal override string Value()
        {
            throw new NotImplementedException();
        }
    }
}
