/**
 * DTO of GWLogger.Backend.DTOs.Connections
 */
class Connections
{
    Clients: Connection[];
    Servers: Connection[];

    constructor(_clients: Connection[],
        _servers: Connection[])
    {
        this.Clients = _clients;
        this.Servers = _servers;
    }

    public static CreateFromObject(obj: any): Connections
    {
        if (!obj)
            return null;
        return new Connections((obj.Clients ? obj.Clients.map(function (c) { return Connection.CreateFromObject(c); }) : null),
            (obj.Servers ? obj.Servers.map(function (c) { return Connection.CreateFromObject(c); }) : null));
    }
}

/**
 * DTO of GWLogger.Backend.DTOs.Connection
 */
class Connection
{
    RemoteIpPoint: string;
    Start: Date;
    End: Date;

    constructor(_remoteippoint: string,
        _start: string,
        _end: string)
    {
        this.RemoteIpPoint = _remoteippoint;
        this.Start = (_start ? new Date(parseInt(_start.substr(6, _start.length - 8))) : null);
        this.End = (_end ? new Date(parseInt(_end.substr(6, _end.length - 8))) : null);
    }

    public static CreateFromObject(obj: any): Connection
    {
        if (!obj)
            return null;
        return new Connection(obj.RemoteIpPoint,
            obj.Start,
            obj.End);
    }
}