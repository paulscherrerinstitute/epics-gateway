using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphAnomalies
{
    public class AnomalyDetector
    {
        public delegate void AnomalyDetectedEvent(AnomalyRange range);

        public event AnomalyDetectedEvent AnomalyDetected;

        private readonly LinkedList<AveragedTemporalValue> AveragedValues = new LinkedList<AveragedTemporalValue>();
        private readonly List<RawTemporalValue> RawValues = new List<RawTemporalValue>();
        private AnomalyRange UnfinishedAnomaly = null;
        private int LastSign = 0;
        private double CumulativeValue = 0;

        public int NumRawPerAveragedValues { get; private set; }
        public int MaxNumAveragedValues { get; private set; }

        public int StandardDeviationThreshold { get; set; } = 5;
        public int AveragedValueThreshold { get; set; } = 5;
        public TimeSpan MinAnomalyDuration { get; set; } = TimeSpan.FromSeconds(55);

        public TimeSpan GroupingSpan { get; set; } = TimeSpan.FromMinutes(1);

        public AnomalyDetector(int numRawPerAveragedValues, int maxNumAveragedValues)
        {
            NumRawPerAveragedValues = numRawPerAveragedValues;
            MaxNumAveragedValues = maxNumAveragedValues;
        }

        public void Update(DateTime dateTime, double value)
        {
            var rawValue = new RawTemporalValue(dateTime, value);
            RawValues.Add(rawValue);
            if(RawValues.Count >= NumRawPerAveragedValues)
            {
                var averagedValue = new AveragedTemporalValue()
                {
                    Date = RawValues.First().Date,
                    Value = RawValues.Average(v => v.Value),
                };
                averagedValue.StandardDeviation = Math.Sqrt(RawValues.Sum(v => (v.Value - averagedValue.Value) * (v.Value - averagedValue.Value)) / RawValues.Count);

                RawValues.Clear();

                var last = AveragedValues.Last;
                AveragedValues.AddLast(averagedValue);
                while (AveragedValues.Count > MaxNumAveragedValues)
                    AveragedValues.RemoveFirst();

                if (last != null) {
                    var stdevDiff = last.Value.StandardDeviation - averagedValue.StandardDeviation;
                    if (Math.Abs(stdevDiff) > StandardDeviationThreshold)
                    {
                        TriggerForRange(new AnomalyRange(last.Value.Date, averagedValue.Date));
                    }

                    var valueDiff = last.Value.Value - averagedValue.Value;
                    if (Math.Abs(valueDiff) > AveragedValueThreshold)
                    {
                        TriggerForRange(new AnomalyRange(last.Value.Date, averagedValue.Date));
                    }

                    if(Math.Sign(valueDiff) == LastSign)
                    {
                        CumulativeValue += Math.Abs(valueDiff);
                        if(CumulativeValue >= AveragedValueThreshold)
                        {
                            TriggerForRange(new AnomalyRange(last.Value.Date, averagedValue.Date));
                            CumulativeValue = 0;
                        }
                    }
                    else
                    {
                        LastSign = Math.Sign(valueDiff);
                        CumulativeValue = Math.Abs(valueDiff);
                    }
                }

                TriggerForDate(averagedValue.Date);
            }
        }

        private void TriggerForDate(DateTime dateTime)
        {
            if (UnfinishedAnomaly != null)
            {
                if (UnfinishedAnomaly.To + GroupingSpan < dateTime)
                {
                    CheckAnomalyDuration(UnfinishedAnomaly);
                    UnfinishedAnomaly = null;
                }
            }
        }

        private void TriggerForRange(AnomalyRange range)
        {
            if(UnfinishedAnomaly == null)
            {
                UnfinishedAnomaly = range;
            }
            else
            {
                if(UnfinishedAnomaly.To + GroupingSpan >= range.From)
                {
                    UnfinishedAnomaly.To = range.To;
                }
                else
                {
                    CheckAnomalyDuration(UnfinishedAnomaly);
                    UnfinishedAnomaly = range;
                }
            }
        }

        private void CheckAnomalyDuration(AnomalyRange range)
        {
            if(range.To - range.From >= MinAnomalyDuration)
            {
                AnomalyDetected?.Invoke(range);
            }
        }

        public List<AveragedTemporalValue> ReadonlyAveragedValues
        {
            get { return AveragedValues.Select(v => new AveragedTemporalValue(v)).ToList(); }
        }
    }

    public enum AnomalyDetectorState
    {
        Normal,
        AnomalyRising,
        AnomalySinking,
        AnomalySteady,
    }
}