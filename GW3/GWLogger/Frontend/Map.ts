﻿interface KnownNetwork
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

class Map
{
    static nets: KnownNetwork[] = [];
    static gateways: KnownNetwork[] = [];
    static HoverGateway: string = "";

    static Init()
    {
        Map.AddNetwork(450, 500, "Office");
        Map.AddNetwork(800, 500, "SLS Machine");
        Map.AddGateway(700, 530, "SLS-CAGW02");
        Map.AddGateway(700, 450, "SLS-ARCH-CAGW02");


        var slsBeamlines = ["X01DC-CAGW02", "X02DA-CAGW02", "X03DA-CAGW02", "X03MA-CAGW02", "X04DB-CAGW02", "X04SA-CAGW02",
            "X05DA-CAGW02", "X05LA-CAGW02", "X06DA-CAGW02", "X06MX-CAGW", "X06SA-CAGW02", "X07DA-CAGW02", "X07MA-CAGW02",
            "X09LA-CAGW02", "X09LB-CAGW02", "X10DA-CAGW02", "X10SA-CAGW02", "X11MA-CAGW02", "X12SA-CAGW02"];

        for (var i = 0; i < slsBeamlines.length; i++)
        {
            var a = Math.PI * 1.5 * i / slsBeamlines.length + Math.PI * 1.25;
            var m = (i % 2 == 0 ? 260 : 390);
            var x = 800 + Math.cos(a) * m;
            var y = 500 + Math.sin(a) * m;
            Map.AddNetwork(x, y, slsBeamlines[i].split('-')[0], 50, 20);
        }

        for (var i = 0; i < slsBeamlines.length; i++)
        {
            var a = Math.PI * 1.5 * i / slsBeamlines.length + Math.PI * 1.25;
            var m = (i % 2 == 0 ? 210 : 310);
            x = 800 + Math.cos(a) * m;
            y = 500 + Math.sin(a) * m;

            Map.AddGateway(x - 40, y - 15, slsBeamlines[i], 90, 10);
            Map.AddGatewayLink(slsBeamlines[i], slsBeamlines[i].split('-')[0], 1);
            Map.AddGatewayLink(slsBeamlines[i], "SLS Machine", 1);
        }

        Map.AddGatewayLink("SLS-CAGW02", "Office", 0);
        Map.AddGatewayLink("SLS-ARCH-CAGW02", "Office", 0);

        Map.AddNetwork(150, 330, "Swiss FEL", 100);
        Map.AddGateway(90, 350, "SF-CAGW", 120, 10);
        Map.AddGatewayLink("SF-CAGW", "Office", 0);

        Map.AddNetwork(150, 110, "SF-ES", 100);
        Map.AddGateway(90, 130, "SFES-CAGW", 120, 10);
        Map.AddGatewayLink("SFES-CAGW", "Swiss FEL", 0);

        Map.AddNetwork(380, 210, "HIPA", 100);
        Map.AddGateway(320, 230, "HIPA-CAGW02", 120, 10);
        Map.AddGatewayLink("HIPA-CAGW02", "Office", 0);

        Map.AddNetwork(380, 800, "Proscan", 100);
        Map.AddGateway(320, 750, "PROSCAN-CAGW02", 120, 10);
        Map.AddGatewayLink("PROSCAN-CAGW02", "Office", 0);

        Map.AddNetwork(150, 600, "Cryo", 100);
        Map.AddGateway(90, 550, "CRYO-CAGW02", 120, 10);
        Map.AddGatewayLink("CRYO-CAGW02", "Office", 0);

        // Legend
        Map.Add("rect", { fill: "#F0F0F0", stroke_width: 2, stroke: "black", x: 1100 - 40, y: 2, width: 80, height: 160 });
        Map.Add("text", { x: 1100 - 35, y: 10, alignment_baseline: "central", font_family: "Sans-serif", font_size: 16, style: "text-anchor: left;", font_weight: "bold", fill: "black" }, "Legend:");

        Map.Add("circle", { fill: "white", stroke_width: 2, stroke: "#E0E0E0", cx: 1100, cy: 60, r: 35 });
        Map.Add("text", { x: 1100, y: 60, alignment_baseline: "central", font_family: "Sans-serif", font_size: 16, style: "text-anchor: middle;", font_weight: "bold", fill: "#A0A0A0" }, "Network");

        Map.Add("rect", { fill: "#b8f779", stroke_width: 2, stroke: "black", x: 1100 - 35, y: 60 + 40, width: 70, height: 12 + 18 });
        Map.Add("text", { x: 1100 - 35 + 70 / 2, y: 60 + 40 + (12 + 16) / 2, alignment_baseline: "central", font_family: "Sans-serif", font_size: 16, style: "text-anchor: middle;", font_weight: "bold", fill: "black" }, "Gateway");

        Map.Add("rect", { fill: "white", stroke_width: 2, stroke: "black", x: 1100 - 35, y: 60 + 40 + 45, width: 6, height: 6 });
        Map.Add("text", { x: 1100, y: 60 + 40 + 47, alignment_baseline: "central", font_family: "Sans-serif", font_size: 16, style: "text-anchor: middle;", font_weight: "bold", fill: "black" }, "NIC");

        $("#mapView").kendoTooltip({
            showOn: "focus",
            content: (e: kendo.ui.TooltipEvent) =>
            {
                var live = Live.Get(Map.HoverGateway);
                var html = "<b>Gateway " + Map.HoverGateway + "</b><br>";
                html += "<table style='text-align: left;'>";
                html += "<tr><td>CPU</td><td>" + Math.round(live.CPU) + "%</td></tr>";
                html += "<tr><td>Searches</td><td>" + live.Searches + " / Sec</td></tr>";
                html += "<tr><td>Version</td><td>" + live.Version + "</td></tr>";
                html += "<tr><td>State</td><td>" + GatewayStates[live.State] + "</td></tr>";
                html += "</table>";
                html += "-- Click to view details --";
                return html;
            }
        }).data("kendoTooltip").hide();
    }

    static AddNetwork(x: number, y: number, label: string, netSize: number = 150, fontSize: number = 30)
    {
        Map.nets.push({ X: x, Y: y, Name: label, Width: netSize });
        Map.Add("circle", { fill: "transparent", stroke_width: 2, stroke: "#E0E0E0", cx: x, cy: y, r: netSize });
        Map.Add("text", { x: x, y: y, alignment_baseline: "central", font_family: "Sans-serif", font_size: fontSize, style: "text-anchor: middle;", font_weight: "bold", fill: "#A0A0A0" }, label);
    }

    static toolTip: kendo.ui.Tooltip;

    static AddGateway(x: number, y: number, label: string, width: number = 180, fontSize: number = 18)
    {
        Map.gateways.push({ X: x, Y: y, Name: label, Width: width, Height: 12 + fontSize });
        var elem = Map.Add("rect", { id: "svg_gw_" + label, fill: "white", stroke_width: 2, stroke: "black", x: x, y: y, width: width, height: 12 + fontSize, cursor: "pointer" });
        elem.onclick = (e) =>
        {
            Main.CurrentGateway = label.toLowerCase();
            Main.Path = "Status";
            State.Set(true);
            State.Pop(null);

        };
        elem.onmouseover = (e) =>
        {
            var j = $("#svg_gw_" + label);
            j.attr("stroke", "#597db7");
            j.attr("stroke-width", "4");
            Map.toolTip = $("#mapView").data("kendoTooltip");
            Map.HoverGateway = label;

            if (Map.toolTip)
            {
                Map.toolTip.show($("#mapView"));
                Map.toolTip.refresh();
                this.toolTip['tt'] = (new Date()).getTime();
            }
            setTimeout(() =>
            {
                var p = $("#mapView").position();

                $("#mapView_tt_active").parent().css({ left: (x + p.left - 90 + width / 2 - $("#mapView").scrollLeft()) + "px", top: (y + p.top + fontSize + 53 - $("#mapView").scrollTop()) + "px" });
            }, 10);
        }
        elem.onmouseout = (e) =>
        {
            $("#svg_gw_" + label).attr("stroke", "black");
            $("#svg_gw_" + label).attr("stroke-width", "2");
            setTimeout(() =>
            {
                if (Map.toolTip && $("#svg_gw_" + label).attr("stroke-width") == "2")
                {
                    Map.toolTip.hide();
                    Map.toolTip = null;
                }
            }, 10);
        }

        var e2 = Map.Add("text", { x: x + width / 2, y: y + (12 + fontSize) / 2, alignment_baseline: "central", font_family: "Sans-serif", font_size: fontSize, cursor: "pointer", style: "text-anchor: middle;", font_weight: "bold", fill: "black" }, label);
        e2.onclick = elem.onclick;
        e2.onmouseover = elem.onmouseover;
        e2.onmouseout = elem.onmouseout;
    }

    static SetGatewayState(label: string, state: number)
    {
        var colors: string[] = ["#b8f779", "#fff375", "#ffc942", "#ff9e91"];
        $("#svg_gw_" + label).attr("fill", colors[state]);
    }

    static Add(tag: string, attrs: {}, content?: string): SVGElement
    {
        var el = document.createElementNS('http://www.w3.org/2000/svg', tag);
        for (var k in attrs)
            el.setAttribute(k.replace(/\_/g, "-"), attrs[k]);
        if (content)
            el.innerHTML = content;
        document.getElementById("svgMap").appendChild(el);
        return el;
    }

    static AddGatewayLink(gateway: string, network: string, netFoot: number)
    {
        var gw: KnownNetwork;
        for (var i = 0; i < Map.gateways.length; i++)
        {
            if (gateway == Map.gateways[i].Name)
            {
                gw = Map.gateways[i];
                break;
            }
        }
        var net: KnownNetwork;
        for (var i = 0; i < Map.gateways.length; i++)
        {
            if (network == Map.nets[i].Name)
            {
                net = Map.nets[i];
                break;
            }
        }

        var a = Map.Angle(net.X - (gw.X + gw.Width / 2), net.Y - (gw.Y + gw.Height / 2));
        var p = Map.BorderPoint({ X: gw.X, Y: gw.Y, Width: gw.Width, Height: gw.Height }, { X: gw.X + gw.Width / 2, Y: gw.Y + gw.Height / 2 }, a);

        var x1 = p.X;
        var y1 = p.Y;
        var x2 = net.X - net.Width * 0.9 * Math.cos(a);
        var y2 = net.Y - net.Width * 0.9 * Math.sin(a);

        if (!(x2 >= gw.X && x2 <= gw.X + gw.Width && y2 >= gw.Y && y2 <= gw.Y + 30))
            Map.Add("line", { x1: x1, y1: y1, x2: x2, y2: y2, stroke: "black", stroke_width: 2 });

        Map.Add("rect", { fill: "white", stroke_width: 2, stroke: "black", x: x1 - 3, y: y1 - 3, width: 6, height: 6 });
        if (netFoot != 0 && !(x2 >= gw.X && x2 <= gw.X + gw.Width && y2 >= gw.Y && y2 <= gw.Y + 30))
            Map.Add("rect", { fill: "white", stroke_width: 2, stroke: "black", x: x2 - 3, y: y2 - 3, width: 6, height: 6 });
    }

    static Angle(ad: number, op: number): number
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

    static BorderPoint(rect: Rectangle, pt: Point, angle: number): Point
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