using UnityEngine;

namespace RhythmParkour
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class SingleButtonChartPrototype : MonoBehaviour
    {
        [SerializeField] private RhythmTrackConfig track;
        [SerializeField] private KeyCode hitKey = KeyCode.Space;
        [SerializeField] private float hitWindowBeats = 0.45f;
        [SerializeField] private Transform cueObject;
        [SerializeField] private float unitsPerBeat = 2.5f;
        [SerializeField] private float startDelaySeconds;
        [SerializeField] private Transform cameraToFollow;
        [SerializeField] private float cameraLeadUnits = 10f;
        [SerializeField] private float minimumCameraZ = 12f;
        [SerializeField] private AudioSource noiseSource;
        [SerializeField] private float distortionNoiseVolume = 0.12f;
        [SerializeField] private AudioDistortionFilter musicDistortionFilter;
        [SerializeField] private float maxMusicDistortionLevel = 0.38f;
        [SerializeField] private float dropoutPulseRate = 9f;
        [SerializeField] private float dropoutVolumeFloor = 0.45f;
        [SerializeField] private float distortionSmoothingSeconds = 0.35f;

        private AudioSource audioSource;
        private SingleButtonChartSession session;
        private string feedback = "Press Space on each cue.";
        private Color feedbackColor = Color.white;
        private float feedbackUntil;
        private float smoothedDistortionAmount;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            musicDistortionFilter = musicDistortionFilter != null
                ? musicDistortionFilter
                : GetComponent<AudioDistortionFilter>();

            if (musicDistortionFilter == null)
            {
                musicDistortionFilter = gameObject.AddComponent<AudioDistortionFilter>();
            }

            musicDistortionFilter.distortionLevel = 0f;
        }

        private void Start()
        {
            Restart();
        }

        private void Update()
        {
            if (track == null || session == null)
            {
                return;
            }

            if (NewInputKeyboard.WasPressedThisFrame(KeyCode.R))
            {
                Restart();
                return;
            }

            var currentBeat = GetCurrentBeat();
            audioSource.pitch = session.TempoScale;
            UpdateDistortionAudio();

            var missed = session.ConsumeMisses(currentBeat);
            if (missed > 0)
            {
                SetFeedback($"MISS x{missed}  {DescribeTempo()}", Color.red);
            }

            if (NewInputKeyboard.WasPressedThisFrame(hitKey))
            {
                var result = session.Press(currentBeat);
                if (result.Kind == SingleButtonChartInputKind.Hit)
                {
                    SetFeedback($"HIT beat {result.Event.Beat:0.##} {result.Event.ActionType}  {DescribeTempo()}", Color.green);
                }
                else
                {
                    SetFeedback($"BAD TIMING  {DescribeTempo()}", Color.red);
                }
            }

            UpdateCueVisual(currentBeat);
        }

        private void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                normal = { textColor = Color.white }
            };

            GUI.Label(new Rect(24, 20, 900, 40), "Single Button Chart Prototype", style);
            GUI.Label(new Rect(24, 58, 900, 40), $"Key: {hitKey}    Restart: R    Window: +/-{hitWindowBeats:0.00} beat", style);

            if (track == null || session == null)
            {
                GUI.Label(new Rect(24, 100, 900, 40), "No track assigned.", style);
                return;
            }

            GUI.Label(new Rect(24, 100, 900, 40), $"Beat: {GetCurrentBeat():0.00}    Hits: {session.SuccessCount}    Misses: {session.MissCount}    {DescribeTempo()}", style);
            GUI.Label(new Rect(24, 138, 900, 40), $"Next wall: {DescribeEvent(session.NextEvent)}", style);

            if (session.IsDistorted || session.IsFailed)
            {
                var stateStyle = new GUIStyle(style)
                {
                    fontSize = 30,
                    normal = { textColor = session.IsFailed ? Color.red : new Color(1f, 0.45f, 0.05f) }
                };
                GUI.Label(new Rect(24, 246, 900, 50), session.IsFailed ? "FAILED" : "DISTORTED", stateStyle);
            }

            var feedbackStyle = new GUIStyle(style)
            {
                fontSize = 36,
                normal = { textColor = Time.time <= feedbackUntil ? feedbackColor : Color.white }
            };
            GUI.Label(new Rect(24, 188, 900, 60), feedback, feedbackStyle);
        }

        private void Restart()
        {
            session = new SingleButtonChartSession(track != null && track.Chart != null ? track.Chart.Events : null, hitWindowBeats);
            feedback = "Press Space as the ball crosses each wall.";
            feedbackColor = Color.white;
            feedbackUntil = 0f;

            if (track == null || track.AudioClip == null)
            {
                return;
            }

            UpdateCameraFollow(0f);

            audioSource.clip = track.AudioClip;
            audioSource.loop = false;
            audioSource.pitch = session.TempoScale;
            audioSource.volume = 1f;
            if (musicDistortionFilter != null)
            {
                musicDistortionFilter.distortionLevel = 0f;
            }
            smoothedDistortionAmount = 0f;
            if (noiseSource != null)
            {
                noiseSource.loop = true;
                noiseSource.volume = 0f;
                if (!noiseSource.isPlaying)
                {
                    noiseSource.Play();
                }
            }
            audioSource.PlayDelayed(Mathf.Max(0f, startDelaySeconds));
        }

        private float GetCurrentBeat()
        {
            if (track == null)
            {
                return 0f;
            }

            var secondsPerBeat = 60f / Mathf.Max(1f, track.BaseBpm);
            var seconds = audioSource != null && audioSource.clip != null
                ? audioSource.time
                : Time.timeSinceLevelLoad;
            return (seconds - track.FirstBeatOffsetSeconds) / secondsPerBeat;
        }

        private void SetFeedback(string text, Color color)
        {
            feedback = text;
            feedbackColor = color;
            feedbackUntil = Time.time + 0.35f;
        }

        private void UpdateCueVisual(float currentBeat)
        {
            if (cueObject == null)
            {
                return;
            }

            cueObject.localPosition = new Vector3(0f, 0.6f, currentBeat * unitsPerBeat);

            var nextEvent = session.NextEvent;
            var distance = nextEvent == null ? 0f : nextEvent.Beat - currentBeat;
            var renderer = cueObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Mathf.Abs(distance) <= hitWindowBeats ? Color.green : Color.yellow;
            }

            UpdateCameraFollow(cueObject.localPosition.z);
        }

        private void UpdateCameraFollow(float ballZ)
        {
            var targetCamera = cameraToFollow != null
                ? cameraToFollow
                : Camera.main != null ? Camera.main.transform : null;

            if (targetCamera == null)
            {
                return;
            }

            var position = targetCamera.position;
            position.z = Mathf.Max(minimumCameraZ, ballZ + cameraLeadUnits);
            targetCamera.position = position;
        }

        private void UpdateDistortionAudio()
        {
            var targetDistortionAmount = session.DistortionAmount;
            var smoothing = Mathf.Max(0.001f, distortionSmoothingSeconds);
            smoothedDistortionAmount = Mathf.MoveTowards(
                smoothedDistortionAmount,
                targetDistortionAmount,
                Time.deltaTime / smoothing);

            if (musicDistortionFilter != null)
            {
                musicDistortionFilter.distortionLevel = maxMusicDistortionLevel * smoothedDistortionAmount;
            }

            var phase = Mathf.Repeat(Time.time * dropoutPulseRate, 1f);
            var dropoutPulse = SingleButtonChartSession.GetDropoutPulse(smoothedDistortionAmount, phase);
            audioSource.volume = Mathf.Lerp(1f, dropoutVolumeFloor, dropoutPulse * smoothedDistortionAmount);

            if (noiseSource == null)
            {
                return;
            }

            if (!noiseSource.isPlaying)
            {
                noiseSource.Play();
            }

            noiseSource.volume = distortionNoiseVolume * smoothedDistortionAmount;
            noiseSource.pitch = Mathf.Lerp(0.8f, 1.25f, smoothedDistortionAmount);
        }

        private static string DescribeEvent(RhythmActionEvent evt)
        {
            if (evt == null)
            {
                return "Complete";
            }

            return $"beat {evt.Beat:0.##} / {evt.ActionType} / {evt.Hand} / {evt.Direction}";
        }

        private string DescribeTempo()
        {
            var effectiveBpm = track != null ? track.BaseBpm * session.TempoScale : 0f;
            var state = session.IsDistorted ? " / DISTORTED" : string.Empty;
            return $"Tempo: {session.TempoScale:0.00} / BPM: {effectiveBpm:0.0}{state}";
        }
    }
}
