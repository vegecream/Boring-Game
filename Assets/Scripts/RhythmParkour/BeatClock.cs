using UnityEngine;

namespace RhythmParkour
{
    public sealed class BeatClock
    {
        private const float MinimumTempoScale = 0.01f;

        public BeatClock(float baseBpm)
        {
            BaseBpm = Mathf.Max(1f, baseBpm);
        }

        public float BaseBpm { get; }

        public float GetSecondsPerBeat(float tempoScale)
        {
            var scaledBpm = BaseBpm * Mathf.Max(MinimumTempoScale, tempoScale);
            return 60f / scaledBpm;
        }
    }
}
