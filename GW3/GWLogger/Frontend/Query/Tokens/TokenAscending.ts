class TokenAscending extends Token {

    public tokenType: TokenType = TokenType.TokenAscending;

    public proposals: SuggestionInterface[] = <SuggestionInterface[]>[{ suggestion: "asc" }];

    public canBeUsed(parser: QueryParser): boolean {
        parser.skipSpaces();
        return parser.peekString().toLowerCase() == 'asc';
    }

    public extract(parser: QueryParser): Token {
        parser.skipSpaces();
        parser.nextString();
        let token = new TokenAscending();
        token.value = "asc";
        return token;
    }

    public getProposals(nextToken?: Token, afterNextToken?: Token): Token[] {
        if (typeof nextToken != 'undefined' && typeof afterNextToken != 'undefined') {
            switch (nextToken.tokenType) {
                case TokenType.TokenGroup:
                case TokenType.TokenOrder:
                    return [new TokenName()];
                case TokenType.TokenLimit:
                    return [new TokenNumber()];
                case TokenType.TokenWhere:
                    return [new TokenName(), new TokenOpenParenthesis()];
            }
        }
        return [];
    }
}
