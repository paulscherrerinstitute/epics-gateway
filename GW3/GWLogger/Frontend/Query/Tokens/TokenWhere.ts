class TokenWhere extends Token {

    public tokenType: TokenType = TokenType.TokenWhere;

    public proposals: SuggestionInterface[] = <SuggestionInterface[]>[{ suggestion: "where" }];

    public canBeUsed(parser: QueryParser): boolean {
        parser.skipSpaces();
        return parser.peekString().toLowerCase() == "where";
    }
    public extract(parser: QueryParser): Token {
        parser.skipSpaces();
        parser.nextString();
        let token = new TokenWhere();
        token.value = "where";
        return token;
    }

    public getProposals(nextToken?: Token, afterNextToken?: Token): Token[] {
        if (typeof nextToken != 'undefined' && typeof afterNextToken != 'undefined') {
            switch (nextToken.tokenType) {
                case TokenType.TokenName:
                    return [new TokenCompare()];
                case TokenType.TokenOpenParenthesis:
                    return [new TokenName(), new TokenOpenParenthesis()];
            }
        }
        return [];
    }
}
