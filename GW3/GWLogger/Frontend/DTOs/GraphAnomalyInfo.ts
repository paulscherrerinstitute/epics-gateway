/**
 * DTO of GWLogger.Live.GraphAnomalyInfo
 */
class GraphAnomalyInfo {

    FileName: string;
    Name: string;
    From: Date;
    To: Date;
    Duration: string;

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
        if (this.From && this.To) {
            this.Duration = Utils.DurationString(this.To.getTime() - this.From.getTime());
        } else {
            this.Duration = "";
        }
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