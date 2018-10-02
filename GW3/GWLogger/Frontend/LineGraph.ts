interface GraphPoint
{
    Label: string;
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
    }

    public Resize(): void
    {
        var container = $("#" + this.elementContainer).first();
        var canvas = <HTMLCanvasElement>($("#canvas_" + this.elementContainer)[0]);
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

        var minY = (this.graphOptions && this.graphOptions.MinY != null ? this.graphOptions.MinY : 0);
        var maxY = (this.graphOptions && this.graphOptions.MaxY != null ? this.graphOptions.MaxY : 100);
        // Let's calculate the min value
        var values: number[];
        if (!this.graphOptions || this.graphOptions.MinY == null)
        {
            values = this.dataSource.Values.map((row) => { return row.Value });
            minY = Math.min.apply(null, values);
        }
        // Let's calculate the max value
        if (!this.graphOptions || this.graphOptions.MaxY == null)
        {
            if (!values)
                values = this.dataSource.Values.map((row) => { return row.Value });
            maxY = Math.max.apply(null, values);
            var unit = Math.pow(10, Math.floor(Math.log10(maxY)));
            maxY = Math.ceil(maxY * 2 / unit) * unit / 2;
        }
        // if the min and the max are the same we must add 1 to make some difference
        if (maxY == minY)
            maxY += 1;

        ctx.font = "" + this.fontSize + "pt sans-serif";
        ctx.fillStyle = "#000000";
        ctx.lineWidth = 1;

        // Let's plot the X labels
        var nbXLabels = Math.floor((this.height - this.fontSize * 3) / (this.fontSize * 4));
        if (nbXLabels > 2 && nbXLabels % 2 != 0)
            nbXLabels--;
        var xLabelWidth = this.graphOptions.XLabelWidth ? this.graphOptions.XLabelWidth : 0;
        if (!this.graphOptions.XLabelWidth)
        {
            // Calculate the width needed for the labels
            for (var i = 0; i <= nbXLabels; i++)
            {
                var xVal = Math.round((minY + (maxY - minY) * i / nbXLabels) * 100) / 100;
                if ((maxY - minY) > 10)
                    xVal = Math.round(xVal);
                xLabelWidth = Math.max(xLabelWidth, ctx.measureText("" + xVal).width);
            }
            xLabelWidth += 3;
        }
        // Draw the X labels
        for (var i = 0; i <= nbXLabels; i++)
        {
            var xVal = Math.round((minY + (maxY - minY) * i / nbXLabels) * 100) / 100;
            if ((maxY - minY) > 10)
                xVal = Math.round(xVal);
            ctx.fillText("" + xVal, xLabelWidth - ctx.measureText("" + xVal).width, this.TransformY(xVal, minY, maxY) + (i == 0 ? 0 : 6));
        }

        // Draw the origins
        ctx.strokeStyle = "#000000";
        ctx.beginPath();
        ctx.moveTo(xLabelWidth + 2.5, 0);
        ctx.lineTo(xLabelWidth + 2.5, Math.round(this.TransformY(minY, minY, maxY)) + 0.5);
        ctx.lineTo(this.width, Math.round(this.TransformY(minY, minY, maxY)) + 0.5);
        ctx.stroke();
        // Draw the horizontal grid
        ctx.beginPath();
        ctx.setLineDash([2, 2]);
        for (var i = 1; i <= nbXLabels; i++)
        {
            var y = Math.round(this.TransformY(Math.round((minY + (maxY - minY) * i / nbXLabels) * 100) / 100, minY, maxY));
            ctx.moveTo(xLabelWidth + 2.5, y + 0.5);
            ctx.lineTo(this.width, y + 0.5);
        }
        ctx.stroke();

        // Draw the Y labels
        var nbYLabels = 3;
        for (var i = 0; i <= nbYLabels; i++)
        {
            var x = (this.width - (xLabelWidth + 2)) * i / nbYLabels + xLabelWidth;
            var label = this.dataSource.Values[Math.round(i * (this.dataSource.Values.length - 1) / nbYLabels)].Label;
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
            var x = Math.round((this.width - (xLabelWidth + 2)) * i / nbYLabels + xLabelWidth);
            ctx.moveTo(x + 0.5, 0);
            ctx.lineTo(x + 0.5, this.TransformY(minY, minY, maxY));
        }
        ctx.stroke();

        // Let's plot the area of the values
        ctx.beginPath();
        ctx.globalAlpha = 0.1;
        ctx.fillStyle = this.plotColor;
        for (var i = 0; i < this.dataSource.Values.length; i++)
        {
            var y = this.TransformY(this.dataSource.Values[i].Value, minY, maxY);
            var x = this.TransformX(i, xLabelWidth + 2);
            if (i == 0)
            {
                ctx.lineTo(x, this.TransformY(0, minY, maxY));
                ctx.lineTo(x, y);
            }
            else
                ctx.lineTo(x, y);
            if (i == this.dataSource.Values.length - 1)
                ctx.lineTo(x, this.TransformY(0, minY, maxY));
        }
        ctx.fill();
        ctx.globalAlpha = 1;

        // Let's plot the values
        ctx.beginPath();
        ctx.setLineDash([]);
        ctx.strokeStyle = this.plotColor;
        ctx.lineWidth = this.plotLineWidth;
        for (var i = 0; i < this.dataSource.Values.length; i++)
        {
            var y = this.TransformY(this.dataSource.Values[i].Value, minY, maxY);
            var x = this.TransformX(i, xLabelWidth + 2);
            if (i == 0)
                ctx.moveTo(x, y);
            else
                ctx.lineTo(x, y);
        }
        ctx.stroke();
    }

    // Transforms the X value in the screen coordinate
    private TransformX(x: number, xLabelWidth: number)
    {
        return x * (this.width - xLabelWidth) / this.dataSource.Values.length + xLabelWidth;
    }

    // Transforms the Y value in the screen coordinate
    private TransformY(y: number, minY: number, maxY: number): number
    {
        return (this.height - (this.fontSize + 1) * 3) - (y - minY) * (this.height - (this.fontSize + 1) * 3) / (maxY - minY) + (this.fontSize + 1) * 1;
    }
}