class TokenName extends Token {
    private allowedChar: string = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_";
    private secondChar: string = "0123456789";
    static keywords: string[] = ["and", "or", "contains", "starts", "ends"];

    public canBeUsed(parser: QueryParser): boolean {
        parser.skipSpaces();
        return this.allowedChar.indexOf(parser.peekChar()) != -1 && TokenName.keywords.indexOf(parser.peekString().toLowerCase()) < 0;
    }
    public extract(parser: QueryParser): Token {
        var extracted = "";
        parser.skipSpaces();

        while (parser.hasChar()) {
            if (extracted.length > 0 && this.allowedChar.indexOf(parser.peekChar()) < 0 && this.secondChar.indexOf(parser.peekChar()) < 0)
                break;
            else if (extracted.length == 0 && this.allowedChar.indexOf(parser.peekChar()) < 0)
                break;
            extracted += parser.nextChar();
        }

        var token = new TokenName();
        token.value = extracted;
        return token;
    }
}