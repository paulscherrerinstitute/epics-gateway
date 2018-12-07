class TokenEnd extends Token {

    constructor(value: string = "") {
        super();
        this.value = value;
    }
    public canBeUsed(parser: QueryParser): boolean {
        return true;
    }
    public extract(parser: QueryParser): Token {
        return new TokenEnd();
    }
}