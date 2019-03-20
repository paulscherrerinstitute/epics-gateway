using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GWLogger.Backend.DataContext.Query.Tokens
{
    internal class TokenWhere : Token
    {
        public override bool CanBeUsed(QueryParser parser)
        {
            parser.SkipSpaces();

            return parser.PeekString().ToLower() == "where";
        }

        public override Token Extract(QueryParser parser)
        {
            parser.SkipSpaces();
            parser.NextString();
            return new TokenWhere();
        }
    }
}
