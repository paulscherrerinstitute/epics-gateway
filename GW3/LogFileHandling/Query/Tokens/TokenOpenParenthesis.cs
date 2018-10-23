using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GWLogger.Backend.DataContext.Query.Tokens
{
    internal class TokenOpenParenthesis : Token
    {
        public override bool CanBeUsed(QueryParser parser)
        {
            parser.SkipSpaces();
            return parser.PeekChar() == '(';
        }

        public override Token Extract(QueryParser parser)
        {
            parser.SkipSpaces();
            return new TokenOpenParenthesis { Value = "" + parser.NextChar() };
        }
    }
}
