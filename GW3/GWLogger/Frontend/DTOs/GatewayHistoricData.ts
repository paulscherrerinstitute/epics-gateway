class GatewayHistoricData
{
    CPU: LogStat[];
    Searches: LogStat[];
    PVs: LogStat[];
    Network: LogStat[];

    constructor(
        cpu: LogStat[],
        searches: LogStat[],
        pvs: LogStat[],
        network: LogStat[]
    ) {
        this.CPU = cpu;
        this.Searches = searches;
        this.PVs = pvs;
        this.Network = network;
    }

    public static CreateFromObject(o: any) {
        return new GatewayHistoricData(
            o.CPU ? o.CPU.map(v => LogStat.CreateFromObject(v)) : null,
            o.Searches ? o.Searches.map(v => LogStat.CreateFromObject(v)) : null,
            o.PVs ? o.PVs.map(v => LogStat.CreateFromObject(v)) : null,
            o.Network ? o.Network.map(v => LogStat.CreateFromObject(v)) : null
        );
    }

}