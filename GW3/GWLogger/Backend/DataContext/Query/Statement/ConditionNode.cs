using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GWLogger.Backend.DataContext.Query.Statement
{
    class ConditionNode : QueryNode
    {
        public QueryNode Left { get; }
        public string Condition { get; }
        public QueryNode Right { get; }

        internal ConditionNode(QueryNode left, string condition, QueryNode right)
        {
            Left = left;
            Condition = condition;
            Right = right;
        }

        internal override bool CheckCondition(Context context, LogEntry entry)
        {
            switch (Condition)
            {
                case "!=":
                    return Left.Value(context, entry) != Right.Value(context, entry);
                case "=":
                    return Left.Value(context, entry) == Right.Value(context, entry);
                case ">":
                    return int.Parse(Left.Value(context, entry)) > int.Parse(Right.Value(context, entry));
                case "<":
                    return int.Parse(Left.Value(context, entry)) < int.Parse(Right.Value(context, entry));
                case ">=":
                    return int.Parse(Left.Value(context, entry)) >= int.Parse(Right.Value(context, entry));
                case "<=":
                    return int.Parse(Left.Value(context, entry)) <= int.Parse(Right.Value(context, entry));
                case "contains":
                    return Left.Value(context, entry).ToLower().Contains(Right.Value(context, entry).ToLower());
                case "starts":
                    return Left.Value(context, entry).ToLower().StartsWith(Right.Value(context, entry).ToLower());
                case "ends":
                    return Left.Value(context, entry).ToLower().EndsWith(Right.Value(context, entry).ToLower());
            }
            throw new UnknownConditionException("Unknown condition '" + Condition + "'");
        }

        internal override string Value(Context context, LogEntry entry)
        {
            throw new NotImplementedException();
        }
    }
}
