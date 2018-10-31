class Subscription
{
    static Show()
    {
        $("#reportView").show();
        $("#reportContent").removeAttr('style').removeClass().css({ "overflow": "auto", "padding": "5px" });
        $('#helpView').hide();

        if ($("#reportContent").data("kendoGrid"))
            $("#reportContent").data("kendoGrid").destroy();

        var html = "";
        html += "<center>";
        html += "<h2>Alert Subscription</h2>";
        html += "<div id='subscriptionEmail'></div>";

        html += "<div id='subscriptionGrid'></div>";

        html += "<div id='subscriptionButtons'><span class='button' onclick='Subscription.All()' id='dbgRun'>All</span>";
        html += "<span class='button' onclick='Subscription.Save()' id='dbgRun'>Save</span>";
        html += "<span class='button' onclick='Subscription.Unsubscribe()' id='dbgRun'>Unsubscribe</span></div>";
        html += "</center>";
        $("#reportContent").html(html);

        $.ajax({
            type: 'POST',
            url: '/AuthAccess/AuthService.asmx/CurrentUserEmail',
            data: JSON.stringify({}),
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',
            success: function (msg)
            {
                $("#subscriptionEmail").html("Current email: " + msg.d);
            }
        });

        $.ajax({
            type: 'POST',
            url: '/AuthAccess/AuthService.asmx/GetCurrentSubscription',
            data: JSON.stringify({}),
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',
            success: function (msg)
            {
                var subscribed: string[] = msg.d;
                var data = Live.shortInfo.map((c) =>
                {
                    return {
                        Subscribed: subscribed.indexOf(c.Name) != -1,
                        Gateway: c.Name
                    }
                });
                data.sort((a, b) =>
                {
                    if (a.Gateway > b.Gateway)
                        return 1;
                    if (a.Gateway < b.Gateway)
                        return -1;
                    return 0;
                });

                $("#subscriptionGrid").kendoGrid({
                    dataSource: {
                        data: data
                    },
                    columns: [
                        { title: "Subscribed", field: "Subscribed", template: '<input type="checkbox" #= Subscribed ? \'checked="checked"\' : "" # class="chkbx k-checkbox" id="check_#= Gateway#" /><label class="k-checkbox-label" for="check_#= Gateway#"></label>', width: 120 },
                        { title: "Gateway", field: "Gateway" }
                    ]
                });

                $("#subscriptionGrid .k-grid-content").on("change", "input.chkbx", function (e)
                {
                    var grid = $("#subscriptionGrid").data("kendoGrid");
                    var dataItem = grid.dataItem($(e.target).closest("tr"));

                    dataItem.set("Subscribed", this.checked);
                });
            }
        });
    }

    static Save()
    {
        var data = $("#subscriptionGrid").data("kendoGrid").dataSource.data();
        var subscribed: string[] = [];
        for (var i = 0; i < data.length; i++)
        {
            if (data[i].Subscribed)
                subscribed.push(data[i].Gateway);
        }

        $.ajax({
            type: 'POST',
            url: '/AuthAccess/AuthService.asmx/Subscribe',
            data: JSON.stringify({ gateways: subscribed }),
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',
            success: function (msg)
            {
                Notifications.Popup("Subscriptions updated.");
            },
            error: function (msg, textStatus)
            {
                Notifications.Popup("Error: " + msg.responseText, "error");
            }
        });
    }

    static All()
    {
        var data = $("#subscriptionGrid").data("kendoGrid").dataSource.data();
        for (var i = 0; i < data.length; i++)
            data[i].Subscribed = true;
        $("#subscriptionGrid").data("kendoGrid").refresh();
    }

    static Unsubscribe()
    {
        $.ajax({
            type: 'POST',
            url: '/AuthAccess/AuthService.asmx/Unsubscribe',
            data: JSON.stringify({}),
            contentType: 'application/json; charset=utf-8',
            dataType: 'json',
            success: function (msg)
            {
                Notifications.Popup("Unsubscribed to all.");

                var data = $("#subscriptionGrid").data("kendoGrid").dataSource.data();
                for (var i = 0; i < data.length; i++)
                    data[i].Subscribed = false;
                $("#subscriptionGrid").data("kendoGrid").refresh();
            }
        });
    }
}