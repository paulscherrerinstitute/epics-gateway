using System;

namespace GWLogger.Backend.DataContext.Query.Statement
{
    internal class AndNode : QueryNode
    {
        public QueryNode StatementA { get; }
        public QueryNode StatementB { get; }

        internal AndNode(QueryNode statementA, QueryNode statementB)
        {
            StatementA = statementA;
            StatementB = statementB;
        }

        internal override bool CheckCondition(Context context, LogEntry entry)
        {
            return StatementA.CheckCondition(context, entry) && StatementB.CheckCondition(context, entry);
        }

        internal override string Value(Context context, LogEntry entry)
        {
            throw new NotImplementedException();
        }
    }
}
