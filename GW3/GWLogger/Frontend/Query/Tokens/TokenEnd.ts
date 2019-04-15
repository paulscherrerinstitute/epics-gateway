class TokenEnd extends Token
{

    public proposals: SuggestionInterface[] = [];

    public tokenType: TokenType = TokenType.TokenEnd;

    constructor(value: string = "")
    {
        super();
        this.value = value;
    }

    public canBeUsed(parser: QueryParser): boolean
    {
        return true;
    }

    public extract(parser: QueryParser): Token
    {
        return new TokenEnd();
    }

    public getProposals(nextToken?: Token, afterNextToken?: Token): Token[]
    {
        return [
            new TokenName(),
            new TokenSelect()
        ];
    }
}