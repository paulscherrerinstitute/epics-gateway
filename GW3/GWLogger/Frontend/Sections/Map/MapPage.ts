interface KnownNetwork
{
    X: number;
    Y: number;
    Name: string;
    Width: number;
    Height?: number;
}

interface RectSize
{
    Width: number;
    Height: number;
}

interface Point
{
    X: number;
    Y: number;
}

interface Rectangle
{
    X: number;
    Y: number;
    Width: number;
    Height: number;
}

class MapPage
{
    static nets: KnownNetwork[] = [];
    static gateways: KnownNetwork[] = [];
    static HoverGateway: string = "";

    public static Init()
    {
        MapPage.AddNetwork(450, 500, "Office");
        MapPage.AddNetwork(800, 500, "SLS Machine");
        MapPage.AddGateway(700, 530, "SLS-CAGW02");
        //Map.AddGateway(700, 450, "SLS-ARCH-CAGW02");

        var slsBeamlines: string[] = [];
        $(".GWDisplay").each((idx, elem) =>
        {
            if (elem.id.startsWith("X"))
                slsBeamlines.push(elem.id);
        });

        for (var i = 0; i < slsBeamlines.length; i++)
        {
            var a = Math.PI * 1.5 * i / slsBeamlines.length + Math.PI * 1.25;
            var m = (i % 2 == 0 ? 260 : 390);
            var x = 800 + Math.cos(a) * m;
            var y = 500 + Math.sin(a) * m;
            MapPage.AddNetwork(x, y, slsBeamlines[i].split('-')[0], 50, 20);
        }

        for (var i = 0; i < slsBeamlines.length; i++)
        {
            var a = Math.PI * 1.5 * i / slsBeamlines.length + Math.PI * 1.25;
            var m = (i % 2 == 0 ? 210 : 310);
            x = 800 + Math.cos(a) * m;
            y = 500 + Math.sin(a) * m;

            MapPage.AddGateway(x - 40, y - 15, slsBeamlines[i], 110, 10);
            MapPage.AddGatewayLink(slsBeamlines[i], slsBeamlines[i].split('-')[0], 1);
            MapPage.AddGatewayLink(slsBeamlines[i], "SLS Machine", 1);
        }

        MapPage.AddGatewayLink("SLS-CAGW02", "Office", 0);
        //Map.AddGatewayLink("SLS-ARCH-CAGW02", "Office", 0);

        MapPage.AddNetwork(150, 330, "Swiss FEL", 100);
        MapPage.AddGateway(90, 350, "SF-CAGW", 140, 10);
        MapPage.AddGatewayLink("SF-CAGW", "Office", 0);

        MapPage.AddNetwork(150, 110, "SF-ES", 100);
        MapPage.AddGateway(90, 130, "SFES-CAGW", 140, 10);
        MapPage.AddGatewayLink("SFES-CAGW", "Swiss FEL", 0);

        MapPage.AddNetwork(380, 210, "HIPA", 100);
        MapPage.AddGateway(320, 230, "HIPA-CAGW02", 120, 10);
        MapPage.AddGatewayLink("HIPA-CAGW02", "Office", 0);

        MapPage.AddNetwork(380, 800, "Proscan", 100);
        MapPage.AddGateway(320, 750, "PROSCAN-CAGW02", 140, 10);
        MapPage.AddGatewayLink("PROSCAN-CAGW02", "Office", 0);

        MapPage.AddNetwork(150, 600, "Cryo", 100);
        MapPage.AddGateway(90, 550, "CRYO-CAGW02", 140, 10);
        MapPage.AddGatewayLink("CRYO-CAGW02", "Office", 0);

        // Legend
        MapPage.Add("rect", { fill: "#F0F0F0", stroke_width: 2, stroke: "black", x: 1100 - 40, y: 2, width: 100, height: 180 });
        MapPage.Add("text", { x: 1100 - 35, y: 10, alignment_baseline: "central", font_family: "Sans-serif", font_size: 16, style: "text-anchor: left;", font_weight: "bold", fill: "black" }, "Legend:");

        MapPage.Add("circle", { fill: "white", stroke_width: 2, stroke: "#E0E0E0", cx: 1100, cy: 60, r: 35 });
        MapPage.Add("text", { x: 1100, y: 60, alignment_baseline: "central", font_family: "Sans-serif", font_size: 16, style: "text-anchor: middle;", font_weight: "bold", fill: "#A0A0A0" }, "Network");

        MapPage.Add("rect", { fill: "#b8f779", stroke_width: 2, stroke: "black", x: 1100 - 35, y: 60 + 40, width: 70, height: 12 + 18 });
        MapPage.Add("text", { x: 1100 - 35 + 70 / 2, y: 60 + 40 + (12 + 16) / 2, alignment_baseline: "central", font_family: "Sans-serif", font_size: 16, style: "text-anchor: middle;", font_weight: "bold", fill: "black" }, "Gateway");

        MapPage.Add("rect", { fill: "white", stroke_width: 2, stroke: "black", x: 1100 - 35, y: 60 + 40 + 45, width: 6, height: 6 });
        MapPage.Add("text", { x: 1100 - 20, y: 60 + 40 + 47, alignment_baseline: "central", font_family: "Sans-serif", font_size: 16, style: "text-anchor: left;", font_weight: "bold", fill: "black" }, "NIC");

        var points = [4, 0, 8, 6, 0, 6, 0, 0, 8, 0, 4, 6];
        var ps = "";
        for (var i = 0; i < 3; i++)
            ps += (i != 0 ? " " : "") + (points[i * 2 + 6] + 1100 - 35) + "," + (points[i * 2 + 1 + 6] + 60 + 40 + 45 + 20);
        MapPage.Add("polygon", { points: ps, fill: "#00E000", stroke: "black", stroke_width: 0.5 });
        MapPage.Add("text", { x: 1100 - 20, y: 60 + 40 + 45 + 22, alignment_baseline: "central", font_family: "Sans-serif", font_size: 16, style: "text-anchor: left;", font_weight: "bold", fill: "black" }, "Direction");

        //<use xlink: href = "#one" />
        //Map.Add("use", { "xlink:href": "", id: "use_hover" });

        MapPage.AddGateway(350, 400, "CAESAR");
    }

    public static GetTooltipText(): string
    {
        if (MapPage.HoverGateway == "CAESAR")
        {
            var html = "<b>CAESAR Server</b><br>";
            return html;
        }
        else
        {
            var live = StatusPage.Get(MapPage.HoverGateway);
            if (!live)
                return;

            var html = "<b>Gateway " + MapPage.HoverGateway + "</b><br>";
            html += "<table style='text-align: left;'>";
            html += "<tr><td>CPU</td><td>" + Math.round(live.CPU) + "%</td></tr>";
            html += "<tr><td>Searches</td><td>" + live.Searches + " / Sec</td></tr>";
            html += "<tr><td>Running Time</td><td>" + (live.RunningTime ? live.RunningTime.substr(0, live.RunningTime.lastIndexOf('.')).replace(".", " day(s) ") : "&nbsp;") + "</td></tr>";
            html += "<tr><td>State</td><td>" + GatewayStates[live.State] + "</td></tr>";
            html += "<tr><td>Build</td><td>" + live.Build + "</td></tr>";
            html += "<tr><td>Version</td><td>" + live.Version + "</td></tr>";
            html += "<tr><td>Direction</td><td>" + live.Direction + "</td></tr>";
            html += "</table>";
            html += "-- Click to view details --";
            return html;
        }
    }

    private static AddNetwork(x: number, y: number, label: string, netSize: number = 150, fontSize: number = 30)
    {
        MapPage.nets.push({ X: x, Y: y, Name: label, Width: netSize });
        MapPage.Add("circle", { fill: "transparent", stroke_width: 2, stroke: "#E0E0E0", cx: x, cy: y, r: netSize });
        MapPage.Add("text", { x: x, y: y, alignment_baseline: "central", font_family: "Sans-serif", font_size: fontSize, style: "text-anchor: middle;", font_weight: "bold", fill: "#A0A0A0" }, label);
    }

    //static toolTip: kendo.ui.Tooltip;

    private static AddGateway(x: number, y: number, label: string, width: number = 200, fontSize: number = 18)
    {
        var h = 12 + fontSize;

        var overFunction = (e) =>
        {
            var j = $("#svg_gw_" + label);
            j.attr("stroke", "#597db7");
            j.attr("stroke-width", "4");
            //Map.toolTip = $("#mapView").data("kendoTooltip");
            MapPage.HoverGateway = label;

            var p = $("#mapView").position();
            // Calculates the position on screen
            var t = (y + p.top + fontSize + 53 - $("#mapView").scrollTop());

            // If the placement is too low, then moves the tooltip above
            if (t + 230 > $(window).innerHeight())
                t -= 215 + fontSize;

            ToolTip.Show(document.getElementById("svg_gw_" + label), "bottom", MapPage.GetTooltipText());
            // Workaround to place the tooltip at the wished position
            setTimeout(() =>
            {
                $("#mapView_tt_active").parent().css({ left: (x + p.left - 90 + width / 2 - $("#mapView").scrollLeft()) + "px", top: t + "px" });
            }, 10);
        };

        var outFunction = (e) =>
        {
            $("#svg_gw_" + label).attr("stroke", "black");
            $("#svg_gw_" + label).attr("stroke-width", "2");
        };

        var elem: SVGElement;
        if (label == "CAESAR")
        {
            elem = MapPage.Add("rect", { x: x, y: y, rx: 5, ry: 5, width: width, height: 12 + fontSize, stroke_width: 2, stroke: "#E0E0E0", fill: "white" });
            elem.onmouseover = overFunction;
            elem.onmouseout = outFunction;
            elem = MapPage.Add("rect", { x: x + 3.5, y: y + 3.5, rx: 3, ry: 3, width: width - 6, height: 6 + fontSize, stroke_width: 1, stroke: "#E0E0E0", fill: "white" });
            elem.onmouseover = overFunction;
            elem.onmouseout = outFunction;
            elem = MapPage.Add("text", { x: x + width / 2, y: 416, alignment_baseline: "central", font_family: "Sans-serif", font_size: 16, style: "text-anchor: middle;", font_weight: "bold", fill: "black" }, "CAESAR");
            elem.onmouseover = overFunction;
            elem.onmouseout = outFunction;
        }
        else
        {
            MapPage.gateways.push({ X: x, Y: y, Name: label, Width: width, Height: 12 + fontSize });
            elem = MapPage.Add("rect", { id: "svg_gw_" + label, fill: "white", stroke_width: 2, stroke: "black", x: x, y: y, width: width, height: h, cursor: "pointer" });
            elem.onclick = (e) =>
            {
                Main.CurrentGateway = label.toLowerCase();
                Main.Path = "Status";
                State.Set(true);
                State.Pop();

            };
            elem.onmouseover = overFunction;
            elem.onmouseout = outFunction;

            var e2 = MapPage.Add("text", { x: x + width / 2, y: y + (12 + fontSize) / 2, alignment_baseline: "central", font_family: "Sans-serif", font_size: fontSize, cursor: "pointer", style: "text-anchor: middle;", font_weight: "bold", fill: "black" }, label);
            e2.onclick = elem.onclick;
            e2.onmouseover = elem.onmouseover;
            e2.onmouseout = elem.onmouseout;

            var points = [4, 0, 8, 6, 0, 6, 0, 0, 8, 0, 4, 6];
            var ps = "";
            for (var i = 0; i < 3; i++)
                ps += (i != 0 ? " " : "") + (points[i * 2 + 6] + x + 3) + "," + (points[i * 2 + 1 + 6] + y + h / 2 + 1);
            e2 = MapPage.Add("polygon", { points: ps, fill: "#00E000", stroke: "black", stroke_width: 0.5 });
            e2.onclick = elem.onclick;
            e2.onmouseover = elem.onmouseover;
            e2.onmouseout = elem.onmouseout;

            ps = "";
            for (var i = 0; i < 3; i++)
                ps += (i != 0 ? " " : "") + (points[i * 2] + x + 3) + "," + (points[i * 2 + 1] + y + h / 2 - 8);
            e2 = MapPage.Add("polygon", { points: ps, fill: "#E00000", stroke: "black", stroke_width: 0.5, id: "svg_gw_" + label + "_dir" });
            e2.onclick = elem.onclick;
            e2.onmouseover = elem.onmouseover;
            e2.onmouseout = elem.onmouseout;
        }
    }

    public static SetGatewayState(label: string, state: number, direction: string)
    {
        var colors: string[] = ["#b8f779", "#fff375", "#ffc942", "#ff9e91"];
        $("#svg_gw_" + label).attr("fill", colors[state]);
        $("#svg_gw_" + label + "_dir").attr("visibility", direction == "BIDIRECTIONAL" ? "visible" : "hidden");
    }

    private static Add(tag: string, attrs: {}, content?: string): SVGElement
    {
        var el = document.createElementNS('http://www.w3.org/2000/svg', tag);
        for (var k in attrs)
            el.setAttribute(k.replace(/\_/g, "-"), attrs[k]);
        if (content)
            el.textContent = content;
        document.getElementById("svgMap").appendChild(el);
        return el;
    }

    private static AddGatewayLink(gateway: string, network: string, netFoot: number)
    {
        var gw: KnownNetwork;
        for (var i = 0; i < MapPage.gateways.length; i++)
        {
            if (gateway == MapPage.gateways[i].Name)
            {
                gw = MapPage.gateways[i];
                break;
            }
        }
        var net: KnownNetwork;
        for (var i = 0; i < MapPage.nets.length; i++)
        {
            if (network == MapPage.nets[i].Name)
            {
                net = MapPage.nets[i];
                break;
            }
        }

        var a = MapPage.Angle(net.X - (gw.X + gw.Width / 2), net.Y - (gw.Y + gw.Height / 2));
        var p = MapPage.BorderPoint({ X: gw.X, Y: gw.Y, Width: gw.Width, Height: gw.Height }, { X: gw.X + gw.Width / 2, Y: gw.Y + gw.Height / 2 }, a);

        var x1 = p.X;
        var y1 = p.Y;
        var x2 = net.X - net.Width * 0.9 * Math.cos(a);
        var y2 = net.Y - net.Width * 0.9 * Math.sin(a);

        if (!(x2 >= gw.X && x2 <= gw.X + gw.Width && y2 >= gw.Y && y2 <= gw.Y + 30))
            MapPage.Add("line", { x1: x1, y1: y1, x2: x2, y2: y2, stroke: "black", stroke_width: 2 });

        MapPage.Add("rect", { fill: "white", stroke_width: 2, stroke: "black", x: x1 - 3, y: y1 - 3, width: 6, height: 6 });
        if (netFoot != 0 && !(x2 >= gw.X && x2 <= gw.X + gw.Width && y2 >= gw.Y && y2 <= gw.Y + 30))
            MapPage.Add("rect", { fill: "white", stroke_width: 2, stroke: "black", x: x2 - 3, y: y2 - 3, width: 6, height: 6 });
    }

    private static Angle(ad: number, op: number): number
    {
        var angle = 0.0;
        if (ad == 0.0) // Avoid angles of 0 where it would make a division by 0
            ad = 0.00001;

        // Get the angle formed by the line
        angle = Math.atan(op / ad);
        if (ad < 0.0)
        {
            angle = Math.PI * 2.0 - angle;
            angle = Math.PI - angle;
        }

        while (angle < 0)
            angle += Math.PI * 2.0;
        return angle;
    }

    private static BorderPoint(rect: Rectangle, pt: Point, angle: number): Point
    {

        // catch cases where point is outside rectangle
        if (pt.X < rect.X) return null;
        if (pt.X > rect.X + rect.Width) return null;
        if (pt.Y < rect.Y) return null;
        if (pt.Y > rect.Y + rect.Height) return null;

        var dx = Math.cos(angle);
        var dy = Math.sin(angle);

        if (dx < 1.0e-16)
        {         // left border
            var y = (rect.X - pt.X) * dy / dx + pt.Y;

            if (y >= rect.Y && y <= rect.Y + rect.Height)
                return { X: rect.X, Y: y };
        }

        if (dx > 1.0e-16)
        {         // right border
            var y = (rect.X + rect.Width - pt.X) * dy / dx + pt.Y;

            if (y >= rect.Y && y <= rect.Y + rect.Height)
                return { X: rect.X + rect.Width, Y: y };
        }

        if (dy < 1.0e-16)
        {         // top border
            var x = (rect.Y - pt.Y) * dx / dy + pt.X;

            if (x >= rect.X && x <= rect.X + rect.Width)
                return { X: x, Y: rect.Y };
        }

        if (dy > 1.0e-16)
        {         // bottom border
            var x = (rect.Y + rect.Height - pt.Y) * dx / dy + pt.X;

            if (x >= rect.X && x <= rect.X + rect.Width)
                return { X: x, Y: rect.Y + rect.Height };
        }

        return null;
    }

}