class GraphAnomalyInfo {

    FileName: string;
    Name: string;
    From: Date;
    To: Date;

    constructor(
        filename: string,
        name: string,
        from: string,
        to: string
    ) {
        this.FileName = filename;
        this.Name = name;
        this.From = (from ? new Date(parseInt(from.substr(6, from.length - 8))) : null);
        this.To = (to ? new Date(parseInt(to.substr(6, to.length - 8))) : null);
    }

    public static CreateFromObject(o: any): GraphAnomalyInfo
    {
        return new GraphAnomalyInfo(
            o.FileName,
            o.Name,
            o.From,
            o.To
        );
    }

}