class ToolTip
{
    static currentElement: HTMLElement | Element = null;
    static currentPosition: "left" | "top" | "bottom" | "right";
    static manualCss: { width?: string; left: string; top: string; } = null;

    public static Show(element: HTMLElement | Element, position: "left" | "top" | "bottom" | "right", content: string, manualCss?: { width?: string; left: string; top: string; })
    {
        ToolTip.currentElement = element;
        ToolTip.currentPosition = position;
        ToolTip.manualCss = manualCss;

        $("#toolTip").html(content).show();
        ToolTip.SetPosition();
        $(element).on("mouseout", ToolTip.MouseOut);
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

        var css = { left: Math.round(x) + "px", top: Math.round(y) + "px" };
        var manualCss = ToolTip.manualCss;
        if (manualCss) {
            if (manualCss.width)
                (<any>css).width = manualCss.width;
            css.left = manualCss.left;
            css.top = manualCss.top;
        }
        $("#toolTip").css(css);
    }

    public static MouseOut(e: JQueryMouseEventObject)
    {
        ToolTip.Hide();
    }

    public static Hide()
    {
        $(ToolTip.currentElement).off("mouseout", ToolTip.MouseOut);
        $("#toolTip").hide();
        ToolTip.currentElement = null;
    }
}