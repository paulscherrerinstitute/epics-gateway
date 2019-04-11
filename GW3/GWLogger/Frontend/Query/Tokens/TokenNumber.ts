class TokenNumber extends Token {


    public proposals: SuggestionInterface[] = [];
    public tokenType: TokenType = TokenType.TokenNumber;
    private allowedChar: string = "0123456789";

    public canBeUsed(parser: QueryParser): boolean {
        parser.skipSpaces();
        if (parser.peekChar() == '.' && this.allowedChar.indexOf(parser.peekChar(1)) != -1)
            return true;
        return this.allowedChar.indexOf(parser.peekChar()) != -1 && parser.peekChar() != '.';
    }

    public extract(parser: QueryParser): Token {
        var extracted = "";
        parser.skipSpaces();

        while (parser.hasChar()) {
            if (this.allowedChar.indexOf(parser.peekChar()) == -1 && parser.peekChar() != '.')
                break;
            extracted += parser.nextChar();
        }
        var token = new TokenNumber();
        token.value = extracted;
        return token;
    }

    public getProposals(nextToken?: Token, afterNextToken?: Token): Token[] {
        if (typeof nextToken != 'undefined' && typeof afterNextToken != 'undefined') {
            switch (nextToken.tokenType) {
                case TokenType.TokenAnd:
                case TokenType.TokenOr:
                case TokenType.TokenWhere:
                    return [new TokenName(), new TokenOpenParenthesis()];
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
                        new TokenName(),
                        new TokenCloseParenthesis()
                    ];
            }
        }
        return [];
    }
}