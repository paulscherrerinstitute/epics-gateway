class TokenOpenParenthesis extends Token {

    public tokenType: TokenType = TokenType.TokenOpenParenthesis;

    public proposals: SuggestionInterface[] = <SuggestionInterface[]>[{ suggestion: "(" }];

    public canBeUsed(parser: QueryParser): boolean {
        parser.skipSpaces();
        return parser.peekChar() == '(';
    }
    public extract(parser: QueryParser): Token {
        parser.skipSpaces();
        var token = new TokenOpenParenthesis
        token.value = parser.nextChar();
        return token;
    }

    public getProposals(nextToken?: Token, afterNextToken?: Token): Token[] {
        if (typeof nextToken != 'undefined' && typeof afterNextToken != 'undefined') {
            switch (nextToken.tokenType) {
                case TokenType.TokenStar:
                    return [new TokenCloseParenthesis()];
                case TokenType.TokenName:
                    return [new TokenCloseParenthesis()];
                case TokenType.TokenOpenParenthesis:
                    return [new TokenName(), new TokenOpenParenthesis()];
                default:
                    return [];
            }
        } else if (typeof nextToken != 'undefined') {
            return [new TokenName(), new TokenOpenParenthesis()];
        } else {
            return [new TokenOpenParenthesis()];
        }
    }
}