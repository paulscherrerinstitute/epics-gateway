class QueryParser {
    public source: string;
    public position: number;
    public tokens: Tokenizer;

    constructor(source: string) {
        this.source = source;
        this.position = 0;
        this.tokens = new Tokenizer(this);
    }

    public peekString(): string {
        var result = "";
        for (var i = this.position; i < this.source.length; i++) {
            var c = this.source[i];
            if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
                result += c;
            else
                break;
        }
        return result;
    }

    public nextString(): string {
        var result = "";
        while (this.position < this.source.length) {
            var c = this.source[this.position];
            if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
                result += c;
            else
                break;
            this.position++;
        }
        return result;
    }

    public skipSpaces(): void {
        while (this.peekChar() == ' ' || this.peekChar() == '\t' || this.peekChar() == '\n' || this.peekChar() == '\r') {
            this.nextChar();
        }
    }

    public peekChar(offset: number = 0): string {
        return this.source[this.position + offset];
    }

    public nextChar(): string {
        return this.source[this.position++];
    }

    public hasChar() {
        return this.position < this.source.length;
    }

    public static getProposals(query: string): SuggestionInterface[] {
        return new QueryParser(query).getProposals();
    }

    private getProposals(): SuggestionInterface[] {
        var length = this.tokens.tokens.length;
        if (length < 1) {
            return this.getVariables();
        }
        var lastToken = this.tokens.tokens[length - 1];
        var secondLastTokenType = typeof (this.tokens.tokens[length - 2]) == 'undefined' ? 'undefined' : (<any>this.tokens.tokens[length - 2].constructor).name;
        var thirdLastTokenType = typeof (this.tokens.tokens[length - 3]) == 'undefined' ? 'undefined' : (<any>this.tokens.tokens[length - 3].constructor).name;
        switch ((<any>lastToken.constructor).name) {
            case "TokenEnd":
                switch (lastToken.value) {
                    case "":
                        switch (secondLastTokenType) {
                            case "TokenAnd":
                            case "TokenOr":
                            case "TokenOpenParenthesis":
                                return this.getVariables();
                            case "TokenName":
                                if (thirdLastTokenType == "TokenCompare") {
                                    return this.getBinaries();
                                }
                                return this.getOperators(lastToken.value);
                            case "TokenNumber":
                            case "TokenString":
                            case "TokenCloseParenthesis":
                                return this.getBinaries();
                            default:
                                return [];
                        }
                    case "&":
                        return this.getBinaries("&");
                    case "|":
                        return this.getBinaries("|");
                    default:
                        if (lastToken.value.length == 1) {
                            return this.getOperators(lastToken.value);
                        }
                }
                return [];
            case "TokenName":
                switch (secondLastTokenType) {
                    case "TokenString":
                    case "TokenNumber":
                    case "TokenName":
                        switch (thirdLastTokenType) {
                            case "TokenCompare":
                                return this.getBinaries();
                            default:
                                return this.getOperators(lastToken.value);
                        }
                    case "TokenCompare":
                        break;
                    default:
                        return this.getVariables(lastToken.value);
                }
                break;
            case "TokenOpenParenthesis":
                return this.getVariables();
            case "TokenCompare":
                if (lastToken.value.length == 1) {
                    return this.getOperators(lastToken.value);
                }
            default:
                if ((this.source.match(/\"/g) || []).length % 2 != 0) {
                    return [<SuggestionInterface>{ suggestion: '"' }];
                }
                if ((this.source.match(/\'/g) || []).length % 2 != 0) {
                    return [<SuggestionInterface>{ suggestion: "'" }];
                }
                if ((this.source.match(/\(/g) || []).length > (this.source.match(/\)/g) || []).length) {
                    return [<SuggestionInterface>{ suggestion: ")" }];
                }
                return [];
        }
    }

    private getVariables(key: string = ""): SuggestionInterface[] {
        key = key.toLowerCase();
        return [
            ["class", "Source class, path & function call", "Text"],
            ["sourcefilepath", "Source file path", "Text"],
            ["sourcemembername", "Function call", "Text"],
            ["line", "Source line number", "Number"],
            ["sourcelinenumber", "Source line number", "Number"],
            ["channel", "EPICS Channel Name", "Text"],
            ["channelname", "EPICS Channel Name", "Text"],
            ["sid", "Server ID (IOC ID)", "Text"],
            ["cid", "Client ID (EPICS client)", "Text"],
            ["gwid", "Gateway ID", "Text"],
            ["remote", "Remote IP & port (either client or IOC)", "Text"],
            ["cmd", "Command id", "Text"],
            ["commandid", "Command id", "Text"],
            ["ip", "3rd party IP (<> remote)", "Text"],
            ["exception", "Exception", "Text"],
            ["datacount", "Channel data count", "Number"],
            ["gatewaymonitorid", "Gateway Monitor Id", "Text"],
            ["clientioid", "Client I/O ID", "Text"],
            ["version", "Channel Access protocol's version", "Text"],
            ["origin", "Origin", "Text"],
            ["type", "Log message type", "Text"],
            ["date", "Entry date", "Date"]
        ]
            .filter(variable => variable[0].indexOf(key) != -1)
            .map(variable => <SuggestionInterface>{ suggestion: variable[0], hint: variable[1], dataType: variable[2], input: key });
    }

    private getOperators(key: string = ""): SuggestionInterface[] {
        key = key.toLowerCase();
        return [
            ["!=", "Not equal"],
            ["=", "Equal"],
            [">", "Bigger than"],
            ["<", "Smaller than"],
            [">=", "Bigger or equal than"],
            ["<=", "Smaller or equal than"],
            ["contains", "Contains"],
            ["starts", "Starts with"],
            ["ends", "Ends with"]
        ]
            .filter(operator => operator[0].indexOf(key) != -1)
            .map(operator => <SuggestionInterface>{ suggestion: operator[0], hint: operator[1], input: key });
    }

    private getBinaries(key: string = ""): SuggestionInterface[] {
        key = key.toLowerCase();
        return ["and", "or", "&&", "||"]
            .filter(operator => operator.indexOf(key) != -1)
            .map(operator => <SuggestionInterface>{ suggestion: operator, input: key });
    }
}