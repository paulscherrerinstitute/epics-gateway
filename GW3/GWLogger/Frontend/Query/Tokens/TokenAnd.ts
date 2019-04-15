class TokenAnd extends Token
{

    public tokenType: TokenType = TokenType.TokenAnd;

    public proposals: SuggestionInterface[] = <SuggestionInterface[]>[
        { suggestion: "and" },
        { suggestion: "&&" }
    ];

    public canBeUsed(parser: QueryParser): boolean
    {
        parser.skipSpaces();
        return ((parser.peekChar() == '&' && parser.peekChar(1) == '&') || parser.peekString().toLowerCase() == "and");
    }

    public extract(parser: QueryParser): Token
    {
        parser.skipSpaces();
        var token = new TokenAnd;
        if (parser.peekString().toLowerCase() == "and")
        {
            parser.nextString();
            token.value = "&&";
        }
        else
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