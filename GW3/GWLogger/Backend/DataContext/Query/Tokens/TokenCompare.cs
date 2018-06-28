using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GWLogger.Backend.DataContext.Query.Tokens
{
    internal class TokenCompare : Token
    {
        readonly static string[] keywords = new string[] { "contains", "starts", "ends" };

        public override bool CanBeUsed(QueryParser parser)
        {
            parser.SkipSpaces();
            return ((parser.PeekChar() == '=') ||
                parser.PeekChar() == '<' ||
                parser.PeekChar() == '>' ||
                (parser.PeekChar() == '!' && parser.PeekChar(1) == '=') || keywords.Contains(parser.PeekString().ToLower()));
        }

        public override Token Extract(QueryParser parser)
        {
            parser.SkipSpaces();
            if (keywords.Contains(parser.PeekString().ToLower()))
                return new TokenCompare { Value = parser.NextString().ToLower() };
            if ((parser.PeekChar() == '<' || parser.PeekChar() == '>' || parser.PeekChar() == '=') && parser.PeekChar(1) != '=')
                return new TokenCompare { Value = "" + parser.NextChar() };
            return new TokenCompare { Value = "" + parser.NextChar() + parser.NextChar() };
        }
    }
}
