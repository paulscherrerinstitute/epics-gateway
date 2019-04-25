using GraphAnomalies;
using GWLogger.Live;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;

namespace GraphAnomalyVisualizer
{
    internal class MainWindowViewModel
    {
        public MainWindowViewModel(string path)
        {
            var success = false;
            var attempts = 0;
            do
            {
                try
                {
                    using (var file = File.OpenRead(path))
                    {
                        var serializer = new XmlSerializer(typeof(GraphAnomaly));
                        var graphAnomalyFromFile = (GraphAnomaly)serializer.Deserialize(file);

                        var model = new PlotModel { Title = "Anomaly: " + Path.GetFileNameWithoutExtension(path) };

                        var cpuMax = 110;
                        var cpuMin = 0;
                        model.Axes.Add(new LinearAxis()
                        {
                            Position = AxisPosition.Left,
                            AbsoluteMaximum = cpuMax,
                            AbsoluteMinimum = cpuMin,
                            Maximum = cpuMax,
                            Minimum = cpuMin,
                            IsPanEnabled = false,
                            IsZoomEnabled = false,
                        });

                        model.Axes.Add(new DateTimeAxis() { Position = AxisPosition.Bottom });

                        var detector = new AnomalyDetector();
                        var anomalies = new List<AnomalyRange>();
                        var rises = new List<AnomalyRange>();
                        var falls = new List<AnomalyRange>();
                        var unexpected = new List<AnomalyRange>();
                        detector.AnomalyDetected += anomalies.Add;
                        detector.RiseDetected += rises.Add;
                        detector.FallDetected += falls.Add;
                        detector.UnexpectedDetected += unexpected.Add;

                        foreach (var v in graphAnomalyFromFile.History.CPU)
                            detector.Update(v.Date, v.Value ?? -1);
                        detector.Finish();

                        foreach (var v in rises)
                            AddArea(model, null, OxyColor.FromAColor(60, OxyColors.Green), v.From, v.To, 0, 100);
                        foreach (var v in falls)
                            AddArea(model, null, OxyColor.FromAColor(60, OxyColors.Red), v.From, v.To, 0, 100);
                        foreach (var v in unexpected)
                            AddArea(model, null, OxyColor.FromAColor(60, OxyColors.Fuchsia), v.From, v.To, 0, 100);

                        foreach (var anomaly in anomalies)
                            AddArea(model, null, OxyColor.FromAColor(120, OxyColors.Orange), anomaly.From, anomaly.To, 100, 110);

                        AddPlotLine(model, "Actual data", OxyColors.Gray, graphAnomalyFromFile.History.CPU);
                        AddPlotLine(model, "Averaged values", OxyColors.BlueViolet, detector.ReadonlyAveragedValues.Select(ToHistoricData));
                        AddPlotLine(model, "Standard deviation", OxyColors.DeepSkyBlue, detector.ReadonlyAveragedValues.Select(v => new HistoricData
                        {
                            Date = v.Date,
                            Value = v.StandardDeviation,
                        }));
                        Plot = model;
                    }
                }
                catch (IOException)
                {
                    attempts++;
                    if (attempts >= 5)
                        throw new Exception("Cannot open file in use.");
                    Thread.Sleep(100);
                    continue;
                }
                success = true;
            }
            while (!success);
        }

        public PlotModel Plot { get; private set; }

        public static PlotModel AddPlotLine(PlotModel model, string title, OxyColor color, IEnumerable<HistoricData> entries)
        {
            var line = new LineSeries { Color = color, LineStyle = LineStyle.Solid };
            foreach (var entry in entries)
            {
                line.Points.Add(DateTimeAxis.CreateDataPoint(entry.Date, entry.Value ?? -1));
            }
            model.Series.Add(line);
            return model;
        }

        private void AddArea(PlotModel model, string title, OxyColor color, DateTime from, DateTime to, int min, int max)
        {
            var series = new AreaSeries
            {
                Title = title,
                Fill = color,
                StrokeThickness = 0,
            };

            var before = from.AddMilliseconds(-10);
            var after = to.AddMilliseconds(10);
            series.Points.Add(DateTimeAxis.CreateDataPoint(before, 0));
            series.Points2.Add(DateTimeAxis.CreateDataPoint(before, 0));
            series.Points.Add(DateTimeAxis.CreateDataPoint(from, min));
            series.Points2.Add(DateTimeAxis.CreateDataPoint(from, max));
            series.Points.Add(DateTimeAxis.CreateDataPoint(to, min));
            series.Points2.Add(DateTimeAxis.CreateDataPoint(to, max));
            series.Points.Add(DateTimeAxis.CreateDataPoint(after, 0));
            series.Points2.Add(DateTimeAxis.CreateDataPoint(after, 0));
            model.Series.Add(series);
        }

        private HistoricData ToHistoricData(AveragedTemporalValue value)
        {
            return new HistoricData
            {
                Date = value.Date,
                Value = value.Value,
            };
        }
    }
}