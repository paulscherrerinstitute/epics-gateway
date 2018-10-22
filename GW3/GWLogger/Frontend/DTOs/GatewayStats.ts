/**
 * DTO of GWLogger.Backend.DTOs.GatewayStats
 */
class GatewayStats
{
    Logs: LogStat[];
    Searches: LogStat[];
    Errors: LogStat[];
    CPU: LogStat[];
    PVs: LogStat[];
    Clients: LogStat[];
    Servers: LogStat[];

    constructor(_logs: LogStat[],
        _searches: LogStat[],
        _errors: LogStat[],
        _cpu: LogStat[],
        _pvs: LogStat[],
        _clients: LogStat[],
        _servers: LogStat[]
    )
    {
        this.Logs = _logs;
        this.Searches = _searches;
        this.Errors = _errors;
        this.CPU = _cpu;
        this.PVs = _pvs;
        this.Clients = _clients;
        this.Servers = _servers;
    }

    public static CreateFromObject(obj: any): GatewayStats
    {
        if (!obj)
            return null;
        return new GatewayStats((obj.Logs ? obj.Logs.map(function (c) { return LogStat.CreateFromObject(c); }) : null),
            (obj.Searches ? obj.Searches.map(function (c) { return LogStat.CreateFromObject(c); }) : null),
            (obj.Errors ? obj.Errors.map(function (c) { return LogStat.CreateFromObject(c); }) : null),
            (obj.CPU ? obj.CPU.map(function (c) { return LogStat.CreateFromObject(c); }) : null),
            (obj.PVs ? obj.PVs.map(function (c) { return LogStat.CreateFromObject(c); }) : null),
            (obj.Clients ? obj.Clients.map(function (c) { return LogStat.CreateFromObject(c); }) : null),
            (obj.Servers ? obj.Servers.map(function (c) { return LogStat.CreateFromObject(c); }) : null));
    }
}

/**
 * DTO of GWLogger.Backend.DTOs.LogStat
 */
class LogStat
{
    Date: Date;
    Value: number;

    constructor(_date: string,
        _value: number)
    {
        this.Date = (_date ? new Date(parseInt(_date.substr(6, _date.length - 8))) : null);
        this.Value = _value;
    }

    public static CreateFromObject(obj: any): LogStat
    {
        if (!obj)
            return null;
        return new LogStat(obj.Date,
            obj.Value);
    }
}