class TokenString extends Token {

    //public proposals: SuggestionInterface[] = <SuggestionInterface[]>[{ suggestion: "\"\"" }];
    public proposals: SuggestionInterface[] = [];

    public tokenType: TokenType = TokenType.TokenString;

    public canBeUsed(parser: QueryParser): boolean {
        parser.skipSpaces();
        return (parser.peekChar() == '\"' || parser.peekChar() == '\'');
    }
    public extract(parser: QueryParser): Token {
        var extracted = "" + parser.nextChar();
        while (parser.hasChar()) {
            var c = parser.nextChar();
            extracted += c;
            if (c == extracted[0])
                break;
        }
        var token = new TokenString();
        token.value = extracted.substr(1, extracted.length - 2);
        return token;
    }

    public getProposals(nextToken?: Token, afterNextToken?: Token): Token[] {
        if (typeof nextToken != 'undefined' && typeof afterNextToken != 'undefined') {
            switch (nextToken.tokenType) {
                case TokenType.TokenAnd:
                case TokenType.TokenOr:
                case TokenType.TokenWhere:
                    return [new TokenName(), new TokenOpenParenthesis()];
                case TokenType.TokenComa:
                    return [new TokenName(), new TokenStar()];
                case TokenType.TokenGroup:
                case TokenType.TokenOrder:
                    return [new TokenName()];
                case TokenType.TokenLimit:
                    return [new TokenNumber()];
                case TokenType.TokenCloseParenthesis:
                    return [
                        new TokenAnd(),
                        new TokenGroup(),
                        new TokenLimit(),
                        new TokenOr(),
                        new TokenOrder(),
                        new TokenWhere(),
                        new TokenCloseParenthesis()
                    ];
            }
        }
        return [];
    }
}