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

        internal override bool CheckCondition()
        {
            switch (Condition)
            {
                case "!=":
                    return Left.Value() != Right.Value();
                case "=":
                    return Left.Value() == Right.Value();
                case ">":
                    return int.Parse(Left.Value()) > int.Parse(Right.Value());
                case "<":
                    return int.Parse(Left.Value()) < int.Parse(Right.Value());
                case ">=":
                    return int.Parse(Left.Value()) >= int.Parse(Right.Value());
                case "<=":
                    return int.Parse(Left.Value()) <= int.Parse(Right.Value());
                case "contains":
                    return Left.Value().ToLower().Contains(Right.Value().ToLower());
                case "starts":
                    return Left.Value().ToLower().StartsWith(Right.Value().ToLower());
                case "ends":
                    return Left.Value().ToLower().EndsWith(Right.Value().ToLower());
            }
            throw new UnknownConditionException("Unknown condition '" + Condition + "'");
        }

        internal override string Value()
        {
            throw new NotImplementedException();
        }
    }
}
