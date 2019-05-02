class AnomaliesPage
{
    private static IsLoadingAnomalyInfos = false;
    private static IsLoadingAnomalyDetail = false;
    private static LastAnomalyInfos: GraphAnomalyInfo[] = [];
    private static CurrentPreview: string = null;
    private static PreviewGraph: LineGraph = null;

    public static async Show() {
        if (Main.DetailAnomaly == null) {
            await this.ShowOverviewSection();
        } else {
            this.CurrentPreview = null;
            await this.ShowDetailSection(Main.DetailAnomaly);
        }
    }

    private static async ShowOverviewSection() {
        if (this.IsLoadingAnomalyInfos)
            return;

        this.IsLoadingAnomalyInfos = true;

        var msg = null;
        try {
            msg = await Utils.Loader("GetGraphAnomalies", null);
        } catch{
            this.IsLoadingAnomalyInfos = false;
            return;
        }

        if (msg == null || msg.d == null)
            return;

        var anomalyInfos: GraphAnomalyInfo[] = msg.d.map(v => GraphAnomalyInfo.CreateFromObject(v)); // Fix dates
        this.LastAnomalyInfos = anomalyInfos;

        this.IsLoadingAnomalyInfos = false;
        if (Main.DetailAnomaly != null)
            return;

        var elem = $("#anomaly-overview-preview");
        if (elem == null || elem.length == 0) {
            $("#anomalyView").html(`
                <div id="anomaly-overview-preview-text">CPU-Usage preview:</div>
                <div id="anomaly-overview-preview"></div>
                <div id="anomaly-overview-grid"></div>
            `);
        }

        if (anomalyInfos.length > 0) {
            this.ShowAnomalyPreview(anomalyInfos[0].FileName);
        }

        var prevGrid = $("#anomaly-overview-grid").data("kendoGrid");
        if (prevGrid)
            prevGrid.destroy();

        var scrollPosition = $("#anomaly-overview-grid").scrollTop();
        $("#anomaly-overview-grid").kendoGrid({
            columns: [
                {
                    title: "From",
                    field: "From",
                    format: "{0:MM/dd HH:mm:ss}",
                },
                {
                    title: "Gateway",
                    field: "Name",
                },
                {
                    title: "Duration",
                    field: "Duration",
                }
            ],
            /*Kendo definitions do not allow strings even though the api supports templates */
            toolbar: <any>`
                <div>
                    <a class="k-button" onclick="AnomaliesPage.Show()"><span class="k-icon k-i-reload"></span>&nbsp;Refresh</a>
                </div>
            `,
            selectable: "row",
            sortable: true,
            scrollable: true,
            height: "100%",
            rowTemplate: `
                <tr data-uid="#= uid #" onmouseover="AnomaliesPage.ShowAnomalyPreview('#: FileName #')">
                    <td>#: Utils.FullUtcDateFormat(From) #</td>
                    <td>#: Name #</td>
                    <td>#: Duration #</td>
                </tr>`,
            dataSource: {
                data: anomalyInfos,
            },
            change: (e: kendo.ui.GridChangeEvent) => {
                e.sender.select().each(function (i, e) {
                    var grid = $("#anomaly-overview-grid").data("kendoGrid");
                    var dataItem = <any>grid.dataItem(this);
                    Main.DetailAnomaly = dataItem.FileName;
                    State.Set(true);
                    State.Pop();
                    AnomaliesPage.Show();
                });
            },
        });
        $("#anomaly-overview-grid").scrollTop(scrollPosition);
    }

    private static async ShowAnomalyPreview(filename: string) {
        var anomalyInfo = this.LastAnomalyInfos.find(v => v.FileName == filename);
        if (anomalyInfo == null)
            return;
        if (this.CurrentPreview == filename)
            return;
        this.CurrentPreview = filename;

        var msg = await Utils.Loader("GetGraphAnomalyPreview", { filename: anomalyInfo.FileName });
        var data = msg.d ? msg.d.map(v => LogStat.CreateFromObject(v)) : [];
        var options: GraphOptions = {
            MaxY: 100,
            MinY: 0,
            XLabelWidth: 30,
            FontSize: 10,
            PlotColor: "#000080",
            LabelFormat: Utils.ShortGWDateFormat,
            HighlightSection: {
                StartLabel: anomalyInfo.From,
                EndLabel: anomalyInfo.To,
                HighlightColor: "#b60000",
            },
            ToolTip: true,
        };
        if (this.PreviewGraph) {
            this.PreviewGraph.Dispose();
        }
        this.PreviewGraph = new LineGraph("anomaly-overview-preview", {
            Values: data.map(v => <GraphPoint>{ Label: v.Date, Value: v.Value })
        }, options);
    }

    private static async ShowDetailSection(filename: string) {
        if (this.IsLoadingAnomalyDetail)
            return;

        var elem = $("#anomaly-detail");
        if (elem != null && elem.length != 0)
            return;

        this.IsLoadingAnomalyDetail = true;
        try {
            var msg = await Utils.Loader("GetGraphAnomaly", { filename: filename });
        } catch{
            this.IsLoadingAnomalyDetail = false;
            return;
        }

        this.IsLoadingAnomalyDetail = false;
        if (Main.DetailAnomaly == null)
            return;

        var anomaly = GraphAnomaly.CreateFromObject(msg.d); // Fix dates

        var html = `
            <div id="anomaly-detail">
                <h2>${anomaly.Name}</h2>
                <div style="float: left;">
                    <p><b>From</b>: ${Utils.FullUtcDateFormat(anomaly.From)}</p>
                    <p><b>To</b>: ${Utils.FullUtcDateFormat(anomaly.To)}</p>
                </div>
                <div style="float: right; padding: 5px;">
                    <div class="button" onclick="AnomaliesPage.DeleteDetail('${filename}')">Delete Anomaly</div>
                </div>
                <hr style="clear:both;"/>

                <h4>CPU</h4>
                <div id="anomaly-detail-cpu-graph"></div>
                <h4>PVs</h4>
                <div id="anomaly-detail-pv-graph"></div>
                <h4>Searches</h4>
                <div id="anomaly-detail-searches-graph"></div>
                <h4>Network (MB/s)</h4>
                <div id="anomaly-detail-network-graph"></div>

                <h4>EventTypes before the anomaly</h4>
                <div id="anomaly-detail-before-event-types"></div>
                <h4>EventTypes during the anomaly</h4>
                <div id="anomaly-detail-during-event-types"></div>
                <h4>Interesting EventType remotes</h4>
                <div id="anomaly-detail-interesting-event-types"></div>
                <h4>Remotes before the anomaly</h4>
                <div id="anomaly-detail-before-remote-counts"></div>
                <h4>Remotes during the anomaly</h4>
                <div id="anomaly-detail-during-remote-counts"></div>
            </div>`;
        var scrolled = $("#anomaly-detail").scrollTop();
        $("#anomalyView").html(html);

        this.CreateGraph("anomaly-detail-cpu-graph", anomaly, anomaly.History.CPU, { MaxY: 100 });
        this.CreateGraph("anomaly-detail-pv-graph", anomaly, anomaly.History.PVs);
        this.CreateGraph("anomaly-detail-searches-graph", anomaly, anomaly.History.Searches);
        this.CreateGraph("anomaly-detail-network-graph", anomaly, anomaly.History.Network.map(v => { v.Value /= (1024 * 1024); return v; }));

        this.CreateTable("anomaly-detail-before-event-types", anomaly.BeforeEventTypes, "EventType", "Count");
        this.CreateTable("anomaly-detail-during-event-types", anomaly.DuringEventTypes, "EventType", "Count");
        this.CreateTable("anomaly-detail-before-remote-counts", anomaly.BeforeRemoteCounts, "Remote", "Count");
        this.CreateTable("anomaly-detail-during-remote-counts", anomaly.DuringRemoteCounts, "Remote", "Count");

        if (anomaly.InterestingEventTypeRemotes) {
            for (var i = 0; i < anomaly.InterestingEventTypeRemotes.length; i++) {
                var childId = `anomaly-detail-interesting-event-types-${i}`;
                $("#anomaly-detail-interesting-event-types").append(`<div id="${childId}" class="anomaly-detail-interesting"></div>`);
                var childTable = anomaly.InterestingEventTypeRemotes[i];
                this.CreateTable(childId, childTable.TopRemotes, `Top remotes for the ${childTable.EventType.Text}-EventType`, "Count");
            }
        }

        $("#anomaly-detail").scrollTop(scrolled);
    }

    public static CreateTable(id: string, data: QueryResultValue[], textName: string, valueName: string) {
        var elem = $(`#${id}`);

        var prevTable = elem.data("kendoGrid");
        if (prevTable)
            prevTable.destroy();

        elem.kendoGrid({
            columns: [
                {
                    title: valueName,
                    field: "Value",
                    width: 200,
                },
                {
                    title: textName,
                    field: "Text",
                }
            ],
            sortable: false,
            selectable: false,
            scrollable: false,
            dataSource: { data: data }
        });
    }

    public static async DeleteDetail(filename: string) {
        await Utils.Loader("DeleteGraphAnomaly", { filename: filename });
        Main.Path = "Anomalies";
        Main.DetailAnomaly = null;

        State.Set(true);
        State.Pop();
        Main.Refresh(true);
    }

    private static CreateGraph(id: string, anomaly: GraphAnomaly, data: LogStat[], opts?: { MaxY: number }) {
        if (data == null)
            return;

        var points = <GraphData>{
            Values: data.map(v => <GraphPoint>{
                Value: v.Value,
                Label: v.Date
            })
        };
        new LineGraph(id, points, {
            XLabelWidth: 50,
            MinY: 0,
            MaxY: opts ? opts.MaxY : null,
            FontSize: 10,
            PlotColor: "#000080",
            LabelFormat: Utils.ShortGWDateFormat,
            HighlightSection: {
                StartLabel: anomaly.From,
                EndLabel: anomaly.To,
                HighlightColor: "#b60000",
            },
            ToolTip: true,
        });
    }
}