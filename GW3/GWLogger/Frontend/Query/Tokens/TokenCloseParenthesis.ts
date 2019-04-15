class TokenCloseParenthesis extends Token
{

    public tokenType: TokenType = TokenType.TokenCloseParenthesis;

    public proposals: SuggestionInterface[] = <SuggestionInterface[]>[{ suggestion: ")" }];

    public canBeUsed(parser: QueryParser): boolean
    {
        parser.skipSpaces();
        return parser.peekChar() == ')';
    }

    public extract(parser: QueryParser): Token
    {
        parser.skipSpaces();
        var token = new TokenCloseParenthesis();
        token.value = parser.nextChar();
        return token;
    }

    public getProposals(nextToken?: Token, afterNextToken?: Token): Token[]
    {

        if (typeof nextToken != 'undefined' && typeof afterNextToken != 'undefined')
        {
            switch (nextToken.tokenType)
            {
                case TokenType.TokenAnd:
                case TokenType.TokenOr:
                case TokenType.TokenWhere:
                    return [new TokenName(), new TokenOpenParenthesis()];
                case TokenType.TokenComa:
                    return [new TokenName(), new TokenStar(), new TokenOperation()];
                case TokenType.TokenGroup:
                case TokenType.TokenOrder:
                    return [new TokenName()];
                case TokenType.TokenLimit:
                    return [new TokenNumber()];
                case TokenType.TokenString:
                case TokenType.TokenName:
                    return [new TokenComa(), new TokenGroup(), new TokenLimit(), new TokenOrder(), new TokenWhere()];
                case TokenType.TokenCloseParenthesis:
                    return [
                        new TokenAnd(),
                        new TokenOr(),
                        new TokenOrder(),
                        new TokenGroup(),
                        new TokenLimit(),
                        new TokenWhere()
                    ];
            }
        }
        return [];
    }
}