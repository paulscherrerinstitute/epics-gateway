using GraphAnomalies.Types;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Xml.Serialization;

namespace GraphAnomalyVisualizer
{
    public class GraphGeneratorViewModel : INotifyPropertyChanged
    {
        public PlotModel Graph { get; set; } = new PlotModel();
        private readonly List<TemporalValue> Values = new List<TemporalValue>();

        private int _BaseValue = 5;

        public int BaseValue
        {
            get { return _BaseValue; }
            set { SetValue(ref _BaseValue, value); }
        }

        private int _MaxStep = 10;

        public int MaxStep
        {
            get { return _MaxStep; }
            set { SetValue(ref _MaxStep, value);}
        }

        private double _Noise = 1;

        public double Noise
        {
            get { return _Noise; }
            set { SetValue(ref _Noise, value); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void SetValue<TValue>(ref TValue oldValue, TValue newValue, [CallerMemberName]string propertyName = null)
        {
            if (Equals(oldValue, newValue))
                return;
            oldValue = newValue;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            GenerateGraph();
        }

        public void ExportToFile(string fileName)
        {
            var serializer = new XmlSerializer(typeof(GraphAnomaly));
            using (var file = File.Create(fileName))
            {
                serializer.Serialize(file, new GraphAnomaly
                {
                    From = Values.First().Date,
                    To = Values.First().Date,
                    Name = Path.GetFileNameWithoutExtension(fileName).ToUpperInvariant(),
                    History = new GatewayHistoricData {
                        CPU = Values.Select(v => new HistoricData { Date = v.Date, Value = v.Value }).ToList(),
                    }
                });
            }
            MessageBox.Show("The graph was successfully exported.", "Exported", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public GraphGeneratorViewModel()
        {
            Graph.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                AbsoluteMaximum = 100,
                AbsoluteMinimum = 0,
                Maximum = 100,
                Minimum = 0,
                IsPanEnabled = false,
                IsZoomEnabled = false,
            });
            Graph.Axes.Add(new DateTimeAxis
            {
                Position = AxisPosition.Bottom,
                IsPanEnabled = false,
                IsZoomEnabled = false,
            });
            GenerateGraph();
        }

        public void GenerateGraph()
        {
            Graph.Series.Clear();
            Values.Clear();

            var line = new LineSeries { Color = OxyColors.Black };
            var date = DateTime.UtcNow.AddHours(-8);
            var rnd = new Random();
            double lastValue = BaseValue;
            for (var i = 0; i < 500;i++)
            {
                var step = rnd.Next(MaxStep) - MaxStep / 2;
                var noise = Noise * rnd.NextDouble() - Noise / 2;
                lastValue += step + noise;
                lastValue = Math.Max(0, Math.Min(100, lastValue));
                line.Points.Add(DateTimeAxis.CreateDataPoint(date, lastValue));
                Values.Add(new TemporalValue(date, lastValue));
                date = date.AddSeconds(5);
            }
            Graph.Series.Add(line);
            Graph.InvalidatePlot(true);
        }
    }
}