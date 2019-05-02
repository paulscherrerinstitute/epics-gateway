/**
 * DTO of GWLogger.Live.GraphAnomaly
 */
class GraphAnomaly {
    FileName: string;
    Name: string;
    From: Date;
    To: Date;
    InterestingEventTypeRemotes: InterestingEventType[];
    BeforeRemoteCounts: QueryResultValue[];
    DuringRemoteCounts: QueryResultValue[];
    BeforeEventTypes: QueryResultValue[];
    DuringEventTypes: QueryResultValue[];
    History: GatewayHistoricData;

    constructor(
        filename: string,
        name: string,
        from: string,
        to: string,
        interestingEventTypeRemotes: InterestingEventType[],
        beforeRemoteCounts: QueryResultValue[],
        duringRemoteCounts: QueryResultValue[],
        beforeEventTypes: QueryResultValue[],
        duringEventTypes: QueryResultValue[],
        history: GatewayHistoricData
    ) {
        this.FileName = filename;
        this.Name = name;
        this.From = (from ? new Date(parseInt(from.substr(6, from.length - 8))) : null);
        this.To = (to ? new Date(parseInt(to.substr(6, to.length - 8))) : null);
        this.InterestingEventTypeRemotes = interestingEventTypeRemotes;
        this.BeforeRemoteCounts = beforeRemoteCounts;
        this.DuringRemoteCounts = duringRemoteCounts;
        this.BeforeEventTypes = beforeEventTypes;
        this.DuringEventTypes = duringEventTypes;
        this.History = history;
    }

    public static CreateFromObject(obj: any): GraphAnomaly {
        if (!obj)
            return null;
        return new GraphAnomaly(
            obj.FileName,
            obj.Name,
            obj.From,
            obj.To,
            obj.InterestingEventTypeRemotes,
            obj.BeforeRemoteCounts,
            obj.DuringRemoteCounts,
            obj.BeforeEventTypes,
            obj.DuringEventTypes,
            GatewayHistoricData.CreateFromObject(obj.History),
        );
    }
}

class InterestingEventType {
    EventType: QueryResultValue;
    TopRemotes: QueryResultValue[];
}

class QueryResultValue {
    Value: number;
    Text: string;
}