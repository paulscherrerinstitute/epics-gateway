using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GWLogger.Backend.DataContext.Query.Tokens
{
    internal class TokenString : Token
    {
        public override bool CanBeUsed(QueryParser parser)
        {
            parser.SkipSpaces();
            return (parser.PeekChar() == '\"' || parser.PeekChar() == '\'');
        }

        public override Token Extract(QueryParser parser)
        {
            var extracted = "" + parser.NextChar();
            while (parser.HasChar())
            {
                var c = parser.NextChar();
                extracted += c;
                if (c == extracted[0])
                    break;
            }
            return new TokenString { Value = extracted.Substring(1, extracted.Length - 2) };
        }
    }
}
