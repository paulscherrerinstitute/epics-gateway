using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GWLogger.Backend.DataContext.Query.Tokens
{
    public abstract class Token
    {
        public string Value { get; protected set; }

        abstract public bool CanBeUsed(QueryParser parser);
        abstract public Token Extract(QueryParser parser);
    }
}
