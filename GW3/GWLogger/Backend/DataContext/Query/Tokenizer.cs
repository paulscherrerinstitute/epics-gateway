using GWLogger.Backend.DataContext.Query.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GWLogger.Backend.DataContext.Query
{
    class Tokenizer
    {
        List<Token> Tokens { get; } = new List<Token>();
        public int Position { get; set; }
        static List<Token> KnownTokenTypes;

        static Tokenizer()
        {
            KnownTokenTypes = Assembly.GetExecutingAssembly()
                .DefinedTypes
                .Where(row => row.IsSubclassOf(typeof(Token)))
                .Select(row => (Token)row.GetConstructor(new Type[] { })
                    .Invoke(new object[] { })).ToList();
        }

        internal Tokenizer(QueryParser parser)
        {
            while (parser.HasChar())
            {
                parser.SkipSpaces();
                var possibleToken = KnownTokenTypes.FirstOrDefault(row => row.CanBeUsed(parser));
                if (possibleToken == null)
                    throw new InvalidTokenException("Token '" + parser.PeekChar() + "' not expected.");
                Tokens.Add(possibleToken.Extract(parser));
            }
        }

        internal bool HasToken()
        {
            return Position < this.Tokens.Count;
        }

        public Token Peek(int offset = 0)
        {
            return Tokens[Position + offset];
        }

        internal Token Next()
        {
            return Tokens[Position++];
        }
    }
}
