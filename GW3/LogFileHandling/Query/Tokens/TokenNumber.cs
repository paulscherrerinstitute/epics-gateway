using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GWLogger.Backend.DataContext.Query.Tokens
{
    internal class TokenNumber : Token
    {
        const string allowedChar = "0123456789";

        public override bool CanBeUsed(QueryParser parser)
        {
            parser.SkipSpaces();
            if (parser.PeekChar() == '.' && allowedChar.IndexOf(parser.PeekChar(1)) != -1)
                return true;
            return allowedChar.IndexOf(parser.PeekChar()) != -1 && parser.PeekChar() != '.';
        }

        public override Token Extract(QueryParser parser)
        {
            var extracted = "";
            parser.SkipSpaces();

            while (parser.HasChar())
            {
                if (allowedChar.IndexOf(parser.PeekChar()) == -1 && parser.PeekChar() != '.')
                    break;
                extracted += parser.NextChar();
            }
            return new TokenNumber { Value = extracted };
        }
    }
}
