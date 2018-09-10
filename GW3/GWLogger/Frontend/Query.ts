var availableVariables = {
    "class": "Source class, path & function call",
    "line": "Source line number",
    "channel": "EPICS Channel Name",
    "sid": "Server ID (IOC ID)",
    "cid": "Client ID (EPICS client)",
    "gwid": "Gateway ID",
    "remote": "Remote IP & port (either client or IOC)",
    "cmd": "Command id",
    "ip": "3rd party IP (&lt;&gt; remote)",
    "exception": "Exception string",
    "datacount": "Channel data count",
    "gatewaymonitorid": "Gateway Monitor Id",
    "clientioid": "Client I/O ID",
    "version": "Channel Access protocol's version",
    "origin": "Origin",
    "type": "Log message type"
};

var availableConditions = {
    "!=": "Not equal",
    "=": "Equal",
    "&gt;": "Bigger than",
    "&lt;": "Smaller than",
    "&gt;=": "Bigger or equal than",
    "&lt;=": "Smaller or equal than",
    "contains": "Contains",
    "starts": "Starts with",
    "ends": "Ends with"
};

var availableOperators = [{ "and": "and" }, { "or": "or" }];