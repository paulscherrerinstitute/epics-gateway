class TokenString extends Token {
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
}