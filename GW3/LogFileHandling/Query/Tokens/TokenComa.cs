using System;
using System.Collections.Generic;
using System.Text;

namespace GWLogger.Backend.DataContext.Query.Tokens
{
    class TokenComa : Token
    {
        public override bool CanBeUsed(QueryParser parser)
        {
            parser.SkipSpaces();
            return parser.PeekChar() == ',';
        }

        public override Token Extract(QueryParser parser)
        {
            parser.SkipSpaces();
            parser.NextChar();
            return new TokenComa();
        }
    }
}
