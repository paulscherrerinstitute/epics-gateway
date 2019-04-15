class QueryParser
{
    public source: string;
    public position: number;
    public tokens: Tokenizer;

    constructor(source: string)
    {
        this.source = source;
        this.position = 0;
        this.tokens = new Tokenizer(this);
    }

    public peekString(): string
    {
        var result = "";
        for (var i = this.position; i < this.source.length; i++)
        {
            var c = this.source[i];
            if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
                result += c;
            else
                break;
        }
        return result;
    }

    public nextString(): string
    {
        var result = "";
        while (this.position < this.source.length)
        {
            var c = this.source[this.position];
            if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
                result += c;
            else
                break;
            this.position++;
        }
        return result;
    }

    public skipSpaces(): void
    {
        while (this.peekChar() == ' ' || this.peekChar() == '\t' || this.peekChar() == '\n' || this.peekChar() == '\r')
        {
            this.nextChar();
        }
    }

    public peekChar(offset: number = 0): string
    {
        return this.source[this.position + offset];
    }

    public nextChar(): string
    {
        return this.source[this.position++];
    }

    public hasChar()
    {
        return this.position < this.source.length;
    }

    public static getProposals(query: string): SuggestionInterface[]
    {
        return new QueryParser(query).getProposals(query);
    }

    private getProposals(query: string): SuggestionInterface[]
    {
        let length = this.tokens.tokens.length;
        let possibleTokens: Token[];
        let suggestions: SuggestionInterface[] = [];
        let input = length <= 0 ? "" : this.tokens.tokens[length - 1].value.toLowerCase();
        if (length <= 1)
            possibleTokens = new TokenEnd().getProposals();
        else if (length == 2)
            possibleTokens = this.tokens.tokens[0].getProposals(this.tokens.tokens[1]);
        else
            possibleTokens = this.tokens.tokens[length - 3].getProposals(this.tokens.tokens[length - 2], this.tokens.tokens[length - 1]);

        if (query.split("(").length - 1 <= query.split(")").length - 1)
            possibleTokens = possibleTokens.filter(token => token.tokenType != TokenType.TokenCloseParenthesis);

        possibleTokens.forEach(token =>
        {
            suggestions = suggestions.concat(token.proposals.filter(prop => prop.suggestion.indexOf(input) > -1));
        });

        suggestions.forEach(suggestion =>
        {
            suggestion.input = input;
        });

        return suggestions;
    }

}