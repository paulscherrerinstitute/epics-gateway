class ToolTip
{
    static currentElement: HTMLElement | Element = null;
    static currentPosition: "left" | "top" | "bottom" | "right";
    static lastCall: Date = null;

    public static Show(element: HTMLElement | Element, position: "left" | "top" | "bottom" | "right", content: string)
    {

        ToolTip.currentElement = element;
        ToolTip.currentPosition = position;

        $("#toolTip").html(content).show().css({ left: "-1000px", top: "-1000px" });
        setTimeout(ToolTip.SetPosition, 0);
        $(element).on("mouseout", ToolTip.MouseOut);
        ToolTip.lastCall = new Date();
    }

    public static SetPosition()
    {
        if (!ToolTip.currentElement)
            return;
        var x = $(ToolTip.currentElement).offset().left;
        var y = $(ToolTip.currentElement).offset().top;
        var w = $(ToolTip.currentElement).width();
        var h = $(ToolTip.currentElement).height();

        var tw = $("#toolTip").width();
        var th = $("#toolTip").height();

        switch (ToolTip.currentPosition)
        {
            case "left":
                x -= tw + 15;
                y = y + h / 2 - th / 2 + 5;
                break;
            case "top":
                y -= th + 15;
                x = x + w / 2 - tw / 2;
                break;
            case "right":
                x += w + 15;
                y = y + h / 2 - th / 2;
                break;
            case "bottom":
                x = x + w / 2 - tw / 2;
                y += h + 15;
                break;
            default:
        }

        if (x < 0)
            x = 0;
        if (y < 0)
            y = 0;
        if (x + tw > window.innerWidth)
            x = window.innerWidth - tw;
        if (y + th > window.innerHeight)
            y = window.innerHeight - th;

        $("#toolTip").css({ left: Math.round(x) + "px", top: Math.round(y) + "px" });
    }

    public static MouseOut(e: JQueryMouseEventObject)
    {
        /*if (ToolTip.lastCall)
        {
            var diff = (new Date()).getTime() - ToolTip.lastCall.getTime();
            if (diff < 50)
                return;
        }*/

        ToolTip.currentElement = null;
        $("#toolTip").hide();
        $(e.target).off("mouseout", ToolTip.MouseOut);
    }

    public static Hide()
    {
        $(ToolTip.currentElement).off("mouseout", ToolTip.MouseOut);
        $("#toolTip").hide();
        ToolTip.currentElement = null;
    }
}