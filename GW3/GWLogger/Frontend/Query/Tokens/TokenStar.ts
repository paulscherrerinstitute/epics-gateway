class TokenStar extends Token {

    public tokenType: TokenType = TokenType.TokenStar;
    public proposals: SuggestionInterface[] = <SuggestionInterface[]>[{ suggestion: "*" }];

    public canBeUsed(parser: QueryParser): boolean {
        parser.skipSpaces();
        return parser.peekChar() == '*';
    }

    public extract(parser: QueryParser): Token {
        parser.skipSpaces();
        parser.nextChar();
        let token = new TokenStar();
        token.value = "*";
        return token;
    }

    public getProposals(nextToken?: Token, afterNextToken?: Token): Token[] {
        if (typeof nextToken != 'undefined' && typeof afterNextToken != 'undefined') {
            switch (nextToken.tokenType) {
                case TokenType.TokenComa:
                case TokenType.TokenGroup:
                case TokenType.TokenOrder:
                    return [new TokenName()];
                case TokenType.TokenLimit:
                    return [new TokenNumber()];
                case TokenType.TokenWhere:
                    return [new TokenName(), new TokenOpenParenthesis()];
                case TokenType.TokenCloseParenthesis:
                    return [
                        new TokenComa(),
                        new TokenGroup(),
                        new TokenLimit(),
                        new TokenOrder(),
                        new TokenWhere(),
                        new TokenString(),
                        new TokenName()
                    ];
            }
        }
        return [];
    }
}
