using UnityEngine;

namespace RhythmParkour
{
    public sealed class DynamicTempoState
    {
        public const float NormalScale = 1f;

        private readonly float missPenalty;
        private readonly float successRecovery;
        private readonly float distortedThreshold;
        private readonly float failedThreshold;

        public DynamicTempoState(
            float missPenalty = 0.03f,
            float successRecovery = 0.01f,
            float distortedThreshold = 0.9f,
            float failedThreshold = 0.75f)
        {
            this.missPenalty = Mathf.Max(0f, missPenalty);
            this.successRecovery = Mathf.Max(0f, successRecovery);
            this.distortedThreshold = distortedThreshold;
            this.failedThreshold = failedThreshold;
            Scale = NormalScale;
        }

        public float Scale { get; private set; }

        public bool IsDistorted => Scale < distortedThreshold;

        public bool IsFailed => Scale < failedThreshold;

        public void RegisterMiss()
        {
            Scale = Mathf.Max(0f, Scale - missPenalty);
        }

        public void RegisterSuccess()
        {
            Scale = Mathf.Min(NormalScale, Scale + successRecovery);
        }

        public void Reset()
        {
            Scale = NormalScale;
        }
    }
}
