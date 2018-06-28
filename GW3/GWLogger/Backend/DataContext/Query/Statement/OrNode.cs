using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GWLogger.Backend.DataContext.Query.Statement
{
    class OrNode : QueryNode
    {
        public QueryNode StatementA { get; }
        public QueryNode StatementB { get; }

        internal OrNode(QueryNode statementA, QueryNode statementB)
        {
            StatementA = statementA;
            StatementB = statementB;
        }

        internal override bool CheckCondition()
        {
            return StatementA.CheckCondition() || StatementB.CheckCondition();
        }

        internal override string Value()
        {
            throw new NotImplementedException();
        }
    }
}
