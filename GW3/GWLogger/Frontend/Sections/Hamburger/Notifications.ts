/// <reference path="../../../scripts/typings/kendo/kendo.all.d.ts" />

class NotificationsMenu
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
            NotificationsMenu.Show("Notifications are already enabled.");
            return;
        }

        Notification.requestPermission((permission) =>
        {
            // If the user accepts, let's create a notification
            if (permission === "granted")
            {
                NotificationsMenu.Show("Notifications are now enabled.");
                return;
            }
        });
    }

    static Show(text: string)
    {
        if (!("Notification" in window) || Notification['permission'] !== "granted")
            return;
        if (NotificationsMenu.notification)
            NotificationsMenu.notification.close();
        NotificationsMenu.notification = null;

        if (NotificationsMenu.notificationTimeout)
            clearTimeout(NotificationsMenu.notificationTimeout);
        NotificationsMenu.notificationTimeout = null;

        (<HTMLAudioElement>$("#notificationSound")[0]).play();

        NotificationsMenu.notification = new Notification("CAESAR", <any>{ icon: '/favicon-32x32.png', body: text, silent: false });
        NotificationsMenu.notification.onclick = (x) =>
        {
            window.focus();
            NotificationsMenu.notification.close();
            NotificationsMenu.notification = null;
            clearTimeout(NotificationsMenu.notificationTimeout);
            NotificationsMenu.notificationTimeout = null;
        };
        NotificationsMenu.notificationTimeout = setTimeout(NotificationsMenu.Close, 4000);
    }

    static Close()
    {
        NotificationsMenu.notification.close();
        NotificationsMenu.notification = null;

        if (NotificationsMenu.notificationTimeout)
            clearTimeout(NotificationsMenu.notificationTimeout);
        NotificationsMenu.notificationTimeout = null;
    }
}