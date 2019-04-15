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

        public TimeSpan GroupingSpan { get; set; } = TimeSpan.FromMinutes(1);

        public AnomalyDetector(int numRawPerAveragedValues, int maxNumAveragedValues)
        {
            NumRawPerAveragedValues = numRawPerAveragedValues;
            MaxNumAveragedValues = maxNumAveragedValues;
        }

        public void Update(RawTemporalValue rawValue)
        {
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
                        var earliest = last;
                        while (earliest.Previous != null && Math.Sign(earliest.Previous.Value.StandardDeviation - earliest.Value.StandardDeviation) == Math.Sign(stdevDiff))
                            earliest = earliest.Previous;
                        TriggerForRange(new AnomalyRange(earliest.Value.Date, averagedValue.Date));
                    }

                    var valueDiff = last.Value.Value - averagedValue.Value;
                    if (Math.Abs(valueDiff) > AveragedValueThreshold)
                    {
                        var earliest = last;
                        while (earliest.Previous != null && Math.Sign(earliest.Previous.Value.Value - earliest.Value.Value) == Math.Sign(valueDiff))
                            earliest = earliest.Previous;
                        TriggerForRange( new AnomalyRange(earliest.Value.Date, averagedValue.Date));
                    }

                    if(Math.Sign(valueDiff) == LastSign)
                    {
                        CumulativeValue += Math.Abs(valueDiff);
                        if(CumulativeValue >= AveragedValueThreshold)
                        {
                            var earliest = last;
                            while (earliest.Previous != null && Math.Sign(earliest.Previous.Value.Value - earliest.Value.Value) == Math.Sign(valueDiff))
                                earliest = earliest.Previous;
                            TriggerForRange(new AnomalyRange(earliest.Value.Date, averagedValue.Date));
                            CumulativeValue = 0;
                        }
                    }
                    else
                    {
                        LastSign = Math.Sign(valueDiff);
                        CumulativeValue = 0;
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
                    AnomalyDetected?.Invoke(UnfinishedAnomaly);
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
                    AnomalyDetected?.Invoke(UnfinishedAnomaly);
                    UnfinishedAnomaly = range;
                }
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