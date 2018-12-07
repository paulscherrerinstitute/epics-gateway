using System;

namespace GWLogger.Backend.DataContext.Query.Statement
{
    class BinaryNode : QueryNode
    {
        public QueryNode StatementA { get; }
        public QueryNode StatementB { get; }
        public Func<bool, bool, bool> Operator { get; }

        internal BinaryNode(QueryNode statementA, QueryNode statementB, Func<bool, bool, bool> oparator)
        {
            StatementA = statementA;
            StatementB = statementB;
            Operator = oparator;
        }
        public override bool CheckCondition(Context context, LogEntry entry)
        {
            return Operator(StatementA.CheckCondition(context, entry), StatementB.CheckCondition(context, entry));
        }

        public override string Value(Context context, LogEntry entry)
        {
            throw new NotImplementedException();
        }
    }
}
