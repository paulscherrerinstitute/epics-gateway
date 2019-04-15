class TokenCompare extends Token
{

    public tokenType: TokenType = TokenType.TokenCompare;

    public proposals: SuggestionInterface[] = <SuggestionInterface[]>[
        { suggestion: "!=", hint: "Not equal" },
        { suggestion: "=", hint: "Equal" },
        { suggestion: ">", hint: "Bigger than" },
        { suggestion: "<", hint: "Smaller than" },
        { suggestion: ">=", hint: "Bigger or equal than" },
        { suggestion: "<=", hint: "Smaller or equal than" },
        { suggestion: "contains", hint: "Contains" },
        { suggestion: "starts", hint: "Starts with" },
        { suggestion: "ends", hint: "Ends with" }
    ];

    static keywords: string[] = ["contains", "starts", "ends"];

    public canBeUsed(parser: QueryParser): boolean
    {
        parser.skipSpaces();
        return ((parser.peekChar() == '=') ||
            parser.peekChar() == '<' ||
            parser.peekChar() == '>' ||
            (parser.peekChar() == '!' && parser.peekChar(1) == '=') || TokenCompare.keywords.indexOf(parser.peekString().toLowerCase()) > -1);
    }

    public extract(parser: QueryParser): Token
    {
        parser.skipSpaces();
        var token = new TokenCompare()
        if (TokenCompare.keywords.indexOf(parser.peekString().toLowerCase()) > -1)
        {
            token.value = parser.nextString().toLowerCase()
            return token;
        }
        if ((parser.peekChar() == '<' || parser.peekChar() == '>' || parser.peekChar() == '=') && parser.peekChar(1) != '=')
        {
            token.value = parser.nextChar();
            return token;
        }
        token.value = parser.nextChar() + parser.nextChar();
        return token;
    }

    public getProposals(nextToken?: Token, afterNextToken?: Token): Token[]
    {
        if (typeof nextToken != 'undefined' && typeof afterNextToken != 'undefined')
        {
            switch (nextToken.tokenType)
            {
                case TokenType.TokenCompare:
                    return [new TokenString(), new TokenName(), new TokenNumber()];
                case TokenType.TokenString:
                case TokenType.TokenName:
                case TokenType.TokenNumber:
                    return [
                        new TokenAnd(),
                        new TokenGroup(),
                        new TokenLimit(),
                        new TokenOr(),
                        new TokenOrder(),
                        new TokenCloseParenthesis()
                    ];
            }
        }
        return [];
    }
}