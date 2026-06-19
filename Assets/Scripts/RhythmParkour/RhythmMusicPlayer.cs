using UnityEngine;
using System.Collections;

namespace RhythmParkour
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class RhythmMusicPlayer : MonoBehaviour
    {
        [SerializeField] RhythmTrackConfig track;
        [SerializeField] bool playOnStart = true;
        [SerializeField] bool loop;
        [SerializeField, Range(0f, 1f)] float volume = 1f;

        AudioSource audioSource;
        Coroutine playRoutine;

        public RhythmTrackConfig Track => track;
        public AudioSource AudioSource => audioSource;

        public float CurrentBeat
        {
            get
            {
                if (track == null || audioSource == null)
                    return 0f;

                var secondsPerBeat = 60f / Mathf.Max(1f, track.BaseBpm);
                return (audioSource.time - track.FirstBeatOffsetSeconds) / secondsPerBeat;
            }
        }

        void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            ConfigureAudioSource();
        }

        void Start()
        {
            if (playOnStart)
                Play();
        }

        public void Play()
        {
            if (track == null || track.AudioClip == null)
            {
                Debug.LogWarning($"{nameof(RhythmMusicPlayer)} on {name} cannot play because the track or audio clip is missing.", this);
                return;
            }

            ConfigureAudioSource();
            if (playRoutine != null)
                StopCoroutine(playRoutine);

            playRoutine = StartCoroutine(PlayWhenClipLoaded());
        }

        public void Stop()
        {
            if (playRoutine != null)
            {
                StopCoroutine(playRoutine);
                playRoutine = null;
            }

            if (audioSource != null)
                audioSource.Stop();
        }

        IEnumerator PlayWhenClipLoaded()
        {
            var clip = audioSource.clip;
            if (clip == null)
                yield break;

            if (clip.loadState == AudioDataLoadState.Unloaded && !clip.LoadAudioData())
            {
                Debug.LogWarning($"{nameof(RhythmMusicPlayer)} on {name} could not start loading {clip.name}.", this);
                yield break;
            }

            while (clip.loadState == AudioDataLoadState.Loading)
                yield return null;

            if (clip.loadState != AudioDataLoadState.Loaded)
            {
                Debug.LogWarning($"{nameof(RhythmMusicPlayer)} on {name} cannot play {clip.name}; load state is {clip.loadState}.", this);
                yield break;
            }

            AudioListener.pause = false;
            if (AudioListener.volume <= 0f)
                AudioListener.volume = 1f;

            audioSource.time = 0f;
            audioSource.Play();
            Debug.Log($"{nameof(RhythmMusicPlayer)} playing {clip.name}. isPlaying={audioSource.isPlaying}, volume={audioSource.volume}, listenerVolume={AudioListener.volume}, listenerPaused={AudioListener.pause}, speakerMode={AudioSettings.speakerMode}", this);
            playRoutine = null;
        }

        void ConfigureAudioSource()
        {
            if (audioSource == null)
                return;

            audioSource.clip = track != null ? track.AudioClip : null;
            audioSource.playOnAwake = false;
            audioSource.loop = loop;
            audioSource.volume = volume;
            audioSource.pitch = 1f;
            audioSource.spatialBlend = 0f;
            audioSource.dopplerLevel = 0f;
        }
    }
}
