class DetailsCommands
{
    static Restart()
    {
        NotificationsMenu.Confirm("Are you sure you want to restart " + Main.CurrentGateway).then(() =>
        {
            var version = StatusPage.Get(Main.CurrentGateway).Version;
            var command = "RestartGateway3";
            if (version && version.startsWith("1."))
                command = "RestartGateway";
            NotificationsMenu.Popup("Restart sent...");
            $.ajax({
                type: 'POST',
                url: '/AuthAccess/AuthService.asmx/GatewayCommand',
                data: JSON.stringify({ gatewayName: Main.CurrentGateway.toUpperCase(), command: command }),
                contentType: 'application/json; charset=utf-8',
                dataType: 'json',
                success: function (msg)
                {
                    NotificationsMenu.Popup((<string>msg.d).replace(/\n/g,"<br>"), "info");
                },
                error: function (msg, textStatus)
                {
                    NotificationsMenu.Popup(msg.responseText, "error");
                }
            });
        });
    }

    static Update()
    {
        NotificationsMenu.Confirm("Are you sure you want to update " + Main.CurrentGateway).then(() =>
        {
            var version = StatusPage.Get(Main.CurrentGateway).Version;
            var command = "UpdateGateway3";
            if (version && version.startsWith("1."))
                command = "UpdateGateway";
            NotificationsMenu.Popup("Update sent...");
            $.ajax({
                type: 'POST',
                url: '/AuthAccess/AuthService.asmx/GatewayCommand',
                data: JSON.stringify({ gatewayName: Main.CurrentGateway.toUpperCase(), command: command }),
                contentType: 'application/json; charset=utf-8',
                dataType: 'json',
                success: function (msg)
                {
                    NotificationsMenu.Popup((<string>msg.d).replace(/\n/g, "<br>"), "info");
                },
                error: function (msg, textStatus)
                {
                    NotificationsMenu.Popup(msg.responseText, "error");
                }
            });
        });
    }
}