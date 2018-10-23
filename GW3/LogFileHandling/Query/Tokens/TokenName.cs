using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GWLogger.Backend.DataContext.Query.Tokens
{
    internal class TokenName : Token
    {
        const string allowedChar = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_";
        const string secondChar = "0123456789";
        readonly static string[] keywords = new string[] { "and", "or", "contains", "starts", "ends" };

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
