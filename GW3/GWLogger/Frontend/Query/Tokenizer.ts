class Tokenizer {
    public tokens: Token[] = [];
    public position: number = 0;
    public knownTokens: Token[] = [
        new TokenAnd(),
        new TokenAscending(),
        new TokenComa(),
        new TokenCompare(),
        new TokenOperation(),
        new TokenDescending(),
        new TokenGroup(),
        new TokenLimit(),
        new TokenSelect(),
        new TokenOr(),
        new TokenOrder(),
        new TokenStar(),
        new TokenWhere(),
        new TokenString(),
        new TokenName(),
        new TokenNumber(),
        new TokenOpenParenthesis(),
        new TokenCloseParenthesis()
    ];

    constructor(parser: QueryParser) {
        while (parser.hasChar()) {
            parser.skipSpaces();
            var possibleToken = this.knownTokens.filter(token => token.canBeUsed(parser))[0];
            if (typeof (possibleToken) != 'undefined' && possibleToken != null) {
                this.tokens.push(possibleToken.extract(parser));
            } else {
                if (typeof (parser.peekChar()) != "undefined") {
                    this.tokens.push(new TokenEnd(parser.peekString()));
                } else {
                    this.tokens.push(new TokenEnd());
                }
                break;
            }
        }
    }

    public hasToken(): boolean {
        return this.position < this.tokens.length;
    }

    public next(): Token {
        return this.tokens[this.position++];
    }

    public peek(offset: number = 0): Token {
        return this.tokens[this.position + offset];
    }
}