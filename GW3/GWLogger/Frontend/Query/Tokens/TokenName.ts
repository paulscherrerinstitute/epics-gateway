class TokenName extends Token
{
    public proposals: SuggestionInterface[] = <SuggestionInterface[]>[
        { suggestion: "class", hint: "Source class, path & function call", dataType: "Text" },
        { suggestion: "sourcefilepath", hint: "Source file path", dataType: "Text" },
        { suggestion: "sourcemembername", hint: "Function call", dataType: "Text" },
        { suggestion: "line", hint: "Source line number", dataType: "Number" },
        { suggestion: "sourcelinenumber", hint: "Source line number", dataType: "Number" },
        { suggestion: "channel", hint: "EPICS Channel Name", dataType: "Text" },
        { suggestion: "channelname", hint: "EPICS Channel Name", dataType: "Text" },
        { suggestion: "sid", hint: "Server ID (IOC ID)", dataType: "Text" },
        { suggestion: "cid", hint: "Client ID (EPICS client)", dataType: "Text" },
        { suggestion: "gwid", hint: "Gateway ID", dataType: "Text" },
        { suggestion: "remote", hint: "Remote IP & port (either client or IOC)", dataType: "Text" },
        { suggestion: "cmd", hint: "Command id", dataType: "Text" },
        { suggestion: "commandid", hint: "Command id", dataType: "Text" },
        { suggestion: "ip", hint: "3rd party IP (<> remote)", dataType: "Text" },
        { suggestion: "exception", hint: "Exception", dataType: "Text" },
        { suggestion: "datacount", hint: "Channel data count", dataType: "Number" },
        { suggestion: "gatewaymonitorid", hint: "Gateway Monitor Id", dataType: "Text" },
        { suggestion: "clientioid", hint: "Client I/O ID", dataType: "Text" },
        { suggestion: "version", hint: "Channel Access protocol's version", dataType: "Text" },
        { suggestion: "origin", hint: "Origin", dataType: "Text" },
        { suggestion: "type", hint: "Log message type", dataType: "Text" },
        { suggestion: "date", hint: "Entry date", dataType: "Date" },
        { suggestion: "reason", hint: "Reason of disconnection", dataType: "Text" },
        { suggestion: "message", hint: "Additional message attached to an event", dataType: "Text" },
        { suggestion: "hostname", hint: "Hostname part of the remote IP", dataType: "Text" },
        { suggestion: "select", hint: "Defines which fields will be selected", dataType: "Fields" },
        { suggestion: "where", hint: "Defines a filter rule", dataType: "Condition" },
        { suggestion: "group", hint: "Defines a grouping rule", dataType: "Field" },
        { suggestion: "order", hint: "Defines a order rule", dataType: "Field" },
        { suggestion: "limit", hint: "Defines a number of rows to be returned", dataType: "Number" },
    ];
    public tokenType: TokenType = TokenType.TokenName;
    private allowedChar: string = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_";
    private secondChar: string = "0123456789";
    static keywords: string[] = ["and", "or", "contains", "starts", "ends", "select", "where", "group", "order", "asc", "desc", "limit"];

    public canBeUsed(parser: QueryParser): boolean
    {
        parser.skipSpaces();
        return this.allowedChar.indexOf(parser.peekChar()) != -1 && TokenName.keywords.indexOf(parser.peekString().toLowerCase()) < 0;
    }

    public extract(parser: QueryParser): Token
    {
        var extracted = "";
        parser.skipSpaces();

        while (parser.hasChar())
        {
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

    public getProposals(nextToken?: Token, afterNextToken?: Token): Token[]
    {
        if (typeof nextToken != 'undefined' && typeof afterNextToken != 'undefined')
        {
            switch (nextToken.tokenType)
            {
                case TokenType.TokenAnd:
                case TokenType.TokenOr:
                case TokenType.TokenWhere:
                    return [new TokenName(), new TokenOpenParenthesis()];
                case TokenType.TokenAscending:
                case TokenType.TokenDescending:
                    return [new TokenGroup(), new TokenLimit(), new TokenOrder(), new TokenWhere()];
                case TokenType.TokenString:
                case TokenType.TokenName:
                    return [new TokenComa(), new TokenGroup(), new TokenLimit(), new TokenOrder(), new TokenWhere()];
                case TokenType.TokenComa:
                    return [new TokenName(), new TokenStar(), new TokenOperation()];
                case TokenType.TokenOpenParenthesis:
                    return [new TokenName(), new TokenStar()];
                case TokenType.TokenCompare:
                    return [];
                case TokenType.TokenGroup:
                case TokenType.TokenOrder:
                    return [new TokenName()];
                case TokenType.TokenLimit:
                    return [new TokenNumber()];
                case TokenType.TokenCloseParenthesis:
                    return [
                        new TokenComa(),
                        new TokenGroup(),
                        new TokenLimit(),
                        new TokenOrder(),
                        new TokenWhere(),
                        new TokenString(),
                        new TokenCloseParenthesis()
                    ];
                default:
                    return [];
            }
        }
        else if (typeof nextToken != 'undefined')
            return [new TokenCompare()];
        else
            return [new TokenName()];
    }
}