class TokenOr extends Token {
    public canBeUsed(parser: QueryParser): boolean {
        parser.skipSpaces();
        return ((parser.peekChar() == '|' && parser.peekChar(1) == '|') || parser.peekString().toLowerCase() == "or");
    }
    public extract(parser: QueryParser): Token {
        parser.skipSpaces();
        var token = new TokenOr();
        if (parser.peekString().toLowerCase() == "or") {
            parser.nextString();
            token.value = "||";
            return token;
        }
        token.value = parser.nextChar() + parser.nextChar();
        return token;
    }
}