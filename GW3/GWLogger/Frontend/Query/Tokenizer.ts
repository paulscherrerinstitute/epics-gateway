class Tokenizer {
    public tokens: Token[] = [];
    public position: number = 0;
    public knownTokens: Token[] = [
        new TokenAnd(),
        new TokenCloseParenthesis(),
        new TokenCompare(),
        new TokenName(),
        new TokenNumber(),
        new TokenOpenParenthesis(),
        new TokenOr(),
        new TokenString()
    ];

    constructor(parser: QueryParser) {
        while (parser.hasChar()) {
            parser.skipSpaces();
            var possibleToken = this.knownTokens.filter(token => token.canBeUsed(parser))[0];
            if (typeof (possibleToken) != 'undefined' && possibleToken != null) {
                this.tokens.push(possibleToken.extract(parser));
            } else {
                if (typeof (parser.peekChar()) != "undefined") {
                    this.tokens.push(new TokenEnd(parser.peekChar()));
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