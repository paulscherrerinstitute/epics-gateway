class TokenLimit extends Token {

    public tokenType: TokenType = TokenType.TokenLimit;

    public proposals: SuggestionInterface[] = <SuggestionInterface[]>[{ suggestion: "limit" }];

    public canBeUsed(parser: QueryParser): boolean {
        parser.skipSpaces();
        return parser.peekString().toLowerCase() == "limit";
    }
    public extract(parser: QueryParser): Token {
        parser.skipSpaces();
        parser.nextString();
        parser.skipSpaces();
        let token = new TokenLimit();
        token.value = "limit";
        return token;
    }

    public getProposals(nextToken?: Token, afterNextToken?: Token): Token[] {
        if (typeof nextToken != 'undefined' && typeof afterNextToken != 'undefined') {
            switch (nextToken.tokenType) {
                case TokenType.TokenNumber:
                    return [
                        new TokenAscending(),
                        new TokenDescending(),
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
