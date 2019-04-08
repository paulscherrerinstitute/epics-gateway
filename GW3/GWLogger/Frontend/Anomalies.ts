class Anomalies
{

    public static Show(): void {
        Main.CurrentGateway = null;
        State.Set(true);
        this.GetGraphAnomalies((anomalies) => {
            var html = "";
            for (var anomaly of anomalies) {
                html += "<div class=\"anomaly-card\" onclick=\"Anomalies.Detail('" + anomaly.Filename + "')\">" + Utils.FullUtcDateFormat(anomaly.From) + ": <b>" + anomaly.Name + "</b> " + ((anomaly.To.getTime() - anomaly.From.getTime()) / 1000).toFixed(2) + " s";
                html += "<div id=\"anomaly_cpu_graph_" + anomaly.Filename + "\"></div>";
                html += "</div>";
            }
            $("#anomalyView").html(html);
            for (var anomaly of anomalies) {
                var graphData = <GraphData>{
                    Values: anomaly.History.cpuHistory.map(v => <GraphPoint>{ Value: v.Value, Label: v.Date }),
                };
                var graph = new LineGraph("anomaly_cpu_graph_" + anomaly.Filename, graphData, {
                    XLabelWidth: 50,
                    MinY: 0,
                    MaxY: 100,
                    FontSize: 10,
                    PlotColor: "#000080",
                    LabelFormat: Utils.ShortGWDateFormat,
                    HighlightSection: {
                        StartLabel: anomaly.From,
                        EndLabel: anomaly.To,
                        HighlightColor: "#800000",
                    }
                });
            }
        });
    }

    public static Refresh() {

    }

    public static Detail(filename: string) {
    }

    private static GetGraphAnomalies(callback: (a: GraphAnomaly[]) => void): void {
        $.ajax({
            type: 'POST',
            url: '/DataAccess.asmx/GetGraphAnomalies',
            data: {},
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',
            success: (data) => {
                var list = data.d;
                if (list) {
                    var anomalies = (<Array<any>>list).map(v => GraphAnomaly.CreateFromObject(v));
                    callback(anomalies);
                } else {
                    callback(null);
                }
            },
        });
    }

}