using UnityEngine;

namespace RhythmParkour
{
    [CreateAssetMenu(fileName = "RhythmTrackConfig", menuName = "Rhythm Parkour/Track Config")]
    public sealed class RhythmTrackConfig : ScriptableObject
    {
        [SerializeField] private AudioClip audioClip;
        [SerializeField] private float baseBpm = 120f;
        [SerializeField] private float firstBeatOffsetSeconds;
        [SerializeField] private RhythmActionChart chart;

        public AudioClip AudioClip => audioClip;

        public float BaseBpm => baseBpm;

        public float FirstBeatOffsetSeconds => firstBeatOffsetSeconds;

        public RhythmActionChart Chart => chart;

        public float GetSecondsAtBeat(float beat, float tempoScale)
        {
            var clock = new BeatClock(baseBpm);
            return firstBeatOffsetSeconds + Mathf.Max(0f, beat) * clock.GetSecondsPerBeat(tempoScale);
        }

        public void Configure(AudioClip audioClip, float baseBpm, float firstBeatOffsetSeconds, RhythmActionChart chart)
        {
            this.audioClip = audioClip;
            this.baseBpm = Mathf.Max(1f, baseBpm);
            this.firstBeatOffsetSeconds = Mathf.Max(0f, firstBeatOffsetSeconds);
            this.chart = chart;
        }

        public void ConfigureForTests(AudioClip audioClip, float baseBpm, float firstBeatOffsetSeconds, RhythmActionChart chart)
        {
            Configure(audioClip, baseBpm, firstBeatOffsetSeconds, chart);
        }

        private void OnValidate()
        {
            baseBpm = Mathf.Max(1f, baseBpm);
            firstBeatOffsetSeconds = Mathf.Max(0f, firstBeatOffsetSeconds);
        }
    }
}
