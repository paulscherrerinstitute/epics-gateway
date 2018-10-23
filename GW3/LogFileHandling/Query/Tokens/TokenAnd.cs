using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GWLogger.Backend.DataContext.Query.Tokens
{
    internal class TokenAnd : Token
    {
        public override bool CanBeUsed(QueryParser parser)
        {
            parser.SkipSpaces();

            return ((parser.PeekChar() == '&' && parser.PeekChar(1) == '&') || parser.PeekString().ToLower() == "and");
        }

        public override Token Extract(QueryParser parser)
        {
            parser.SkipSpaces();
            if (parser.PeekString().ToLower() == "and")
            {
                parser.NextString();
                return new TokenAnd { Value = "&&" };
            }
            return new TokenAnd { Value = "" + parser.NextChar() + parser.NextChar() };
        }
    }
}
