class TokenNumber extends Token {
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
}