using System;
using UnityEngine;
using UnityEngine.Events;

namespace RhythmParkour
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class RhythmBeatManager : MonoBehaviour
    {
        [SerializeField] private float baseBpm = 120f;
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private bool logBeats;
        [SerializeField] private AudioSource musicSource;

        private BeatClock beatClock;
        private DynamicTempoState tempoState;
        private double nextBeatDspTime;
        private bool isRunning;

        public event Action<int> Beat;

        public UnityEvent<int> BeatUnityEvent;
        public UnityEvent<float> TempoScaleChanged;
        public UnityEvent DistortionStarted;
        public UnityEvent FailureReached;

        public int CurrentBeat { get; private set; }

        public float TempoScale => tempoState?.Scale ?? DynamicTempoState.NormalScale;

        public bool IsDistorted => tempoState?.IsDistorted ?? false;

        public bool IsFailed => tempoState?.IsFailed ?? false;

        private void Awake()
        {
            musicSource = musicSource != null ? musicSource : GetComponent<AudioSource>();
            beatClock = new BeatClock(baseBpm);
            tempoState = new DynamicTempoState();
        }

        private void Start()
        {
            if (playOnStart)
            {
                StartPlayback();
            }
        }

        private void Update()
        {
            if (!isRunning || IsFailed)
            {
                return;
            }

            if (musicSource != null)
            {
                musicSource.pitch = TempoScale;
            }

            var now = AudioSettings.dspTime;
            while (now >= nextBeatDspTime)
            {
                FireBeat();
                nextBeatDspTime += beatClock.GetSecondsPerBeat(TempoScale);
            }
        }

        public void StartPlayback()
        {
            CurrentBeat = 0;
            isRunning = true;
            nextBeatDspTime = AudioSettings.dspTime;

            if (musicSource != null && musicSource.clip != null)
            {
                musicSource.loop = true;
                musicSource.pitch = TempoScale;
                musicSource.Play();
            }
        }

        public void StopPlayback()
        {
            isRunning = false;

            if (musicSource != null)
            {
                musicSource.Stop();
            }
        }

        [ContextMenu("Register Success")]
        public void RegisterSuccess()
        {
            var wasDistorted = IsDistorted;
            tempoState.RegisterSuccess();
            TempoScaleChanged?.Invoke(TempoScale);

            if (wasDistorted && !IsDistorted && logBeats)
            {
                Debug.Log("[RhythmBeatManager] Tempo recovered from distortion.");
            }
        }

        [ContextMenu("Register Miss")]
        public void RegisterMiss()
        {
            var wasDistorted = IsDistorted;
            tempoState.RegisterMiss();
            TempoScaleChanged?.Invoke(TempoScale);

            if (!wasDistorted && IsDistorted)
            {
                DistortionStarted?.Invoke();
            }

            if (IsFailed)
            {
                FailureReached?.Invoke();
                StopPlayback();
            }
        }

        [ContextMenu("Reset Tempo")]
        public void ResetTempo()
        {
            tempoState.Reset();
            TempoScaleChanged?.Invoke(TempoScale);
        }

        private void FireBeat()
        {
            CurrentBeat++;
            Beat?.Invoke(CurrentBeat);
            BeatUnityEvent?.Invoke(CurrentBeat);

            if (logBeats)
            {
                Debug.Log($"[RhythmBeatManager] Beat {CurrentBeat} at tempo scale {TempoScale:0.00}");
            }
        }

        private void OnValidate()
        {
            baseBpm = Mathf.Max(1f, baseBpm);
        }
    }
}
