class Anomalies
{
    private static OneSuccessfulLoad = false;

    public static Show(): void {
        Main.CurrentGateway = null;
        State.Set(true);
        this.GetGraphAnomalies((anomalies) => {
            this.OneSuccessfulLoad = true;
            var html = "";
            if (anomalies.length == 0) {
                html += "<div class=\"no-anomalies\">No anomalies</div>";
            }

            for (var anomaly of anomalies) {
                html += "<div class=\"anomaly-card\" onclick=\"Anomalies.Detail('" + anomaly.Filename + "')\">" + Utils.FullUtcDateFormat(anomaly.From) + ": <b>" + anomaly.Name + "</b> " + this.DurationString(anomaly.To.getTime() - anomaly.From.getTime());
                html += "<div id=\"anomaly_cpu_graph_" + anomaly.Filename + "\"></div>";
                html += "</div>";
            }
            var scrolled = $("#anomalyView").scrollTop();
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
                        HighlightColor: "#b60000",
                    }
                });
            }

            $("#anomalyView").scrollTop(scrolled);
        });
    }

    public static Refresh() {
        if (this.OneSuccessfulLoad) {
            this.Show();
        }
    }

    public static Detail(filename: string) {
    }

    private static DurationString(millis: number): string {
        var duration = "";

        var seconds = millis / 1000;
        if (seconds >= 1)
            duration = (seconds % 60).toFixed(0) + " s";

        var minutes = seconds / 60;
        if (minutes >= 1)
            duration = (minutes % 60).toFixed(0) + " min " + duration;

        var hours = minutes / 60;
        if (hours >= 1)
            duration = (hours % 24).toFixed(0) + " h " + duration;

        return duration;
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