﻿class State
{
    public static Init()
    {
        $(window).bind('popstate', State.Pop);
        State.Pop();
    }

    static Path(): string[]
    {
        return document.location.pathname.split('/');
    }

    static Parameters(): any
    {
        var url = "" + document.location;
        if (url.indexOf("#") == -1)
            url = "";
        else
            url = url.substr(url.indexOf("#") + 1);
        var parts = url.split("&");
        var queryString = {};
        parts.forEach(row => queryString[row.split("=")[0]] = decodeURIComponent(row.split("=")[1]));

        return queryString;
    }

    static Pop()
    {
        Main.CurrentGateway = null;
        Main.CurrentTime = null;
        Main.EndDate = null;

        //$("#help").show();
        //$("#clients, #servers, #logs").hide();

        var path = State.Path();

        if (path.length > 1)
        {
            $("#mainTabs li").removeClass("activeTab");
            switch (path[1])
            {
                case "GW":
                    Main.Path = "GW";
                    $($("#mainTabs li")[2]).addClass("activeTab");
                    $("#mapView").hide();
                    $("#gatewayView").hide();
                    $("#gatewayDetails").hide();
                    $("#anomalyView").hide();
                    $("#configurationView").hide();
                    $("#logView").show();
                    break;
                case "Map":
                    Main.Path = "Map";
                    $($("#mainTabs li")[3]).addClass("activeTab");
                    $("#mapView").show();
                    $("#gatewayView").hide();
                    $("#gatewayDetails").hide();
                    $("#anomalyView").hide();
                    $("#configurationView").hide();
                    $("#logView").hide();
                    break;
                case "Anomalies":
                    Main.Path = "Anomalies";
                    $($("#mainTabs li")[4]).addClass("activeTab");
                    $("#mapView").hide();
                    $("#gatewayView").hide();
                    $("#gatewayDetails").hide();
                    $("#anomalyView").show();
                    $("#configurationView").hide();
                    $("#logView").hide();
                    break;
                case "Configuration":
                    Main.Path = "Configuration";
                    $("#mapView").hide();
                    $("#gatewayView").hide();
                    $("#gatewayDetails").hide();
                    $("#anomalyView").show();
                    $("#configurationView").show();
                    $("#logView").hide();
                    ConfigurationPage.Show(path[2]);
                    break;
                default:
                    Main.Path = "Status";
                    $($("#mainTabs li")[1]).addClass("activeTab");
                    $("#mapView").hide();
                    $("#gatewayView").show();
                    $("#gatewayDetails").hide();
                    $("#logView").hide();
                    $("#configurationView").hide();
                    $("#anomalyView").hide();
            }
        }

        if (path.length > 2 && (path[1] == "GW" || path[1] == "Status" || path[1] == "Configuration"))
            Main.CurrentGateway = path[2];
        else if (path.length > 1 && path[1] == "GW")
            Main.CurrentGateway = null;

        if (Main.CurrentGateway && path[1] == "GW")
            LogsPage.LoadGateways();

        if (Main.Path == "Status" && Main.CurrentGateway)
        {
            $("#gatewayView").hide();
            $("#gatewayDetails").show();
            DetailPage.Show(Main.CurrentGateway);
        }
        else if (Main.Path == "Status")
        {
            $("#gatewayView").show();
            $("#gatewayDetails").hide();
        }

        /*var url = "" + document.location;
        //console.log(url);
        if (url.indexOf("#") == -1)
            url = "";
        else
            url = url.substr(url.indexOf("#") + 1);
        var parts = url.split("&");
        var queryString = {};
        parts.forEach(row => queryString[row.split("=")[0]] = decodeURIComponent(row.split("=")[1]));*/
        var queryString = State.Parameters();

        if (queryString["c"])
            Main.CurrentTime = new Date(parseInt(queryString["c"]));
        else
            Main.CurrentTime = null;
        if (queryString["s"])
        {
            Main.StartDate = new Date(parseInt(queryString["s"]));
            $("#startDate").val(Utils.FullUtcDateFormat(Main.StartDate));
        }
        else
        {
            Main.StartDate = null;
            $("#startDate").val("");
        }
        if (queryString["e"])
        {
            Main.EndDate = new Date(parseInt(queryString["e"]));
            $("#endDate").val(Utils.FullUtcDateFormat(Main.EndDate));
        }
        else
        {
            Main.EndDate = null;
            $("#endDate").val("");
        }
        if (queryString["q"])
            $("#queryField").val(queryString["q"]);
        else
            $("#queryField").val("");
        if (queryString["t"])
            Main.CurrentTab = parseInt(queryString["t"]);
        else
            Main.CurrentTab = 0;
        if (queryString["f"])
            Main.DetailAnomaly = queryString["f"];
        else
            Main.DetailAnomaly = null;

        $("#logFilter li").removeClass("activeTab");
        $($("#logFilter li")[Main.CurrentTab]).addClass("activeTab");

        if (Main.CurrentTime)
        {
            var tdiff = (Main.EndDate.getTime() - Main.CurrentTime.getTime()) + Main.EndDate.getTimezoneOffset() * 60000;
            var t = tdiff / (10 * 60 * 1000);
            if (t == 144 && ((new Date()).getTime() - Main.CurrentTime.getTime()) < 24 * 3600 * 1000)
                Main.IsLast = true;
            else
                Main.IsLast = false;
        }

        Main.Refresh();
        if (Main.Path == "Anomalies") {
            AnomaliesPage.Show();
        }
        //Main.DelayedSearch(Main.LoadLogStats, true);
    }

    static setStateTimeout: number;

    static Set(force: boolean = false)
    {
        if (State.setStateTimeout)
            clearTimeout(State.setStateTimeout);
        State.setStateTimeout = null;
        if (force)
            State.DoSet();
        else
            setTimeout(State.DoSet, 200);
    }

    static DoSet()
    {
        State.setStateTimeout = null;
        var params = "";
        if (Main.Path == "GW") {
            if (Main.StartDate && Main.EndDate) {
                if (Main.CurrentTime)
                    params += (params != "" ? "&" : "#") + "c=" + Main.CurrentTime.getTime();
                params += (params != "" ? "&" : "#") + "s=" + Main.StartDate.getTime();
                params += (params != "" ? "&" : "#") + "e=" + Main.EndDate.getTime();
            }
            if ($("#queryField").val())
                params += (params != "" ? "&" : "#") + "q=" + encodeURIComponent($("#queryField").val());
            if (Main.CurrentTab != 0)
                params += (params != "" ? "&" : "#") + "t=" + Main.CurrentTab;
        }
        else if (Main.Path == "Anomalies")
        {
            if (Main.DetailAnomaly) {
                params += "#f=" + encodeURIComponent(Main.DetailAnomaly);
            }
        }
        window.history.pushState(null, Main.BaseTitle + " - " + Main.CurrentGateway, '/' + Main.Path + '/' + (Main.CurrentGateway ? Main.CurrentGateway : "") + params);
    }
}