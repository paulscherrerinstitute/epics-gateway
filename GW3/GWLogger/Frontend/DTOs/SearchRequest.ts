/**
 * DTO of GWLogger.Backend.DTOs.SearchRequest
 */
class SearchRequest
{
    Channel: string;
    Date: Date;
    Client: string;
    NbSearches: number;

    constructor(_channel: string,
        _date: string,
        _client: string,
        _nbsearches: number)
    {
        this.Channel = _channel;
        this.Date = (_date ? new Date(parseInt(_date.substr(6, _date.length - 8))) : null);
        this.Client = _client;
        this.NbSearches = _nbsearches;
    }

    public static CreateFromObject(obj: any): SearchRequest
    {
        if (!obj)
            return null;
        return new SearchRequest(obj.Channel,
            obj.Date,
            obj.Client,
            obj.NbSearches);
    }
}