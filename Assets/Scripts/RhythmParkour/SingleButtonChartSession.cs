using System.Collections.Generic;

namespace RhythmParkour
{
    public enum SingleButtonChartInputKind
    {
        Hit,
        BadTiming
    }

    public readonly struct SingleButtonChartInputResult
    {
        public SingleButtonChartInputResult(SingleButtonChartInputKind kind, RhythmActionEvent evt)
        {
            Kind = kind;
            Event = evt;
        }

        public SingleButtonChartInputKind Kind { get; }

        public RhythmActionEvent Event { get; }
    }

    public sealed class SingleButtonChartSession
    {
        private readonly SingleButtonChartJudge judge;
        private readonly DynamicTempoState tempoState = new DynamicTempoState();

        public SingleButtonChartSession(IEnumerable<RhythmActionEvent> events, float hitWindowBeats)
        {
            judge = new SingleButtonChartJudge(events, hitWindowBeats);
        }

        public int SuccessCount => judge.SuccessCount;

        public int MissCount => judge.MissCount;

        public float TempoScale => tempoState.Scale;

        public float DistortionAmount
        {
            get
            {
                const float startScale = 0.9f;
                const float fullScale = 0.75f;
                var amount = (startScale - TempoScale) / (startScale - fullScale);

                if (amount < 0f)
                {
                    return 0f;
                }

                return amount > 1f ? 1f : amount;
            }
        }

        public bool IsDistorted => tempoState.IsDistorted;

        public bool IsFailed => tempoState.IsFailed;

        public RhythmActionEvent NextEvent => judge.NextEvent;

        public static float GetDropoutPulse(float distortionAmount, float phase)
        {
            if (distortionAmount <= 0f)
            {
                return 0f;
            }

            return phase < distortionAmount * 0.18f ? 1f : 0f;
        }

        public SingleButtonChartInputResult Press(float currentBeat)
        {
            if (judge.TryHit(currentBeat, out var hitEvent))
            {
                tempoState.RegisterSuccess();
                return new SingleButtonChartInputResult(SingleButtonChartInputKind.Hit, hitEvent);
            }

            tempoState.RegisterMiss();
            return new SingleButtonChartInputResult(SingleButtonChartInputKind.BadTiming, null);
        }

        public int ConsumeMisses(float currentBeat)
        {
            var missed = judge.ConsumeMisses(currentBeat);
            for (var i = 0; i < missed; i++)
            {
                tempoState.RegisterMiss();
            }

            return missed;
        }
    }
}
