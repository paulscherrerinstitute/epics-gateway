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

        public event AnomalyRangeEvent ThinSpikeFound;

        #endregion Events

        private readonly LinkedList<AveragedTemporalValue> AveragedValues = new LinkedList<AveragedTemporalValue>();
        private readonly List<RawTemporalValue> RawValues = new List<RawTemporalValue>();
        private AnomalyRange UnfinishedAnomaly = null;
        private int LastSign = 0;
        private double CumulativeValue = 0;
        private DateTime? StartOfCumulativeValue = null;

        public int NumRawPerAveragedValues { get; private set; }
        public int MaxNumAveragedValues { get; private set; }

        public int StandardDeviationThreshold { get; set; } = 5;
        public int AveragedValueThreshold { get; set; } = 5;
        public TimeSpan MinAnomalyDuration { get; set; } = TimeSpan.FromSeconds(70);

        public TimeSpan GroupingSpan { get; set; } = TimeSpan.FromMinutes(2);

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

                if (last != null)
                {
                    var previousAveragedValue = last.Value;
                    var stdevDiff = previousAveragedValue.StandardDeviation - averagedValue.StandardDeviation;
                    var valueDiff = previousAveragedValue.Value - averagedValue.Value;
                    var currentSign = Math.Sign(valueDiff);
                    var absValueDiff = Math.Abs(valueDiff);

                    var thinSpike = averagedValue.StandardDeviation > averagedValue.Value;

                    if (thinSpike)
                    {
                        ThinSpikeFound?.Invoke(new AnomalyRange(averagedValue.Date.AddSeconds(-2.5), averagedValue.Date.AddSeconds(2.5)));
                        LastSign = currentSign;
                        CumulativeValue = absValueDiff;
                        StartOfCumulativeValue = averagedValue.Date;
                    }
                    else
                    {
                        if (Math.Abs(stdevDiff) > StandardDeviationThreshold)
                        {
                            TriggerFound?.Invoke(new AnomalyRange(previousAveragedValue.Date, averagedValue.Date));
                        }

                        if (absValueDiff > AveragedValueThreshold)
                        {
                            TriggerFound?.Invoke(new AnomalyRange(previousAveragedValue.Date, averagedValue.Date));
                        }

                        if (currentSign == LastSign && absValueDiff >= 0.25)
                        {
                            CumulativeValue += absValueDiff;
                            if (StartOfCumulativeValue == null)
                                StartOfCumulativeValue = previousAveragedValue.Date;
                            if (CumulativeValue >= AveragedValueThreshold)
                            {
                                TriggerFound?.Invoke(new AnomalyRange(StartOfCumulativeValue.Value, averagedValue.Date));
                            }
                        }
                        else
                        {
                            LastSign = currentSign;
                            CumulativeValue = absValueDiff;
                            StartOfCumulativeValue = previousAveragedValue.Date;
                        }
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
            if (UnfinishedAnomaly != null)
                TryFinishAnomaly();
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