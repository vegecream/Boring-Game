using System.Collections.Generic;
using System.Linq;

namespace RhythmParkour
{
    public sealed class SingleButtonChartJudge
    {
        private readonly IReadOnlyList<RhythmActionEvent> events;
        private readonly float hitWindowBeats;
        private int nextEventIndex;

        public SingleButtonChartJudge(IEnumerable<RhythmActionEvent> events, float hitWindowBeats)
        {
            this.events = (events ?? Enumerable.Empty<RhythmActionEvent>())
                .Where(evt => evt != null)
                .OrderBy(evt => evt.Beat)
                .ToArray();
            this.hitWindowBeats = hitWindowBeats < 0f ? 0f : hitWindowBeats;
        }

        public int SuccessCount { get; private set; }

        public int MissCount { get; private set; }

        public bool IsComplete => nextEventIndex >= events.Count;

        public RhythmActionEvent NextEvent => IsComplete ? null : events[nextEventIndex];

        public bool TryHit(float currentBeat, out RhythmActionEvent hitEvent)
        {
            hitEvent = null;

            if (IsComplete)
            {
                return false;
            }

            var nextEvent = events[nextEventIndex];
            if (currentBeat < nextEvent.Beat - hitWindowBeats || currentBeat > nextEvent.Beat + hitWindowBeats)
            {
                return false;
            }

            hitEvent = nextEvent;
            nextEventIndex++;
            SuccessCount++;
            return true;
        }

        public int ConsumeMisses(float currentBeat)
        {
            var missed = 0;

            while (!IsComplete && currentBeat > events[nextEventIndex].Beat + hitWindowBeats)
            {
                nextEventIndex++;
                MissCount++;
                missed++;
            }

            return missed;
        }
    }
}
