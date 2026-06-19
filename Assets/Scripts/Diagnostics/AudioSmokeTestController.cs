using UnityEngine;
using RhythmParkour;

namespace BoringRun.Diagnostics
{
    public sealed class AudioSmokeTestController : MonoBehaviour
    {
        [SerializeField] AudioClip musicClip;
        [SerializeField, Range(0f, 1f)] float volume = 1f;
        [SerializeField] KeyCode playMusicKey = KeyCode.Space;
        [SerializeField] KeyCode playToneKey = KeyCode.T;

        AudioSource musicSource;
        AudioSource toneSource;
        string status = "Press Space for music, T for generated tone.";

        void Awake()
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f;
            musicSource.volume = volume;
            musicSource.clip = musicClip;

            toneSource = gameObject.AddComponent<AudioSource>();
            toneSource.playOnAwake = false;
            toneSource.spatialBlend = 0f;
            toneSource.volume = volume;
            toneSource.clip = CreateToneClip();
        }

        void Update()
        {
            musicSource.volume = volume;
            toneSource.volume = volume;

            if (NewInputKeyboard.WasPressedThisFrame(playMusicKey))
                PlayMusic();

            if (NewInputKeyboard.WasPressedThisFrame(playToneKey))
                PlayTone();
        }

        public void PlayMusic()
        {
            if (musicSource.clip == null)
            {
                status = "Music clip is missing.";
                return;
            }

            musicSource.Stop();
            musicSource.time = 0f;
            musicSource.Play();
            status = $"Playing music: {musicSource.clip.name}";
        }

        public void PlayTone()
        {
            toneSource.Stop();
            toneSource.time = 0f;
            toneSource.Play();
            status = "Playing generated 440 Hz tone.";
        }

        void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 26,
                normal = { textColor = Color.white }
            };

            GUI.Label(new Rect(24, 24, 1200, 36), "Audio Smoke Test", style);
            GUI.Label(new Rect(24, 64, 1200, 36), status, style);
            GUI.Label(new Rect(24, 104, 1200, 36), $"Music playing: {musicSource != null && musicSource.isPlaying}    Tone playing: {toneSource != null && toneSource.isPlaying}    Listener volume: {AudioListener.volume:0.00}    Paused: {AudioListener.pause}", style);

            if (GUI.Button(new Rect(24, 156, 220, 48), "Play Music"))
                PlayMusic();

            if (GUI.Button(new Rect(264, 156, 220, 48), "Play Tone"))
                PlayTone();
        }

        static AudioClip CreateToneClip()
        {
            const int sampleRate = 48000;
            const float frequency = 440f;
            const float durationSeconds = 2f;
            var sampleCount = Mathf.CeilToInt(sampleRate * durationSeconds);
            var data = new float[sampleCount];

            for (var i = 0; i < sampleCount; i++)
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate) * 0.25f;

            var clip = AudioClip.Create("Generated 440 Hz Tone", sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
