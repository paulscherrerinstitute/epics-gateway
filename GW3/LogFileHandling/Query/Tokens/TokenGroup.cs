using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GWLogger.Backend.DataContext.Query.Tokens
{
    internal class TokenGroup : Token
    {
        public override bool CanBeUsed(QueryParser parser)
        {
            parser.SkipSpaces();

            return parser.PeekString().ToLower() == "group";
        }

        public override Token Extract(QueryParser parser)
        {
            parser.SkipSpaces();
            parser.NextString();
            if (parser.PeekString().ToLower() == "by")
                parser.NextString();
            return new TokenGroup();
        }
    }
}
