namespace GWLogger.Backend.DataContext.Query.Tokens
{
    internal class TokenStar : Token
    {
        public override bool CanBeUsed(QueryParser parser)
        {
            parser.SkipSpaces();

            return parser.PeekChar() == '*';
        }

        public override Token Extract(QueryParser parser)
        {
            parser.SkipSpaces();
            return new TokenStar { Value = "" + parser.NextChar() };
        }
    }
}
