abstract class Token {
    public value: string;
    public abstract proposals: SuggestionInterface[];
    public abstract tokenType: TokenType;
    public abstract canBeUsed(parser: QueryParser): boolean;
    public abstract extract(parser: QueryParser): Token;
    public abstract getProposals(nextToken?: Token, afterNextToken?: Token): Token[];
}