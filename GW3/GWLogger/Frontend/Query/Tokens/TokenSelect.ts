class TokenSelect extends Token {

    public tokenType: TokenType = TokenType.TokenSelect;

    public proposals: SuggestionInterface[] = <SuggestionInterface[]>[{ suggestion: "select" }];

    public canBeUsed(parser: QueryParser): boolean {
        parser.skipSpaces();
        return parser.peekString().toLowerCase() == "select";
    }
    public extract(parser: QueryParser): Token {
        parser.skipSpaces();
        parser.nextString();
        let token = new TokenSelect();
        token.value = "select";
        return token;
    }

    public getProposals(nextToken?: Token, afterNextToken?: Token): Token[] {
        if (typeof nextToken != 'undefined' && typeof afterNextToken != 'undefined') {
            switch (nextToken.tokenType) {
                case TokenType.TokenStar:
                    return [
                        new TokenComa(),
                        new TokenGroup(),
                        new TokenLimit(),
                        new TokenOrder(),
                        new TokenWhere()
                    ];
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
                case TokenType.TokenOperation:
                    return [new TokenOpenParenthesis()];
                default:
                    return [];
            }
        } else if (typeof nextToken != 'undefined') {
            return [new TokenStar(), new TokenName(), new TokenOperation()];
        } else {
            return [new TokenSelect()];
        }
    }
}
