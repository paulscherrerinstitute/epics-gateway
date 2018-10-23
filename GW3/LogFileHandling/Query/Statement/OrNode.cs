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

        internal override bool CheckCondition(Context context, LogEntry entry)
        {
            return StatementA.CheckCondition(context, entry) || StatementB.CheckCondition(context, entry);
        }

        internal override string Value(Context context, LogEntry entry)
        {
            throw new NotImplementedException();
        }
    }
}
