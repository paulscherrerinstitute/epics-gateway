using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphAnomalies
{
    public class AnomalyDetector
    {
        #region Events

        public delegate void AnomalyRangeEvent(AnomalyRange range);

        public event AnomalyRangeEvent AnomalyDetected;

        public event AnomalyRangeEvent TriggerFound;

        #endregion Events

        private readonly LinkedList<AveragedTemporalValue> AveragedValues = new LinkedList<AveragedTemporalValue>();
        private readonly List<RawTemporalValue> RawValues = new List<RawTemporalValue>();
        private AnomalyRange UnfinishedAnomaly = null;
        private int LastSign = 0;
        private double CumulativeValue = 0;

        public int NumRawPerAveragedValues { get; private set; }
        public int MaxNumAveragedValues { get; private set; }

        public int StandardDeviationThreshold { get; set; } = 5;
        public int AveragedValueThreshold { get; set; } = 5;
        public TimeSpan MinAnomalyDuration { get; set; } = TimeSpan.FromSeconds(70);

        public TimeSpan GroupingSpan { get; set; } = TimeSpan.FromMinutes(5);

        public AnomalyDetector(int numRawPerAveragedValues, int maxNumAveragedValues)
        {
            NumRawPerAveragedValues = numRawPerAveragedValues;
            MaxNumAveragedValues = maxNumAveragedValues;

            TriggerFound += ProcessTrigger;
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
                        TriggerFound?.Invoke(new AnomalyRange(last.Value.Date, averagedValue.Date));
                    }

                    var valueDiff = last.Value.Value - averagedValue.Value;
                    if (Math.Abs(valueDiff) > AveragedValueThreshold)
                    {
                        TriggerFound?.Invoke(ExpandValueRange(new AnomalyRange(last.Value.Date, averagedValue.Date)));
                    }

                    if(Math.Sign(valueDiff) == LastSign)
                    {
                        CumulativeValue += Math.Abs(valueDiff);
                        if(CumulativeValue >= AveragedValueThreshold)
                        {
                            TriggerFound?.Invoke(ExpandValueRange(new AnomalyRange(last.Value.Date, averagedValue.Date)));
                            CumulativeValue = 0;
                        }
                    }
                    else
                    {
                        LastSign = Math.Sign(valueDiff);
                        CumulativeValue = Math.Abs(valueDiff);
                    }
                }

                // Trigger if there was no trigger for longer than the GroupingSpan
                if (UnfinishedAnomaly != null)
                {
                    if (UnfinishedAnomaly.To + GroupingSpan < averagedValue.Date)
                    {
                        TryFinishAnomaly();
                        UnfinishedAnomaly = null;
                    }
                }
            }
        }

        private AnomalyRange ExpandValueRange(AnomalyRange range)
        {
            var from = range.From;
            var start = AveragedValues.Last;
            while(start.Previous != null && start.Value.Date != from)
                start = start.Previous;
            var startValue = start.Value.Value;
            while (start.Previous != null && start.Value.Value < startValue - 0.5)
            {
                from = start.Value.Date;
                startValue = start.Value.Value;
                start = start.Previous;
            }

            var to = range.To;
            var last = AveragedValues.First;
            while (last.Next != null && last.Value.Date != to)
                last = last.Next;
            var lastValue = last.Value.Value;
            while (last.Next != null && last.Value.Value < startValue - 0.5)
            {
                to = last.Value.Date;
                lastValue = last.Value.Value;
                last = last.Next;
            }
            return new AnomalyRange(from, to);
        }

        private void ProcessTrigger(AnomalyRange range)
        {
            if(UnfinishedAnomaly == null)
            {
                UnfinishedAnomaly = range;
            }
            else
            {
                // If they overlap, extend the current anomaly
                if(UnfinishedAnomaly.To + GroupingSpan >= range.From)
                {
                    if(range.To > UnfinishedAnomaly.To)
                        UnfinishedAnomaly.To = range.To;
                }
                // If they don't, send the anomaly and use the new range for the new anomaly
                else
                {
                    TryFinishAnomaly();
                    UnfinishedAnomaly = range;
                }
            }
        }

        private void TryFinishAnomaly()
        {
            if(UnfinishedAnomaly.To - UnfinishedAnomaly.From >= MinAnomalyDuration)
            {
                AnomalyDetected?.Invoke(UnfinishedAnomaly);
            }
        }

        public void Finish()
        {
            if(UnfinishedAnomaly != null)
                AnomalyDetected?.Invoke(UnfinishedAnomaly);
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