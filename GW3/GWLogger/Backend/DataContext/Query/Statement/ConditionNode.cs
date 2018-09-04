using System;

namespace GWLogger.Backend.DataContext.Query.Statement
{
    internal class ConditionNode : QueryNode
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
                    return Left.Value(context, entry).IndexOf(Right.Value(context, entry), StringComparison.InvariantCultureIgnoreCase) != -1;
                //return Left.Value(context, entry).ToLower().Contains(Right.Value(context, entry).ToLower());
                case "starts":
                    return Left.Value(context, entry).IndexOf(Right.Value(context, entry), StringComparison.InvariantCultureIgnoreCase) == 0;
                //return Left.Value(context, entry).ToLower().StartsWith(Right.Value(context, entry).ToLower());
                case "ends":
                    var a = Left.Value(context, entry);
                    var b = Right.Value(context, entry);
                    return a.IndexOf(b, StringComparison.InvariantCultureIgnoreCase) == a.Length - b.Length;
                    //return Left.Value(context, entry).ToLower().EndsWith(Right.Value(context, entry).ToLower());
            }
            throw new UnknownConditionException("Unknown condition '" + Condition + "'");
        }

        internal override string Value(Context context, LogEntry entry)
        {
            throw new NotImplementedException();
        }
    }
}
