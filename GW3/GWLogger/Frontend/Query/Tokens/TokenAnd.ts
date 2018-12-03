class TokenAnd extends Token {

    public canBeUsed(parser: QueryParser): boolean {
        parser.skipSpaces();
        return ((parser.peekChar() == '&' && parser.peekChar(1) == '&') || parser.peekString().toLowerCase() == "and");
    }

    public extract(parser: QueryParser): Token {
        parser.skipSpaces();
        var token = new TokenAnd;
        if (parser.peekString().toLowerCase() == "and") {
            parser.nextString();
            token.value = "&&";
        } else {
            token.value = parser.nextChar() + parser.nextChar();
        }
        return token;
    }
}