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

        public event AnomalyRangeEvent UnexpectedDetected;

        public event AnomalyRangeEvent RiseDetected;

        public event AnomalyRangeEvent FallDetected;

        #endregion Events

        #region Configuration

        public int NumRawPerAveragedValues { get; set; } = 7;
        public int MaxNumAveragedValues { get; set; } = 100;

        public int CumulativeValueThreshold { get; set; } = 7;
        public TimeSpan MinAnomalyDuration { get; set; } = TimeSpan.FromMinutes(5); //.Add(TimeSpan.FromSeconds(30));
        public TimeSpan AnomalyGroupingTimeout { get; set; } = TimeSpan.FromMinutes(3);
        public double ValueCutoff { get; set; } = 0.125;

        #endregion Configuration

        public List<AveragedTemporalValue> ReadonlyAveragedValues => AveragedValues.Select(v => new AveragedTemporalValue(v)).ToList();
        private readonly LinkedList<AveragedTemporalValue> AveragedValues = new LinkedList<AveragedTemporalValue>();
        private readonly List<RawTemporalValue> RawValues = new List<RawTemporalValue>();

        public AnomalyDetector()
        {
            RiseDetected += RiseHandler;
            FallDetected += FallHandler;
            UnexpectedDetected += range => VerifyAndTriggerAnomaly(new AnomalyRange(range), hasGap: false);
        }

        private int LastSign = -2;
        private double CumulativeValue = 0;
        private bool ReachedCumulativeThreshold = false;
        private AnomalyRange CurrentCumulativeRange = null;

        public void Update(DateTime dateTime, double value)
        {
            var rawValue = new RawTemporalValue(dateTime, value);
            RawValues.Add(rawValue);
            if (RawValues.Count >= NumRawPerAveragedValues)
            {
                var averagedValue = new AveragedTemporalValue()
                {
                    Date = RawValues.First().Date,
                    Value = RawValues.Average(v => v.Value),
                };
                averagedValue.StandardDeviation = Math.Sqrt(RawValues.Sum(v => (v.Value - averagedValue.Value) * (v.Value - averagedValue.Value)) / (RawValues.Count - 1));

                RawValues.Clear();

                var last = AveragedValues.Last;
                AveragedValues.AddLast(averagedValue);
                while (AveragedValues.Count > MaxNumAveragedValues)
                    AveragedValues.RemoveFirst();

                if (last == null)
                    return;

                var previousAveragedValue = last.Value;
                var valueDiff = averagedValue.Value - previousAveragedValue.Value;
                var currentSign = Math.Sign(valueDiff);
                var absValueDiff = Math.Abs(valueDiff);

                if (currentSign == LastSign && absValueDiff >= ValueCutoff)
                {
                    CumulativeValue += absValueDiff;
                    if (CumulativeValue >= CumulativeValueThreshold)
                    {
                        ReachedCumulativeThreshold = true;
                        if (CurrentCumulativeRange.To < averagedValue.Date)
                            CurrentCumulativeRange.To = averagedValue.Date;
                    }
                }
                else
                {
                    if (ReachedCumulativeThreshold)
                    {
                        if (LastSign == 1)
                            RiseDetected?.Invoke(CurrentCumulativeRange);
                        else if (LastSign == -1)
                            FallDetected?.Invoke(CurrentCumulativeRange);
                    }

                    LastSign = currentSign;
                    CumulativeValue = absValueDiff;
                    ReachedCumulativeThreshold = false;
                    if (absValueDiff >= CumulativeValueThreshold)
                        ReachedCumulativeThreshold = true;
                    CurrentCumulativeRange = new AnomalyRange(previousAveragedValue.Date, averagedValue.Date);
                }

                CheckTimeouts(averagedValue);
            }
        }

        private AnomalyRange LastRise = null;

        private void RiseHandler(AnomalyRange range)
        {
            if (LastRise == null)
            {
                LastRise = range;
                VerifyAndTriggerAnomaly(new AnomalyRange(range), false);
            }
            else
            {
                UnexpectedDetected?.Invoke(range);
            }
        }

        private void FallHandler(AnomalyRange range)
        {
            if (LastRise != null)
            {
                VerifyAndTriggerAnomaly(new AnomalyRange(LastRise.From, range.To), hasGap: LastRise.To < range.From);
                LastRise = null;
            }
            else
            {
                UnexpectedDetected?.Invoke(range);
            }
        }

        private void CheckTimeouts(AveragedTemporalValue averagedValue)
        {
            // Rise without fall should trigger anomaly after timeout
            if (LastRise != null && LastRise.To + AnomalyGroupingTimeout < averagedValue.Date)
            {
                VerifyAndTriggerAnomaly(new AnomalyRange(LastRise.From, averagedValue.Date), hasGap: true);
                LastRise = null;
            }

            if (LastAnomaly != null && LastAnomaly.To + AnomalyGroupingTimeout < averagedValue.Date)
            {
                if (LastAnomalyHasGaps || LastAnomaly.To - LastAnomaly.From >= MinAnomalyDuration)
                {
                    AnomalyDetected?.Invoke(LastAnomaly);
                    LastAnomaly = null;
                    LastAnomalyHasGaps = false;
                }
            }
        }

        private AnomalyRange LastAnomaly = null;
        private bool LastAnomalyHasGaps = false;

        private void VerifyAndTriggerAnomaly(AnomalyRange range, bool hasGap)
        {
            if(LastAnomaly != null)
            {
                // Check if the anomaly has passed enough time wihout another
                if(LastAnomaly.To + AnomalyGroupingTimeout < range.From)
                {
                    if(LastAnomalyHasGaps || LastAnomaly.To - LastAnomaly.From >= MinAnomalyDuration)
                        AnomalyDetected?.Invoke(LastAnomaly);
                    LastAnomaly = range;
                    LastAnomalyHasGaps = hasGap;
                }
                else
                {
                    if (hasGap)
                        LastAnomalyHasGaps = true;

                    LastAnomaly.From = LastAnomaly.From < range.From ? LastAnomaly.From : range.From;
                    LastAnomaly.To = LastAnomaly.To > range.To ? LastAnomaly.To : range.To;
                }
            }
            else
            {
                LastAnomaly = range;
                LastAnomalyHasGaps = hasGap;
            }
        }
    }
}