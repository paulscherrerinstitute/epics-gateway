/**
 * DTO of GWLogger.Backend.DTOs.GatewaySession
 */
class GatewaySession
{
    EndDate: Date;
    StartDate: Date;
    NbEntries: number;

    constructor(_enddate: string,
        _startdate: string,
        _nbentries: number)
    {
        this.EndDate = (_enddate ? new Date(parseInt(_enddate.substr(6, _enddate.length - 8))) : null);
        this.StartDate = (_startdate ? new Date(parseInt(_startdate.substr(6, _startdate.length - 8))) : null);
        this.NbEntries = _nbentries;
    }

    public static CreateFromObject(obj: any): GatewaySession
    {
        if (!obj)
            return null;
        return new GatewaySession(obj.EndDate,
            obj.StartDate,
            obj.NbEntries);
    }
}