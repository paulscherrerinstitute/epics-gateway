/**
 * DTO of GWLogger.Backend.DTOs.GatewaySession
 */
enum RestartType
{
    Unknown = 0,
    WatchdogNoResponse = 1,
    WatchdogCPULimit = 2,
    GatewayUpdate = 3,
    ManualRestart = 4,
}

class GatewaySession
{
    EndDate: Date;
    StartDate: Date;
    NbEntries: number;
    Description: string;
    RestartType: RestartType;

    constructor(_enddate: string,
        _startdate: string,
        _nbentries: number,
        _description: string,
        _restarttype: RestartType)
    {
        this.EndDate = (_enddate ? new Date(parseInt(_enddate.substr(6, _enddate.length - 8))) : null);
        this.StartDate = (_startdate ? new Date(parseInt(_startdate.substr(6, _startdate.length - 8))) : null);
        this.NbEntries = _nbentries;
        this.Description = _description;
        this.RestartType = _restarttype;
    }

    public static CreateFromObject(obj: any): GatewaySession
    {
        if (!obj)
            return null;
        return new GatewaySession(obj.EndDate,
            obj.StartDate,
            obj.NbEntries,
            obj.Description,
            obj.RestartType);
    }
}
