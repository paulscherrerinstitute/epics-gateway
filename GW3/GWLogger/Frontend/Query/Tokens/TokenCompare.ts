class TokenCompare extends Token {
    static keywords: string[] = ["contains", "starts", "ends"];
    public canBeUsed(parser: QueryParser): boolean {
        parser.skipSpaces();
        return ((parser.peekChar() == '=') ||
            parser.peekChar() == '<' ||
            parser.peekChar() == '>' ||
            (parser.peekChar() == '!' && parser.peekChar(1) == '=') || TokenCompare.keywords.indexOf(parser.peekString().toLowerCase()) > -1);
    }

    public extract(parser: QueryParser): Token {
        parser.skipSpaces();
        var token = new TokenCompare()
        if (TokenCompare.keywords.indexOf(parser.peekString().toLowerCase()) > -1) {
            token.value = parser.nextString().toLowerCase()
            return token;
        }
        if ((parser.peekChar() == '<' || parser.peekChar() == '>' || parser.peekChar() == '=') && parser.peekChar(1) != '=') {
            token.value = parser.nextChar();
            return token;
        }
        token.value = parser.nextChar() + parser.nextChar();
        return token;
    }
}