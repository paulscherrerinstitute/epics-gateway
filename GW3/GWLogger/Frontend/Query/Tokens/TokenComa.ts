class TokenComa extends Token {

    public tokenType: TokenType = TokenType.TokenComa;

    //public proposals: SuggestionInterface[] = <SuggestionInterface[]>[{ suggestion: "," }];
    public proposals: SuggestionInterface[] = []

    public canBeUsed(parser: QueryParser): boolean {
        parser.skipSpaces();
        return parser.peekChar() == ',';
    }

    public extract(parser: QueryParser): Token {
        parser.skipSpaces();
        parser.nextChar();
        let token = new TokenComa();
        token.value = ",";
        return token;
    }

    public getProposals(nextToken?: Token, afterNextToken?: Token): Token[] {
        if (typeof nextToken != 'undefined' && typeof afterNextToken != 'undefined') {
            switch (nextToken.tokenType) {
                case TokenType.TokenOperation:
                    return [new TokenOpenParenthesis()];
                case TokenType.TokenName:
                    return [
                        new TokenComa(),
                        new TokenGroup(),
                        new TokenLimit(),
                        new TokenOrder(),
                        new TokenWhere(),
                        new TokenString(),
                        new TokenName()
                    ];
                case TokenType.TokenStar:
                    return [
                        new TokenGroup(),
                        new TokenLimit(),
                        new TokenOrder(),
                        new TokenWhere()
                    ];
            }
        }
        return [];
    }
}