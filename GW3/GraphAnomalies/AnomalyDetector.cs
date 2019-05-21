using GraphAnomalies.Types;
using System;

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

        public int CumulativeValueThreshold { get; set; } = 7;
        public TimeSpan MinAnomalyDuration { get; set; } = TimeSpan.FromMinutes(4);
        public TimeSpan AnomalyGroupingTimeout { get; set; } = TimeSpan.FromMinutes(5);
        public TimeSpan FlatTimeout { get; set; } = TimeSpan.FromMinutes(2);

        #endregion Configuration

        public readonly ProcessorChain PreProcessorChain;

        private TemporalValue? LastValue = null;

        public AnomalyDetector(ProcessorChain preProcessorChain)
        {
            PreProcessorChain = preProcessorChain;
            RiseDetected += RiseHandler;
            FallDetected += FallHandler;
            UnexpectedDetected += range => VerifyAndTriggerAnomaly(new AnomalyRange(range), hasGap: false);
            preProcessorChain.LastProcessor.ValueProcessed += HandleProcessedValue;
        }

        private int LastSign = -2;
        private double CumulativeValue = 0;
        private bool ReachedCumulativeThreshold = false;
        private AnomalyRange CurrentCumulativeRange = null;
        private DateTime? StartFlat = null;

        public void Update(TemporalValue rawValue)
        {
            PreProcessorChain.FirstProcessor.Update(rawValue);
        }

        public void HandleProcessedValue(TemporalValue processedValue)
        {
            if (LastValue == null)
            {
                LastValue = processedValue;
                return;
            }

            var previouslyProcessedValue = LastValue.Value;
            var valueDiff = processedValue.Value - previouslyProcessedValue.Value;
            var absValueDiff = Math.Abs(valueDiff);
            var currentSign = Math.Sign(valueDiff);

            if (currentSign == LastSign || currentSign == 0)
            {
                CumulativeValue += absValueDiff;
                if (CumulativeValue >= CumulativeValueThreshold)
                {
                    ReachedCumulativeThreshold = true;
                    if (CurrentCumulativeRange.To < processedValue.Date)
                        CurrentCumulativeRange.To = processedValue.Date;
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
                ReachedCumulativeThreshold = absValueDiff >= CumulativeValueThreshold;
                CurrentCumulativeRange = new AnomalyRange(previouslyProcessedValue.Date, processedValue.Date);
            }

            if (currentSign == 0)
            {
                if (!StartFlat.HasValue)
                    StartFlat = processedValue.Date;

                if(processedValue.Date - StartFlat >= FlatTimeout)
                {
                    if (ReachedCumulativeThreshold)
                    {
                        if (LastSign == 1)
                            RiseDetected?.Invoke(CurrentCumulativeRange);
                        else if (LastSign == -1)
                            FallDetected?.Invoke(CurrentCumulativeRange);
                    }

                    CumulativeValue = 0;
                    ReachedCumulativeThreshold = false;
                    CurrentCumulativeRange = new AnomalyRange(previouslyProcessedValue.Date, processedValue.Date);
                }
            }
            else
            {
                StartFlat = null;
            }

            CheckTimeouts(processedValue);

            LastValue = processedValue;
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

        private void CheckTimeouts(TemporalValue averagedValue)
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
                //VerifyAndTriggerAnomaly(LastAnomaly, LastAnomalyHasGaps);
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