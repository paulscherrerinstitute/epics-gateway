using System.Linq;

namespace GWLogger.Backend.DataContext.Query.Tokens
{
    internal class TokenName : Token
    {
        private const string allowedChar = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_";
        private const string secondChar = "0123456789";
        private static readonly string[] keywords = new string[] { "and", "or", "contains", "starts", "ends", "select", "where", "group" };

        public override bool CanBeUsed(QueryParser parser)
        {
            parser.SkipSpaces();
            return allowedChar.IndexOf(parser.PeekChar()) != -1 && !keywords.Contains(parser.PeekString().ToLower());
        }

        public override Token Extract(QueryParser parser)
        {
            var extracted = "";
            parser.SkipSpaces();

            while (parser.HasChar())
            {
                if (extracted.Length > 0 && allowedChar.IndexOf(parser.PeekChar()) == -1 && secondChar.IndexOf(parser.PeekChar()) == -1)
                    break;
                else if (extracted.Length == 0 && allowedChar.IndexOf(parser.PeekChar()) == -1)
                    break;
                extracted += parser.NextChar();
            }
            return new TokenName { Value = extracted };
        }
    }
}
