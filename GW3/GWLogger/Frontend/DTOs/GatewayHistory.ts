class GatewayHistory {
    public cpuHistory: LogStat[];
    public searchHistory: LogStat[];
    public pvsHistory: LogStat[];
    public msgSecHistory: LogStat[];
    public clientsHistory: LogStat[];
    public serversHistory: LogStat[];
    public networkHistory: LogStat[];

    public static CreateFromObject(o: any): GatewayHistory {
        var hist = new GatewayHistory();
        hist.cpuHistory = o.cpuHistory.map(hist => LogStat.CreateFromObject(hist));
        hist.searchHistory = o.searchHistory.map(hist => LogStat.CreateFromObject(hist));
        hist.pvsHistory = o.pvsHistory.map(hist => LogStat.CreateFromObject(hist));
        hist.msgSecHistory = o.msgSecHistory.map(hist => LogStat.CreateFromObject(hist));
        hist.clientsHistory = o.clientsHistory.map(hist => LogStat.CreateFromObject(hist));
        hist.serversHistory = o.serversHistory.map(hist => LogStat.CreateFromObject(hist));
        hist.networkHistory = o.networkHistory.map(hist => LogStat.CreateFromObject(hist));
        return hist;
    }

}