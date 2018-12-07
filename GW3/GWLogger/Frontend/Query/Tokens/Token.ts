abstract class Token {
    public value: string;
    public abstract canBeUsed(parser: QueryParser): boolean;
    public abstract extract(parser: QueryParser): Token;
}