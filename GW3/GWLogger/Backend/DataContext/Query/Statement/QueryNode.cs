using GWLogger.Backend.DataContext.Query.Tokens;

namespace GWLogger.Backend.DataContext.Query.Statement
{
    internal abstract class QueryNode
    {
        internal static QueryNode Get(QueryParser parser)
        {
            return And(parser);
        }

        private static QueryNode And(QueryParser parser)
        {
            var node = Or(parser);
            if (!parser.Tokens.HasToken())
                return node;
            if (parser.Tokens.Peek() is TokenAnd)
            {
                parser.Tokens.Next();
                return new AndNode(node, Get(parser));
            }
            return node;
        }

        private static QueryNode Or(QueryParser parser)
        {
            var node = Condition(parser);
            if (!parser.Tokens.HasToken())
                return node;
            if (parser.Tokens.Peek() is TokenOr)
            {
                parser.Tokens.Next();
                return new OrNode(node, Get(parser));
            }
            return node;
        }

        private static QueryNode Condition(QueryParser parser)
        {
            var a = Base(parser);
            if (!parser.Tokens.HasToken() || !(parser.Tokens.Peek() is TokenCompare))
                throw new MissingTokenException("Was expecting a condition check.");
            var condition = parser.Tokens.Next();
            if (!parser.Tokens.HasToken())
                throw new MissingTokenException("Was expecting a second value to compare to.");
            var b = Base(parser);
            return new ConditionNode(a, condition.Value, b);
        }

        private static QueryNode Base(QueryParser parser)
        {
            switch (parser.Tokens.Peek())
            {
                case TokenOpenParenthesis tOpen:
                    var node = Get(parser);
                    if (!(parser.Tokens.Peek() is TokenCloseParenthesis))
                        throw new MissingTokenException("Was expecting a ')'");
                    parser.Tokens.Next();
                    return node;
                case TokenString tokenString:
                    return new ValueNode(parser.Tokens.Next());
                case TokenNumber tokenValue:
                    return new ValueNode(parser.Tokens.Next());
                case TokenName tokenName:
                    return new VariableNode(parser.Tokens.Next());
                default:
                    throw new InvalidTokenException("Wasn't expecting a " + parser.Tokens.Peek().GetType().Name + " here.");
            }
        }

        internal abstract bool CheckCondition(Context context, LogEntry entry);

        internal abstract string Value(Context context, LogEntry entry);
    }
}
