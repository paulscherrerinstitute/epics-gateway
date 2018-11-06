/// <reference path="../scripts/typings/kendo/kendo.all.d.ts" />

class Notifications
{
    static notification: Notification = null;
    static notificationTimeout: number = null;

    static Alert(message: string, title: string = "CAESAR")
    {
        $("#diaglog").kendoDialog({ closable: true, title: title, content: message, modal: true, actions: [{ text: "Ok", action: (e) => { return true }, primary: true }] });
    }

    static Confirm(message: string, title: string = "CAESAR")
    {
        return (<any>$("<div></div>").kendoConfirm(<any>{
            title: title,
            content: message
        }).data("kendoConfirm").open()).result;
    }

    static Popup(message: string, type: string = "info")
    {
        if (!$("#popupNotification").data("kendoNotification"))
            $("#popupNotification").kendoNotification();
        var popup = $("#popupNotification").data("kendoNotification");
        popup.show(message, type);
    }

    static Enable()
    {
        if (!("Notification" in window))
        {
            kendo.alert("This browser does not support system notifications");
            return;
        }

        if (Notification['permission'] === "granted")
        {
            // If it's okay let's create a notification
            Notifications.Show("Notifications are already enabled.");
            return;
        }

        Notification.requestPermission((permission) =>
        {
            // If the user accepts, let's create a notification
            if (permission === "granted")
            {
                Notifications.Show("Notifications are now enabled.");
                return;
            }
        });
    }

    static Show(text: string)
    {
        if (!("Notification" in window) || Notification['permission'] !== "granted")
            return;
        if (Notifications.notification)
            Notifications.notification.close();
        Notifications.notification = null;

        if (Notifications.notificationTimeout)
            clearTimeout(Notifications.notificationTimeout);
        Notifications.notificationTimeout = null;

        (<HTMLAudioElement>$("#notificationSound")[0]).play();

        Notifications.notification = new Notification("CAESAR", <any>{ icon: '/favicon-32x32.png', body: text, silent: false });
        Notifications.notification.onclick = (x) =>
        {
            window.focus();
            Notifications.notification.close();
            Notifications.notification = null;
            clearTimeout(Notifications.notificationTimeout);
            Notifications.notificationTimeout = null;
        };
        Notifications.notificationTimeout = setTimeout(Notifications.Close, 4000);
    }

    static Close()
    {
        Notifications.notification.close();
        Notifications.notification = null;

        if (Notifications.notificationTimeout)
            clearTimeout(Notifications.notificationTimeout);
        Notifications.notificationTimeout = null;
    }
}