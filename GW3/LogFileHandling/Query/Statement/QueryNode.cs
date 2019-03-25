using GWLogger.Backend.DataContext.Query.Tokens;
using System;

namespace GWLogger.Backend.DataContext.Query.Statement
{
    public abstract class QueryNode
    {
        internal static QueryNode Get(QueryParser parser, bool allowFurtherTokens = false)
        {
            var next = parser.Tokens.Peek();
            if (next is TokenSelect)
                return new SelectNode(parser);

            var root = Or(parser);
            if (!allowFurtherTokens && parser.Tokens.HasToken())
                throw new SpareTokenException();
            return root;
        }

        private static QueryNode Or(QueryParser parser)
        {
            var left = And(parser);
            while (true)
            {
                if (parser.Tokens.HasToken() && parser.Tokens.Peek() is TokenOr)
                {
                    parser.Tokens.Next();
                    var right = And(parser);
                    left = new BinaryNode(left, right, (val1, val2) => val1 || val2);
                }
                else
                {
                    return left;
                }
            }
        }

        private static QueryNode And(QueryParser parser)
        {
            var left = Condition(parser);
            while (true)
            {
                if (parser.Tokens.HasToken() && parser.Tokens.Peek() is TokenAnd)
                {
                    parser.Tokens.Next();
                    var right = Condition(parser);
                    left = new BinaryNode(left, right, (val1, val2) => val1 && val2);
                }
                else
                {
                    return left;
                }
            }
        }

        private static QueryNode Condition(QueryParser parser)
        {
            var left = Base(parser);
            while (true)
            {
                if (parser.Tokens.HasToken() && parser.Tokens.Peek() is TokenCompare)
                {
                    var condition = parser.Tokens.Next().Value;
                    var right = Base(parser);
                    left = new ConditionNode(left, condition, right);
                }
                else
                {
                    return left;
                }
            }
        }

        private static QueryNode Base(QueryParser parser)
        {
            if (!parser.Tokens.HasToken())
                throw new MissingTokenException("There's missing something");

            try
            {
                switch (parser.Tokens.Peek())
                {
                    case TokenOpenParenthesis tOpen:
                        parser.Tokens.Next();
                        var node = Or(parser);
                        if (!(parser.Tokens.Next() is TokenCloseParenthesis))
                            throw new MissingTokenException("Was expecting a ')'");
                        return node;
                    case TokenString tokenString:
                        return new ValueNode(parser.Tokens.Next());
                    case TokenNumber tokenValue:
                        return new ValueNode(parser.Tokens.Next());
                    case TokenName tokenName:
                        var after = parser.Tokens.Peek(1);
                        if (after != null && after is TokenOpenParenthesis)
                            return new FunctionNode(parser);
                        return new VariableNode(parser.Tokens.Next());
                    case TokenStar tokenStar:
                        return new VariableNode(parser.Tokens.Next());
                    default:
                        throw new InvalidTokenException("Wasn't expecting a " + parser.Tokens.Peek().GetType().Name + " here.");
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new MissingTokenException("There's missing something");
            }
        }

        public abstract bool CheckCondition(Context context, LogEntry entry);

        public abstract string Value(Context context, LogEntry entry);
    }
}
