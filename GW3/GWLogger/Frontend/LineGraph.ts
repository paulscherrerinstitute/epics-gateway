interface GraphPoint
{
    Label: any;
    Value: number;
}

interface GraphData
{
    Values: GraphPoint[];
}

interface GraphOptions
{
    MinY?: number;
    MaxY?: number;
    FontSize?: number;
    PlotColor?: string;
    XLabelWidth?: number;
    ToolTip?: boolean;
    LabelFormat?: (value: any) => string;
    TooltipLabelFormat?: (value: any) => string;
    OnClick?: (label: any, value: number) => void;
    HighlightSection?: HighlightSection;
}

interface HighlightSection {
    StartLabel: any;
    EndLabel: any;
    HighlightColor: string;
}

interface Math
{
    log10(x): number;
}

Math.log10 = Math.log10 || function (x)
{
    return Math.log(x) * Math.LOG10E;
}

class LineGraph
{
    private elementContainer: string;
    private dataSource: GraphData;
    private graphOptions: GraphOptions;
    private width: number;
    private height: number;
    private fontSize: number;
    private plotColor: string;
    private plotLineWidth: number;
    private myResize;
    private xLabelWidth: number;
    private minY: number;
    private maxY: number;

    constructor(elementContainer: string, dataSource: GraphData, graphOptions?: GraphOptions)
    {
        this.elementContainer = elementContainer;
        this.dataSource = dataSource;
        this.graphOptions = graphOptions;
        this.fontSize = (graphOptions && graphOptions.FontSize ? graphOptions.FontSize : 12);
        this.plotColor = (graphOptions && graphOptions.PlotColor ? graphOptions.PlotColor : "#FF0000");
        this.plotLineWidth = 2;

        $("#" + elementContainer).html("<canvas id='canvas_" + elementContainer + "'></canvas>");
        this.myResize = () =>
        {
            this.Resize();
        };
        $(window).bind("resize", this.myResize);
        this.Resize();

        if (graphOptions && graphOptions.ToolTip === true)
            $("#canvas_" + elementContainer).on("mouseover", (evt) => { this.MouseOver(evt); }).on("mouseout", (evt) => { this.MouseOut(evt); });

        if (graphOptions.OnClick)
            $("#canvas_" + elementContainer).css({ cursor: "pointer" }).on("click", (evt) => { this.MouseClick(evt); });
    }

    public TooltipContent(): string
    {
        return "Works...";
    }

    public MouseClick(evt: JQueryMouseEventObject)
    {
        var p = $("#" + this.elementContainer).position();
        if (!this.dataSource || !this.dataSource.Values || this.dataSource.Values.length == 0)
            return;

        var x = evt.pageX;
        var y = evt.pageY;

        var gx = x - p.left;
        var gy = y - p.top;

        var idx = this.TransformCanvasToPos(gx, gy);
        if (idx === null)
            return;
        if (this.dataSource.Values.length <= idx)
            return;
        this.graphOptions.OnClick(this.dataSource.Values[idx].Label, this.dataSource.Values[idx].Value);
    }

    public MouseOver(evt: JQueryMouseEventObject)
    {
        if (!this.graphOptions.ToolTip)
            return;
        $("#canvas_" + this.elementContainer).on("mousemove", (evt) => { this.MouseMove(evt); });
        setTimeout(() =>
        {
            this.MouseMove(evt);
        }, 10);
    }

    public MouseOut(evt: JQueryMouseEventObject)
    {
        if (!this.graphOptions.ToolTip)
            return;
        $("#toolTip").css({ width: "" });
        ToolTip.Hide();
        $("#canvas_" + this.elementContainer).off("mousemove");
    }

    public MouseMove(evt: JQueryMouseEventObject)
    {
        if (!this.graphOptions.ToolTip)
            return;
        var p = $("#" + this.elementContainer).position();

        var x = evt.pageX;
        var y = evt.pageY;

        var gx = x - p.left;
        var gy = y - p.top;

        var idx = this.TransformCanvasToPos(gx, gy);
        if (idx === null || isNaN(idx))
        {
            //this.toolTip.hide();
            return;
        }

        var v = this.dataSource.Values[idx].Value;
        var vy = this.TransformY(v);

        var label = this.dataSource.Values[idx].Label;
        if (this.graphOptions.TooltipLabelFormat)
            label = this.graphOptions.TooltipLabelFormat(label);
        else if (this.graphOptions.LabelFormat)
            label = this.graphOptions.LabelFormat(label);
        ToolTip.Show(document.getElementById(this.elementContainer), "bottom", "" + label + ": " + this.dataSource.Values[idx].Value, { width: "200px", left: (x - 90) + "px", top: (y + 30) + "px" });
    }

    public Resize(): void
    {
        var container = $("#" + this.elementContainer).first();
        var canvas = <HTMLCanvasElement>($("#canvas_" + this.elementContainer)[0]);
        if (!canvas || !container) {
            this.Dispose();
            return;
        }
        this.width = canvas.width = container.width();
        this.height = canvas.height = container.height();
        this.Plot();
    }

    public Dispose(): void
    {
        $(window).unbind("resize", this.myResize);
    }

    public SetDataSource(source: GraphData)
    {
        this.dataSource = source;
        this.Resize();
    }

    public Plot(): void
    {
        var canvas = <HTMLCanvasElement>($("#canvas_" + this.elementContainer)[0]);
        var ctx = canvas.getContext("2d");
        ctx.clearRect(0, 0, this.width, this.height);

        // if there is no data we should stop
        if (!this.dataSource || !this.dataSource.Values || !this.dataSource.Values.length)
            return;

        this.minY = (this.graphOptions && this.graphOptions.MinY != null ? this.graphOptions.MinY : 0);
        this.maxY = (this.graphOptions && this.graphOptions.MaxY != null ? this.graphOptions.MaxY : 100);
        // Let's calculate the min value
        var values: number[];
        if (!this.graphOptions || this.graphOptions.MinY == null)
        {
            values = this.dataSource.Values.map((row) => { return row.Value });
            this.minY = Math.min.apply(null, values);
        }
        // Let's calculate the max value
        if (!this.graphOptions || this.graphOptions.MaxY == null)
        {
            if (!values)
                values = this.dataSource.Values.map((row) => { return row.Value });
            this.maxY = Math.max.apply(null, values);
            var unit = Math.pow(10, Math.floor(Math.log10(this.maxY)));
            this.maxY = Math.ceil(this.maxY * 2 / unit) * unit / 2;
        }
        // if the min and the max are the same we must add 1 to make some difference
        if (this.maxY == this.minY)
            this.maxY += 1;

        ctx.font = "" + this.fontSize + "pt sans-serif";
        ctx.fillStyle = "#000000";
        ctx.lineWidth = 1;

        // Let's plot the X labels
        var nbXLabels = Math.floor((this.height - this.fontSize * 3) / (this.fontSize * 4));
        if (nbXLabels > 2 && nbXLabels % 2 != 0)
            nbXLabels--;
        this.xLabelWidth = this.graphOptions.XLabelWidth ? this.graphOptions.XLabelWidth : 0;
        if (!this.graphOptions.XLabelWidth)
        {
            // Calculate the width needed for the labels
            for (var i = 0; i <= nbXLabels; i++)
            {
                var xVal = Math.round((this.minY + (this.maxY - this.minY) * i / nbXLabels) * 100) / 100;
                if ((this.maxY - this.minY) > 10)
                    xVal = Math.round(xVal);
                this.xLabelWidth = Math.max(this.xLabelWidth, ctx.measureText("" + xVal).width);
            }
            this.xLabelWidth += 3;
        }
        // Draw the X labels
        for (var i = 0; i <= nbXLabels; i++)
        {
            var xVal = Math.round((this.minY + (this.maxY - this.minY) * i / nbXLabels) * 100) / 100;
            if ((this.maxY - this.minY) > 10)
                xVal = Math.round(xVal);
            ctx.fillText("" + xVal, this.xLabelWidth - ctx.measureText("" + xVal).width, this.TransformY(xVal) + (i == 0 ? 0 : 6));
        }

        // Draw the origins
        ctx.strokeStyle = "#000000";
        ctx.beginPath();
        ctx.moveTo(this.xLabelWidth + 2.5, 0);
        ctx.lineTo(this.xLabelWidth + 2.5, Math.round(this.TransformY(this.minY)) + 0.5);
        ctx.lineTo(this.width, Math.round(this.TransformY(this.minY)) + 0.5);
        ctx.stroke();
        // Draw the horizontal grid
        ctx.beginPath();
        ctx.setLineDash([2, 2]);
        for (var i = 1; i <= nbXLabels; i++)
        {
            var y = Math.round(this.TransformY(Math.round((this.minY + (this.maxY - this.minY) * i / nbXLabels) * 100) / 100));
            ctx.moveTo(this.xLabelWidth + 2.5, y + 0.5);
            ctx.lineTo(this.width, y + 0.5);
        }
        ctx.stroke();

        // Draw the Y labels
        var nbYLabels = 3;
        for (var i = 0; i <= nbYLabels; i++)
        {
            var x = (this.width - (this.xLabelWidth + 2)) * i / nbYLabels + this.xLabelWidth;
            var label = this.dataSource.Values[Math.round(i * (this.dataSource.Values.length - 1) / nbYLabels)].Label;
            if (this.graphOptions.LabelFormat)
                label = this.graphOptions.LabelFormat(label);
            var labelWidth = ctx.measureText(label).width;
            if (i == nbYLabels)
                x -= labelWidth;
            else if (i > 0)
                x -= labelWidth / 2;
            ctx.fillText(label, x, this.height - this.fontSize);
        }
        // Draw the vertical grid
        ctx.beginPath();
        for (var i = 1; i <= nbYLabels; i++)
        {
            var x = Math.round((this.width - (this.xLabelWidth + 2)) * i / nbYLabels + this.xLabelWidth);
            ctx.moveTo(x + 0.5, 0);
            ctx.lineTo(x + 0.5, this.TransformY(this.minY));
        }
        ctx.stroke();

        var highlightSection = this.graphOptions.HighlightSection;
        var isHighlighted = false;

        // Let's plot the area of the values
        ctx.beginPath();
        ctx.globalAlpha = 0.1;
        ctx.fillStyle = this.plotColor;
        for (var i = 0; i < this.dataSource.Values.length; i++)
        {
            var point = this.dataSource.Values[i];
            var y_zero = this.TransformY(0);
            var y = this.TransformY(point.Value);
            var x = this.TransformX(i, this.xLabelWidth + 2);

            if (highlightSection) {
                var comparer = point.Label instanceof Date ? (a, b) => this.AreDatesEqual(a, b) : (a, b) => a == b;
                if (i == 0 && point.Label >= highlightSection.StartLabel)
                    isHighlighted = true;
                if (comparer(point.Label, highlightSection.StartLabel))
                    isHighlighted = true;
                if (comparer(point.Label, highlightSection.EndLabel))
                    isHighlighted = false;

                var newFillStyle = isHighlighted ? highlightSection.HighlightColor : this.plotColor;
                if (ctx.fillStyle != newFillStyle) {
                    ctx.lineTo(x, y);
                    ctx.lineTo(x, y_zero);

                    ctx.fill();

                    ctx.beginPath();
                    ctx.fillStyle = newFillStyle;

                    ctx.lineTo(x, y_zero);
                    ctx.lineTo(x, y);
                }
            }

            if (i == 0)
            {
                ctx.lineTo(x, y_zero);
                ctx.lineTo(x, y);
            }
            else
                ctx.lineTo(x, y);
            if (i == this.dataSource.Values.length - 1)
                ctx.lineTo(x, y_zero);
        }
        ctx.fill();
        ctx.globalAlpha = 1;

        // Let's plot the values
        ctx.beginPath();
        ctx.setLineDash([]);
        ctx.strokeStyle = this.plotColor;
        ctx.lineWidth = this.plotLineWidth;

        isHighlighted = false;
        for (var i = 0; i < this.dataSource.Values.length; i++)
        {
            var point = this.dataSource.Values[i];

            var y = this.TransformY(point.Value);
            var x = this.TransformX(i, this.xLabelWidth + 2);

            if (highlightSection) {
                var comparer = point.Label instanceof Date ? (a,b) => this.AreDatesEqual(a,b) : (a, b) => a == b;

                if (i == 0 && point.Label >= highlightSection.StartLabel)
                    isHighlighted = true;
                if (comparer(point.Label, highlightSection.StartLabel))
                    isHighlighted = true;
                if (comparer(point.Label, highlightSection.EndLabel))
                    isHighlighted = false;

                var newFillStyle = isHighlighted ? highlightSection.HighlightColor : this.plotColor;
                if (ctx.strokeStyle != newFillStyle)
                {
                    if (i == 0)
                        ctx.moveTo(x, y);
                    else
                        ctx.lineTo(x, y);

                    ctx.stroke();

                    ctx.beginPath();
                    ctx.strokeStyle = newFillStyle;
                    ctx.setLineDash([]);
                    ctx.lineWidth = this.plotLineWidth;
                }
            }

            if (i == 0)
                ctx.moveTo(x, y);
            else
                ctx.lineTo(x, y);
        }
        ctx.stroke();
    }

    private AreDatesEqual(a: Date, b: Date): boolean {
        return a.getUTCFullYear() == b.getUTCFullYear() &&
            a.getUTCMonth() == b.getUTCMonth() &&
            a.getUTCDate() == b.getUTCDate() &&
            a.getUTCHours() == b.getUTCHours() &&
            a.getUTCMinutes() == b.getUTCMinutes() &&
            a.getUTCSeconds() == b.getUTCSeconds();
    }

    // Transforms the X value in the screen coordinate
    private TransformX(x: number, xLabelWidth: number)
    {
        return x * (this.width - this.xLabelWidth) / this.dataSource.Values.length + this.xLabelWidth;
    }

    // Transforms the Y value in the screen coordinate
    private TransformY(y: number): number
    {
        return (this.height - (this.fontSize + 1) * 3) - (y - this.minY) * (this.height - (this.fontSize + 1) * 3) / (this.maxY - this.minY) + (this.fontSize + 1) * 1;
    }

    private TransformCanvasToPos(x: number, y: number): number
    {
        if (x - this.xLabelWidth < 0 || x >= this.width)
            return null;
        var idx = Math.ceil((x - this.xLabelWidth) / ((this.width - this.xLabelWidth) / this.dataSource.Values.length));
        if (idx < 0 || idx >= this.dataSource.Values.length)
            return null;
        return idx;
    }
}