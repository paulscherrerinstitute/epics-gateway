class TokenOpenParenthesis extends Token {
    public canBeUsed(parser: QueryParser): boolean {
        parser.skipSpaces();
        return parser.peekChar() == '(';
    }
    public extract(parser: QueryParser): Token {
        parser.skipSpaces();
        var token = new TokenOpenParenthesis
        token.value = parser.nextChar();
        return token;
    }
}