using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GWLogger.Backend.DataContext.Query.Tokens
{
    internal class TokenOr : Token
    {
        public override bool CanBeUsed(QueryParser parser)
        {
            parser.SkipSpaces();
            return ((parser.PeekChar() == '|' && parser.PeekChar(1) == '|') || parser.PeekString().ToLower() == "or");
        }

        public override Token Extract(QueryParser parser)
        {
            parser.SkipSpaces();
            if (parser.PeekString().ToLower() == "or")
            {
                parser.NextString();
                return new TokenOr { Value = "||" };
            }
            return new TokenOr { Value = "" + parser.NextChar() + parser.NextChar() };
        }
    }
}
