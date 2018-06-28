using GWLogger.Backend.DataContext.Query.Statement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GWLogger.Backend.DataContext.Query
{
    class QueryParser
    {
        public string Source { get; }
        public int Position { get; set; }
        public Tokenizer Tokens { get; }
        
        private QueryParser(string source)
        {
            this.Source = source;
            this.Position = 0;
            Tokens = new Tokenizer(this);
        }

        internal string PeekString()
        {
            var result = "";
            for (var i = Position; i < Source.Length; i++)
            {
                var c = Source[i];
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
                    result += c;
                else
                    break;
            }
            return result;
        }

        internal string NextString()
        {
            var result = "";
            while(Position < Source.Length)
            {
                var c = Source[Position];
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
                    result += c;
                else
                    break;
                Position++;
            }
            return result;
        }

        internal void SkipSpaces()
        {
            while (PeekChar() == ' ' || PeekChar() == '\t' || PeekChar() == '\n' || PeekChar() == '\r')
                NextChar();
        }

        internal char PeekChar(int offset = 0)
        {
            return Source[Position + offset];
        }

        internal char NextChar()
        {
            return Source[Position++];
        }

        public static QueryNode Parse(string query)
        {
            var result = new QueryParser(query);
            return result.Top();
        }

        internal bool HasChar()
        {
            return Position < Source.Length;
        }

        private QueryNode Top()
        {
            return QueryNode.Get(this);
        }
    }
}
