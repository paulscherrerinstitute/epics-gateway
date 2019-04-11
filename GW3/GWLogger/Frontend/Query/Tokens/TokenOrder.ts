class TokenOrder extends Token {

    public tokenType: TokenType = TokenType.TokenOrder;

    public proposals: SuggestionInterface[] = <SuggestionInterface[]>[{ suggestion: "order by" }];

    public canBeUsed(parser: QueryParser): boolean {
        parser.skipSpaces();
        return parser.peekString().toLowerCase() == "order";
    }
    public extract(parser: QueryParser): Token {
        parser.skipSpaces();
        parser.nextString();
        parser.skipSpaces();
        let token = new TokenOrder();
        token.value = "limit";
        if (parser.peekString().toLowerCase() == "by") {
            token.value += " " + parser.nextString();
        }
        return token;
    }

    public getProposals(nextToken?: Token, afterNextToken?: Token): Token[] {
        if (typeof nextToken != 'undefined' && typeof afterNextToken != 'undefined') {
            switch (nextToken.tokenType) {
                case TokenType.TokenName:
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
