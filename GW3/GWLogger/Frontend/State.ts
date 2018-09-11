﻿class State
{
    static Path(): string[]
    {
        return document.location.pathname.split('/');
    }

    static Pop(jEvent: JQueryEventObject)
    {
        Main.CurrentGateway = null;
        Main.CurrentTime = null;
        Main.EndDate = null;

        $("#help").show();
        $("#clients, #servers, #logs").hide();

        var path = State.Path();
        if (path.length > 2 && path[1] == "GW")
            Main.CurrentGateway = path[2];
        else
            Main.CurrentGateway = null;

        var url = "" + document.location;
        console.log(url);
        if (url.indexOf("#") == -1)
            url = "";
        else
            url = url.substr(url.indexOf("#") + 1);
        var parts = url.split("&");
        var queryString = {};
        parts.forEach(row => queryString[row.split("=")[0]] = decodeURIComponent(row.split("=")[1]));

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

        Main.LoadGateways();
        //Main.DelayedSearch(Main.LoadLogStats, true);
    }

    static Set()
    {
        var params = "";
        if (Main.CurrentTime)
            params += (params != "" ? "&" : "#") + "c=" + Main.CurrentTime.getTime();
        if (Main.StartDate)
            params += (params != "" ? "&" : "#") + "s=" + Main.StartDate.getTime();
        if (Main.EndDate)
            params += (params != "" ? "&" : "#") + "e=" + Main.EndDate.getTime();
        if ($("#queryField").val())
            params += (params != "" ? "&" : "#") + "q=" + encodeURIComponent($("#queryField").val());
        if (Main.CurrentTab != 0)
            params += (params != "" ? "&" : "#") + "t=" + Main.CurrentTab;
        window.history.pushState(null, Main.BaseTitle + " - " + Main.CurrentGateway, '/GW/' + Main.CurrentGateway + params);
    }
}