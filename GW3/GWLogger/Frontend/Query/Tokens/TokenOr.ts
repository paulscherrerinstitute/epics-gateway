class TokenOr extends Token
{

    public tokenType: TokenType = TokenType.TokenOr;

    public proposals: SuggestionInterface[] = <SuggestionInterface[]>[
        { suggestion: "or" },
        { suggestion: "||" }
    ];

    public canBeUsed(parser: QueryParser): boolean
    {
        parser.skipSpaces();
        return ((parser.peekChar() == '|' && parser.peekChar(1) == '|') || parser.peekString().toLowerCase() == "or");
    }

    public extract(parser: QueryParser): Token
    {
        parser.skipSpaces();
        var token = new TokenOr();
        if (parser.peekString().toLowerCase() == "or")
        {
            parser.nextString();
            token.value = "||";
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
                case TokenType.TokenName:
                    return [new TokenCompare()];
                case TokenType.TokenOpenParenthesis:
                    return [new TokenName(), new TokenOpenParenthesis()];
            }
        }
        return [];
    }
}