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

class Map
{
    static nets: KnownNetwork[] = [];
    static gateways: KnownNetwork[] = [];

    static Init()
    {
        Map.AddNetwork(450, 500, "Office");
        Map.AddNetwork(800, 500, "SLS Machine");
        Map.AddGateway(700, 530, "SLS-CAGW02");
        Map.AddGateway(700, 450, "SLS-ARCH-CAGW02");


        var slsBeamlines = ["X01DC", "X02DA", "X03DA", "X03MA", "X04DB", "X04SA", "X05DA", "X05LA", "X06DA", "X06MX", "X06SA", "X07DA", "X07MA", "X09LA", "X09LB", "X10DA", "X10SA", "X11MA", "X12SA"];

        for (var i = 0; i < slsBeamlines.length; i++)
        {
            var a = Math.PI * 1.5 * i / slsBeamlines.length + Math.PI * 1.25;
            var m = (i % 2 == 0 ? 260 : 390);
            var x = 800 + Math.cos(a) * m;
            var y = 500 + Math.sin(a) * m;
            Map.AddNetwork(x, y, slsBeamlines[i], 50, 20);
        }

        for (var i = 0; i < slsBeamlines.length; i++)
        {
            var a = Math.PI * 1.5 * i / slsBeamlines.length + Math.PI * 1.25;
            var m = (i % 2 == 0 ? 210 : 310);
            x = 800 + Math.cos(a) * m;
            y = 500 + Math.sin(a) * m;

            Map.AddGateway(x - 40, y - 15, slsBeamlines[i] + "-CAGW02", 90, 10);
            Map.AddGatewayLink(slsBeamlines[i] + "-CAGW02", slsBeamlines[i], 1);
            Map.AddGatewayLink(slsBeamlines[i] + "-CAGW02", "SLS Machine", 1);
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
    }

    static AddNetwork(x, y, label, netSize: number = 150, fontSize: number = 30)
    {
        Map.nets.push({ X: x, Y: y, Name: label, Width: netSize });
        Map.Add("circle", { fill: "transparent", stroke_width: 2, stroke: "#E0E0E0", cx: x, cy: y, r: netSize });
        Map.Add("text", { x: x, y: y, alignment_baseline: "central", font_family: "Sans-serif", font_size: fontSize, style: "text-anchor: middle;", font_weight: "bold", fill: "#A0A0A0" }, label);
    }

    static AddGateway(x, y, label, width: number = 180, fontSize: number = 18)
    {
        Map.gateways.push({ X: x, Y: y, Name: label, Width: width, Height: 12 + fontSize });
        Map.Add("rect", { fill: "white", stroke_width: 2, stroke: "black", x: x, y: y, width: width, height: 12 + fontSize });
        Map.Add("text", { x: x + width / 2, y: y + (12 + fontSize) / 2, alignment_baseline: "central", font_family: "Sans-serif", font_size: fontSize, style: "text-anchor: middle;", font_weight: "bold", fill: "black" }, label);
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