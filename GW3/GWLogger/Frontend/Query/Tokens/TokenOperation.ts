class TokenOperation extends Token {

    public tokenType: TokenType = TokenType.TokenOperation;

    public proposals: SuggestionInterface[] = <SuggestionInterface[]>[
        { suggestion: "min", hint: "Smallest value" },
        { suggestion: "max", hint: "Biggest value" },
        { suggestion: "count", hint: "Count" },
        { suggestion: "sum", hint: "Sum" },
        {suggestion: "average", hint: "Average"},
        { suggestion: "avg", hint: "Average (Alias)" },
    ];

    static keywords: string[] = ["min", "max", "count", "avg", "sum", "average"];

    public canBeUsed(parser: QueryParser): boolean {
        parser.skipSpaces();
        return TokenOperation.keywords.indexOf(parser.peekString().toLowerCase()) > -1;
    }

    public extract(parser: QueryParser): Token {
        parser.skipSpaces();
        var token = new TokenOperation()
        token.value = parser.nextString().toLowerCase()
        return token;
    }

    public getProposals(nextToken?: Token, afterNextToken?: Token): Token[] {
        if (typeof nextToken != 'undefined' && typeof afterNextToken != 'undefined') {
            switch (nextToken.tokenType) {
                case TokenType.TokenOpenParenthesis:
                    return [new TokenName(), new TokenStar()];
            }
        }
        return [];
    }
}