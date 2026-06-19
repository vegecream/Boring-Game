using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using RhythmParkour;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BoringRun.VRInput
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class VRRhythmActionPrototype : MonoBehaviour
    {
        const string OffsetInputControlName = "VRRhythmActionPrototype.OffsetInput";

        [SerializeField] RhythmTrackConfig track;
        [SerializeField] VRInputReader inputReader;
        [SerializeField] bool useKeyboardFallback = true;
        [SerializeField] float hitWindowBeats = 0.45f;
        [SerializeField] Transform cueObject;
        [SerializeField] float unitsPerBeat = 2.5f;
        [SerializeField] bool useTrackPositionMap = true;
        [SerializeField] float grappleExtraUnitsPerBeat = RhythmTrackPositionMapper.DefaultGrappleExtraUnitsPerBeat;
        [SerializeField] Transform cameraToFollow;
        [SerializeField] float cameraLeadUnits = 10f;
        [SerializeField] float minimumCameraZ = 12f;
        [SerializeField] AudioSource noiseSource;
        [SerializeField] AudioSource feedbackSource;
        [SerializeField, Range(0f, 1f)] float successSoundVolume = 0.45f;
        [SerializeField, Range(0f, 1f)] float failureSoundVolume = 0.55f;
        [SerializeField] float distortionNoiseVolume = 0.08f;
        [SerializeField] AudioDistortionFilter musicDistortionFilter;
        [SerializeField] float maxMusicDistortionLevel = 0.22f;
        [SerializeField] float dropoutPulseRate = 9f;
        [SerializeField] float dropoutVolumeFloor = 0.7f;
        [SerializeField] float distortionSmoothingSeconds = 0.35f;
        [SerializeField] bool enableMissTempoPenalty = true;
        [SerializeField] float tailSilenceSeconds = 10f;

        [Header("Debug Display")]
        [SerializeField] bool showDebugGui = true;

        [Header("VR Feedback HUD")]
        [SerializeField] bool enableVrFeedbackHud = true;
        [SerializeField] TextMesh vrFeedbackText;
        [SerializeField] Vector3 vrFeedbackLocalPosition = new Vector3(0f, -0.42f, 2.25f);
        [SerializeField] float vrFeedbackScale = 0.035f;
        [SerializeField] float feedbackDisplaySeconds = 1.4f;

        [Header("Offset Calibration")]
        [SerializeField] bool enableRuntimeOffsetAdjustment;

        [Header("Keyboard Fallback")]
        [SerializeField] KeyCode keyboardHitKey = KeyCode.Space;

        [Header("Start Flow")]
        [SerializeField] bool waitForStartInput = true;
        [SerializeField] KeyCode keyboardStartKey = KeyCode.Space;
        [SerializeField] bool allowVrStartInput = true;

        AudioSource audioSource;
        AudioClip successClip;
        AudioClip failureClip;
        VRRhythmActionSession session;
        string feedback = "Waiting for track.";
        Color feedbackColor = Color.white;
        float feedbackUntil;
        bool previousKeyboardPressed;
        float keyboardPressedAt = -1f;
        float smoothedDistortionAmount;
        bool hasFailed;
        bool gameStarted;
        bool waitingForAudioLoad;
        bool hasCurrentBeat;
        float lastCurrentBeat;
        float lastPlaybackSeconds;
        float tailSilenceStartedAt = -1f;
        bool mainAudioEnded;
        float runtimeOffsetAdjustmentSeconds;
        string offsetInputText;
        bool offsetInputFocused;
        Coroutine playbackRoutine;
        readonly List<RhythmActionEvent> missedEvents = new List<RhythmActionEvent>();

        public VRRhythmActionSession Session => session;

        public bool IsGameStarted => gameStarted;

        public float CurrentTempoScale => session != null ? session.TempoScale : 1f;

        public float CurrentBeat => gameStarted ? GetCurrentBeat() : 0f;

        public float CurrentSecondsPerBeat
        {
            get
            {
                if (track == null)
                    return 60f / 120f;

                var tempoScale = session != null ? session.TempoScale : 1f;
                return 60f / Mathf.Max(1f, track.BaseBpm * tempoScale);
            }
        }

        public event Action<VRRhythmJudgmentResult> JudgmentResolved;

        public event Action<RhythmActionEvent> ActionMissed;

        public event Action<RhythmActionEvent> GrappleStarted;

        void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            musicDistortionFilter = musicDistortionFilter != null
                ? musicDistortionFilter
                : GetComponent<AudioDistortionFilter>();

            if (musicDistortionFilter == null)
                musicDistortionFilter = gameObject.AddComponent<AudioDistortionFilter>();

            musicDistortionFilter.distortionLevel = 0f;
            EnsureNoiseSource();
            EnsureFeedbackSource();
            EnsureVrFeedbackHud();
        }

        void Start()
        {
            ResetRunState();
            if (!waitForStartInput)
                BeginPlayback();
        }

        void Update()
        {
            if (track == null || session == null)
                return;

            EnsureVrFeedbackHud();
            UpdateVrFeedbackHud();

            if (NewInputKeyboard.WasPressedThisFrame(KeyCode.R))
            {
                ResetRunState();
                if (!waitForStartInput)
                    BeginPlayback();

                return;
            }

            if (!gameStarted)
            {
                if (HasStartInput())
                    BeginPlayback();

                UpdateCueVisual(0f);
                return;
            }

            var currentBeat = GetCurrentBeat();
            EnsureMainAudioIsPlaying();
            audioSource.pitch = session.TempoScale;
            UpdateDistortionAudio();

            if (hasFailed)
                return;

            missedEvents.Clear();
            var missed = session.ConsumeMisses(currentBeat, missedEvents);
            if (missed > 0)
            {
                for (var i = 0; i < missedEvents.Count; i++)
                {
                    ActionMissed?.Invoke(missedEvents[i]);
                }

                PlayFailureSound();
                var missedEvent = missedEvents.Count > 0 ? missedEvents[0] : null;
                var missText = $"MISS x{missed}  {DescribeEventShort(missedEvent)}";
                SetFeedback(missText, Color.red);
            }

            var keyboardHandledThisFrame = useKeyboardFallback && JudgeKeyboardFallback(currentBeat);

            var reader = inputReader != null ? inputReader : VRInputReader.Instance;
            if (!keyboardHandledThisFrame && reader != null)
            {
                var secondsPerBeat = 60f / Mathf.Max(1f, track.BaseBpm * session.TempoScale);
                var snapshot = reader.CreateSnapshot(secondsPerBeat);
                if (ShouldJudgeVRInput(session.NextEvent, currentBeat, snapshot))
                {
                    var result = session.JudgeInput(currentBeat, snapshot);
                    ResolveJudgment(result);
                }
            }
            UpdateCueVisual(currentBeat);
            if (session.IsFailed)
                EnterFailureState();
        }

        void OnGUI()
        {
            if (!showDebugGui)
                return;

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                normal = { textColor = Color.white }
            };

            GUI.Label(new Rect(24, 20, 1000, 40), "VR Rhythm Action Prototype", style);
            GUI.Label(new Rect(24, 58, 1000, 40), $"Start: {keyboardStartKey} / controller button, Hit: {keyboardHitKey}, R = reset", style);

            if (track == null || session == null)
            {
                GUI.Label(new Rect(24, 100, 1000, 40), "No track assigned.", style);
                return;
            }

            var beat = gameStarted ? GetCurrentBeat() : 0f;
            var state = gameStarted ? "Playing" : waitingForAudioLoad ? "Loading music" : "Waiting for start";
            GUI.Label(new Rect(24, 100, 1000, 40), $"State: {state}    Beat: {beat:0.00}    Hits: {session.SuccessCount}    Misses: {session.MissCount}    {DescribeTempo()}", style);
            GUI.Label(new Rect(24, 138, 1000, 40), $"Next action: {DescribeEvent(session.NextEvent)}", style);
            if (enableRuntimeOffsetAdjustment)
                DrawOffsetAdjustmentGui(style);

            var feedbackStyle = new GUIStyle(style)
            {
                fontSize = 34,
                normal = { textColor = Time.time <= feedbackUntil ? feedbackColor : Color.white }
            };
            GUI.Label(new Rect(24, enableRuntimeOffsetAdjustment ? 266 : 188, 1000, 60), feedback, feedbackStyle);
        }

        void ResetRunState()
        {
            session = new VRRhythmActionSession(
                track != null && track.Chart != null ? track.Chart.Events : null,
                hitWindowBeats,
                enableMissTempoPenalty);
            feedback = waitForStartInput
                ? $"Press {keyboardStartKey} or a controller button to start."
                : $"Press {keyboardHitKey} as the cue reaches the timing line.";
            feedbackColor = Color.white;
            feedbackUntil = 0f;
            previousKeyboardPressed = false;
            keyboardPressedAt = -1f;
            smoothedDistortionAmount = 0f;
            hasFailed = false;
            gameStarted = false;
            waitingForAudioLoad = false;
            hasCurrentBeat = false;
            lastCurrentBeat = 0f;
            lastPlaybackSeconds = 0f;
            tailSilenceStartedAt = -1f;
            mainAudioEnded = false;

            if (playbackRoutine != null)
            {
                StopCoroutine(playbackRoutine);
                playbackRoutine = null;
            }

            if (musicDistortionFilter != null)
                musicDistortionFilter.distortionLevel = 0f;

            if (noiseSource != null)
            {
                noiseSource.volume = 0f;
                noiseSource.loop = true;
                noiseSource.Stop();
            }

            EnsureFeedbackSource();

            ConfigureAudioSource();
            UpdateCueVisual(0f);
        }

        void BeginPlayback()
        {
            if (track == null || track.AudioClip == null)
                return;

            ConfigureAudioSource();
            if (playbackRoutine != null)
                StopCoroutine(playbackRoutine);

            playbackRoutine = StartCoroutine(BeginPlaybackWhenClipLoaded());
        }

        IEnumerator BeginPlaybackWhenClipLoaded()
        {
            var clip = audioSource != null ? audioSource.clip : null;
            if (clip == null)
                yield break;

            waitingForAudioLoad = true;
            feedback = $"Loading music: {clip.name}";
            feedbackColor = Color.white;
            feedbackUntil = Time.time + 0.8f;

            if (clip.loadState == AudioDataLoadState.Unloaded && !clip.LoadAudioData())
            {
                waitingForAudioLoad = false;
                playbackRoutine = null;
                SetFeedback($"Music failed to load: {clip.name}", Color.red);
                Debug.LogWarning($"{nameof(VRRhythmActionPrototype)} on {name} could not start loading {clip.name}.", this);
                yield break;
            }

            while (clip.loadState == AudioDataLoadState.Loading)
                yield return null;

            if (clip.loadState != AudioDataLoadState.Loaded)
            {
                waitingForAudioLoad = false;
                playbackRoutine = null;
                SetFeedback($"Music load state: {clip.loadState}", Color.red);
                Debug.LogWarning($"{nameof(VRRhythmActionPrototype)} on {name} cannot play {clip.name}; load state is {clip.loadState}.", this);
                yield break;
            }

            AudioListener.pause = false;
            if (AudioListener.volume <= 0f)
                AudioListener.volume = 1f;

            audioSource.time = 0f;
            audioSource.Play();
            gameStarted = true;
            hasCurrentBeat = false;
            lastCurrentBeat = 0f;
            lastPlaybackSeconds = 0f;
            tailSilenceStartedAt = -1f;
            mainAudioEnded = false;
            waitingForAudioLoad = false;
            playbackRoutine = null;
            previousKeyboardPressed = NewInputKeyboard.IsPressed(keyboardHitKey);
            keyboardPressedAt = -1f;
            feedback = $"Started. Press {keyboardHitKey} as the cue reaches the timing line.";
            feedbackColor = Color.white;
            feedbackUntil = Time.time + 0.8f;

            if (noiseSource != null && !noiseSource.isPlaying)
                noiseSource.Play();

            Debug.Log($"{nameof(VRRhythmActionPrototype)} playing {clip.name}. isPlaying={audioSource.isPlaying}, volume={audioSource.volume}, listenerVolume={AudioListener.volume}, listenerPaused={AudioListener.pause}, speakerMode={AudioSettings.speakerMode}", this);
        }

        void ConfigureAudioSource()
        {
            if (audioSource == null || track == null || track.AudioClip == null)
                return;

            audioSource.clip = track.AudioClip;
            audioSource.loop = false;
            audioSource.playOnAwake = false;
            audioSource.pitch = session != null ? session.TempoScale : 1f;
            audioSource.volume = 1f;
            audioSource.spatialBlend = 0f;
            audioSource.dopplerLevel = 0f;
            audioSource.Stop();
            audioSource.time = 0f;
        }

        bool HasStartInput()
        {
            if (NewInputKeyboard.WasPressedThisFrame(keyboardStartKey))
                return true;

            if (!allowVrStartInput)
                return false;

            var reader = inputReader != null ? inputReader : VRInputReader.Instance;
            if (reader == null)
                return HasOVRStartInput();

            return reader.LeftHand.wasPressedThisFrame
                || reader.RightHand.wasPressedThisFrame
                || HasOVRStartInput();
        }

        void EnsureMainAudioIsPlaying()
        {
            if (audioSource == null || audioSource.clip == null || audioSource.isPlaying || hasFailed || mainAudioEnded)
                return;

            if (audioSource.time >= audioSource.clip.length - 0.05f)
                return;

            audioSource.Play();
        }

        static bool HasOVRStartInput()
        {
            var connected = OVRInput.GetConnectedControllers();
            return HasOVRStartInput(connected, OVRInput.Controller.LTouch)
                || HasOVRStartInput(connected, OVRInput.Controller.RTouch)
                || HasOVRStartInput(connected, OVRInput.Controller.LHand)
                || HasOVRStartInput(connected, OVRInput.Controller.RHand);
        }

        static bool HasOVRStartInput(OVRInput.Controller connected, OVRInput.Controller controller)
        {
            if ((connected & controller) == 0)
                return false;

            return OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, controller)
                || OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, controller)
                || OVRInput.GetDown(OVRInput.Button.One, controller)
                || OVRInput.GetDown(OVRInput.Button.Two, controller);
        }

        float GetCurrentBeat()
        {
            if (track == null)
                return 0f;

            var secondsPerBeat = 60f / Mathf.Max(1f, track.BaseBpm);
            var seconds = GetPlaybackSeconds();
            var beat = (seconds - GetEffectiveFirstBeatOffsetSeconds()) / secondsPerBeat;

            if (!gameStarted)
                return beat;

            if (!hasCurrentBeat)
            {
                hasCurrentBeat = true;
                lastCurrentBeat = beat;
                return beat;
            }

            if (beat < lastCurrentBeat)
                return lastCurrentBeat;

            lastCurrentBeat = beat;
            return beat;
        }

        float GetPlaybackSeconds()
        {
            if (audioSource == null || audioSource.clip == null)
                return Time.timeSinceLevelLoad;

            if (audioSource.isPlaying)
            {
                lastPlaybackSeconds = Mathf.Max(lastPlaybackSeconds, audioSource.time);
                tailSilenceStartedAt = -1f;
                return lastPlaybackSeconds;
            }

            var clipLength = audioSource.clip.length;
            if (gameStarted && HasReachedMainAudioTail(clipLength))
            {
                mainAudioEnded = true;
                if (tailSilenceStartedAt < 0f)
                    tailSilenceStartedAt = Time.time;

                var tailSeconds = Mathf.Min(Mathf.Max(0f, tailSilenceSeconds), Time.time - tailSilenceStartedAt);
                lastPlaybackSeconds = Mathf.Max(lastPlaybackSeconds, clipLength + tailSeconds);
                return lastPlaybackSeconds;
            }

            lastPlaybackSeconds = Mathf.Max(lastPlaybackSeconds, audioSource.time);
            return lastPlaybackSeconds;
        }

        bool HasReachedMainAudioTail(float clipLength)
        {
            if (!gameStarted || clipLength <= 0f)
                return false;

            if (mainAudioEnded)
                return true;

            if (lastPlaybackSeconds >= clipLength - 0.1f)
                return true;

            return lastPlaybackSeconds >= clipLength - 1f
                && audioSource != null
                && audioSource.time <= 0.05f;
        }

        void SetEffectiveFirstBeatOffsetSeconds(float offsetSeconds)
        {
            if (track == null)
                return;

            var offset = Mathf.Max(0f, offsetSeconds);
            runtimeOffsetAdjustmentSeconds = offset - track.FirstBeatOffsetSeconds;
            SyncOffsetInputText();
        }

        void ResetRuntimeOffsetAdjustment()
        {
            runtimeOffsetAdjustmentSeconds = 0f;
            SyncOffsetInputText();
            SetFeedback($"Offset reset to {GetEffectiveFirstBeatOffsetSeconds():0.000}s", Color.cyan);
        }

        float GetEffectiveFirstBeatOffsetSeconds()
        {
            if (track == null)
                return 0f;

            return Mathf.Max(0f, track.FirstBeatOffsetSeconds + runtimeOffsetAdjustmentSeconds);
        }

#if UNITY_EDITOR
        void SaveRuntimeOffsetAdjustment()
        {
            if (!enableRuntimeOffsetAdjustment || track == null)
                return;

            var offset = GetEffectiveFirstBeatOffsetSeconds();
            track.Configure(track.AudioClip, track.BaseBpm, offset, track.Chart);
            EditorUtility.SetDirty(track);
            AssetDatabase.SaveAssets();
            runtimeOffsetAdjustmentSeconds = 0f;
            SyncOffsetInputText();
            SetFeedback($"Saved offset {offset:0.000}s to {track.name}", Color.cyan);
        }
#endif

        bool JudgeKeyboardFallback(float currentBeat)
        {
            var nextEvent = session.NextEvent;
            var isPressed = NewInputKeyboard.IsPressed(keyboardHitKey);
            var wasPressedThisFrame = isPressed && !previousKeyboardPressed;
            var wasReleasedThisFrame = !isPressed && previousKeyboardPressed;

            if (wasPressedThisFrame)
                keyboardPressedAt = Time.time;

            if (nextEvent != null && nextEvent.ActionType == RhythmActionType.Grapple)
            {
                var keyboardOwnsGrappleFrame = isPressed || wasReleasedThisFrame || keyboardPressedAt >= 0f;
                var holdDuration = isPressed && keyboardPressedAt >= 0f ? Time.time - keyboardPressedAt : 0f;
                var secondsPerBeat = 60f / Mathf.Max(1f, track.BaseBpm * session.TempoScale);
                var snapshot = VRKeyboardFallbackInput.CreateHoldSnapshot(
                    nextEvent,
                    isPressed,
                    wasPressedThisFrame,
                    wasReleasedThisFrame,
                    holdDuration,
                    secondsPerBeat);

                if (session.TryConfirmGrappleStart(currentBeat, snapshot))
                    GrappleStarted?.Invoke(nextEvent);

                if (session.TryFailGrappleContinuation(currentBeat, snapshot, out var failResult))
                {
                    ResolveJudgment(failResult);
                    previousKeyboardPressed = isPressed;
                    return true;
                }

                if (session.IsInsideJudgmentWindow(currentBeat) && VRExpectedActionInputMatcher.MatchesGrappleHeldHand(nextEvent, snapshot))
                {
                    var result = session.JudgeInput(currentBeat, snapshot);
                    ResolveJudgment(result);
                    previousKeyboardPressed = isPressed;
                    return true;
                }

                previousKeyboardPressed = isPressed;
                return keyboardOwnsGrappleFrame;
            }
            else if (wasPressedThisFrame)
            {
                var result = session.JudgeTimingOnly(currentBeat);
                ResolveJudgment(result);
                previousKeyboardPressed = isPressed;
                return true;
            }

            previousKeyboardPressed = isPressed;
            return false;
        }

        bool ShouldJudgeVRInput(RhythmActionEvent nextEvent, float currentBeat, VRInputSnapshot snapshot)
        {
            if (nextEvent == null)
                return HasPressAttempt(snapshot);

            if (nextEvent.ActionType == RhythmActionType.Grapple)
            {
                if (session.TryConfirmGrappleStart(currentBeat, snapshot))
                    GrappleStarted?.Invoke(nextEvent);

                if (session.TryFailGrappleContinuation(currentBeat, snapshot, out var failResult))
                {
                    ResolveJudgment(failResult);
                    return false;
                }

                return session.IsInsideJudgmentWindow(currentBeat)
                    && VRExpectedActionInputMatcher.MatchesGrappleHeldHand(nextEvent, snapshot);
            }

            if (HasPressAttempt(snapshot))
                return true;

            return false;
        }

        static bool HasPressAttempt(VRInputSnapshot snapshot)
        {
            return snapshot.leftHand.wasPressedThisFrame || snapshot.rightHand.wasPressedThisFrame;
        }

        void UpdateCueVisual(float currentBeat)
        {
            if (cueObject == null)
                return;

            var z = GetTrackZ(currentBeat);
            cueObject.localPosition = new Vector3(0f, 0.6f, z);

            var nextEvent = session.NextEvent;
            var renderer = cueObject.GetComponent<Renderer>();
            if (renderer != null && nextEvent != null)
            {
                var distance = Mathf.Abs(nextEvent.Beat - currentBeat);
                renderer.material.color = distance <= hitWindowBeats ? Color.green : Color.yellow;
            }

            UpdateCameraFollow(z);
        }

        float GetTrackZ(float beat)
        {
            if (!useTrackPositionMap || track == null || track.Chart == null)
                return beat * unitsPerBeat;

            return RhythmTrackPositionMapper.GetZ(
                beat,
                track.Chart.Events,
                unitsPerBeat,
                grappleExtraUnitsPerBeat);
        }

        void UpdateCameraFollow(float ballZ)
        {
            var targetCamera = cameraToFollow != null
                ? cameraToFollow
                : Camera.main != null ? Camera.main.transform : null;

            if (targetCamera == null)
                return;

            var position = targetCamera.position;
            position.z = Mathf.Max(minimumCameraZ, ballZ + cameraLeadUnits);
            targetCamera.position = position;
        }

        void UpdateDistortionAudio()
        {
            var targetDistortionAmount = session.DistortionAmount;
            var smoothing = Mathf.Max(0.001f, distortionSmoothingSeconds);
            smoothedDistortionAmount = Mathf.MoveTowards(
                smoothedDistortionAmount,
                targetDistortionAmount,
                Time.deltaTime / smoothing);

            if (musicDistortionFilter != null)
                musicDistortionFilter.distortionLevel = maxMusicDistortionLevel * smoothedDistortionAmount;

            var phase = Mathf.Repeat(Time.time * dropoutPulseRate, 1f);
            var dropoutPulse = VRRhythmActionSession.GetDropoutPulse(smoothedDistortionAmount, phase);
            audioSource.volume = Mathf.Lerp(1f, dropoutVolumeFloor, dropoutPulse * smoothedDistortionAmount);

            if (noiseSource == null)
                return;

            if (!noiseSource.isPlaying)
                noiseSource.Play();

            noiseSource.volume = distortionNoiseVolume * smoothedDistortionAmount;
            noiseSource.pitch = Mathf.Lerp(0.85f, 1.2f, smoothedDistortionAmount);
        }

        void EnsureFeedbackSource()
        {
            if (feedbackSource == null)
            {
                var sourceObject = new GameObject("Rhythm Judgment Feedback Audio");
                sourceObject.transform.SetParent(transform, false);
                feedbackSource = sourceObject.AddComponent<AudioSource>();
            }

            feedbackSource.playOnAwake = false;
            feedbackSource.loop = false;
            feedbackSource.spatialBlend = 0f;
            feedbackSource.dopplerLevel = 0f;

            if (successClip == null)
                successClip = CreateSuccessClip();
            if (failureClip == null)
                failureClip = CreateFailureClip();
        }

        void PlaySuccessSound()
        {
            EnsureFeedbackSource();
            if (feedbackSource != null && successClip != null)
                feedbackSource.PlayOneShot(successClip, successSoundVolume);
        }

        void PlayFailureSound()
        {
            EnsureFeedbackSource();
            if (feedbackSource != null && failureClip != null)
                feedbackSource.PlayOneShot(failureClip, failureSoundVolume);
        }

        void EnterFailureState()
        {
            if (hasFailed)
                return;

            hasFailed = true;
            PlayFailureSound();
            SetFeedback("FAILED - tempo collapsed. Press R to restart.", Color.red);
        }

        void EnsureNoiseSource()
        {
            if (noiseSource == null)
                noiseSource = gameObject.AddComponent<AudioSource>();

            if (noiseSource.clip == null)
                noiseSource.clip = CreateNoiseClip();

            noiseSource.playOnAwake = false;
            noiseSource.loop = true;
            noiseSource.spatialBlend = 0f;
            noiseSource.volume = 0f;
        }

        static AudioClip CreateNoiseClip()
        {
            const int sampleRate = 22050;
            const int sampleCount = sampleRate;
            var data = new float[sampleCount];
            var seed = 0x12345678u;

            for (var i = 0; i < sampleCount; i++)
            {
                seed = seed * 1664525u + 1013904223u;
                var white = ((seed >> 9) / 8388608f) * 2f - 1f;
                var crackle = i % 173 == 0 ? white * 0.85f : 0f;
                data[i] = white * 0.18f + crackle;
            }

            var clip = AudioClip.Create("Generated VR Distortion Noise", sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip CreateSuccessClip()
        {
            const int sampleRate = 44100;
            const float duration = 0.095f;
            var sampleCount = Mathf.CeilToInt(sampleRate * duration);
            var data = new float[sampleCount];
            var seed = 0x2468ace0u;

            for (var i = 0; i < sampleCount; i++)
            {
                seed = seed * 1664525u + 1013904223u;
                var t = i / (float)sampleRate;
                var normalized = t / duration;
                var bodyEnvelope = Mathf.Exp(-normalized * 10.5f);
                var clickEnvelope = Mathf.Exp(-normalized * 52f);
                var noise = ((seed >> 9) / 8388608f) * 2f - 1f;
                var tone = Mathf.Sin(2f * Mathf.PI * 760f * t) * 0.58f
                    + Mathf.Sin(2f * Mathf.PI * 1180f * t) * 0.28f
                    + Mathf.Sin(2f * Mathf.PI * 1720f * t) * 0.14f;
                data[i] = (tone * bodyEnvelope + noise * clickEnvelope * 0.55f) * 0.46f;
            }

            var clip = AudioClip.Create("Generated Rhythm Success Woodblock", sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip CreateFailureClip()
        {
            const int sampleRate = 44100;
            const float duration = 0.18f;
            var sampleCount = Mathf.CeilToInt(sampleRate * duration);
            var data = new float[sampleCount];
            var seed = 0x87654321u;

            for (var i = 0; i < sampleCount; i++)
            {
                seed = seed * 1664525u + 1013904223u;
                var t = i / (float)sampleRate;
                var normalized = t / duration;
                var envelope = Mathf.Clamp01(1f - normalized);
                envelope *= envelope;
                var frequency = Mathf.Lerp(1150f, 720f, normalized);
                var phase = Mathf.Repeat(t * frequency, 1f);
                var square = phase < 0.5f ? 1f : -1f;
                var noise = ((seed >> 9) / 8388608f) * 2f - 1f;
                data[i] = (square * 0.75f + noise * 0.25f) * envelope * 0.38f;
            }

            var clip = AudioClip.Create("Generated Rhythm Failure Buzz", sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        void SetFeedback(string text, Color color)
        {
            feedback = text;
            feedbackColor = color;
            feedbackUntil = Time.time + Mathf.Max(0.1f, feedbackDisplaySeconds);
            UpdateVrFeedbackHud();
        }

        void EnsureVrFeedbackHud()
        {
            if (!enableVrFeedbackHud || vrFeedbackText != null)
                return;

            var cameraTransform = Camera.main != null ? Camera.main.transform : null;
            if (cameraTransform == null)
                return;

            var textObject = new GameObject("VR Judgment Feedback HUD");
            textObject.transform.SetParent(cameraTransform, false);
            textObject.transform.localPosition = vrFeedbackLocalPosition;
            textObject.transform.localRotation = Quaternion.identity;
            textObject.transform.localScale = Vector3.one * Mathf.Max(0.001f, vrFeedbackScale);

            vrFeedbackText = textObject.AddComponent<TextMesh>();
            vrFeedbackText.anchor = TextAnchor.MiddleCenter;
            vrFeedbackText.alignment = TextAlignment.Center;
            vrFeedbackText.fontSize = 72;
            vrFeedbackText.characterSize = 0.18f;
            vrFeedbackText.text = string.Empty;
            vrFeedbackText.color = Color.clear;
        }

        void UpdateVrFeedbackHud()
        {
            if (!enableVrFeedbackHud || vrFeedbackText == null)
                return;

            var visible = Time.time <= feedbackUntil;
            if (!visible)
            {
                vrFeedbackText.color = Color.clear;
                vrFeedbackText.text = string.Empty;
                return;
            }

            var alpha = Mathf.Clamp01((feedbackUntil - Time.time) / Mathf.Max(0.1f, feedbackDisplaySeconds));
            var color = feedbackColor;
            color.a = Mathf.Lerp(0.25f, 1f, alpha);
            vrFeedbackText.color = color;
            vrFeedbackText.text = feedback;
        }

        void ResolveJudgment(VRRhythmJudgmentResult result)
        {
            if (result.Kind == VRRhythmJudgmentKind.Hit)
                PlaySuccessSound();
            else
                PlayFailureSound();

            SetFeedback(DescribeResult(result), ResultColor(result.Kind));
            JudgmentResolved?.Invoke(result);
        }

        string DescribeResult(VRRhythmJudgmentResult result)
        {
            switch (result.Kind)
            {
                case VRRhythmJudgmentKind.Hit:
                    return $"HIT  {DescribeEventShort(result.Event)}  {result.BeatDelta:+0.00;-0.00;0.00}";
                case VRRhythmJudgmentKind.WrongInput:
                    return $"WRONG INPUT  {DescribeEventShort(result.Event)}";
                case VRRhythmJudgmentKind.BadTiming:
                default:
                    return $"BAD TIMING  next {DescribeEventShort(result.Event)}";
            }
        }

        static Color ResultColor(VRRhythmJudgmentKind kind)
        {
            return kind == VRRhythmJudgmentKind.Hit ? Color.green : Color.red;
        }

        static string DescribeEvent(RhythmActionEvent evt)
        {
            if (evt == null)
                return "Complete";

            return $"beat {evt.Beat:0.##} / {evt.ActionType} / {evt.Hand} / {evt.Direction}";
        }

        static string DescribeEventShort(RhythmActionEvent evt)
        {
            if (evt == null)
                return "Complete";

            return $"{evt.ActionType} {evt.Hand} @{evt.Beat:0.##}";
        }

        string DescribeTempo()
        {
            var effectiveBpm = track != null ? track.BaseBpm * session.TempoScale : 0f;
            var state = session.IsFailed ? " / FAILED" : session.IsDistorted ? " / DISTORTED" : string.Empty;
            return $"Tempo: {session.TempoScale:0.00} / BPM: {effectiveBpm:0.0}{state}";
        }

        void DrawOffsetAdjustmentGui(GUIStyle labelStyle)
        {
            var textFieldStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 24
            };
            var buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 22
            };

            if (!IsOffsetInputFocused())
                SyncOffsetInputText();

            GUI.Label(new Rect(24, 176, 130, 40), "Offset(s)", labelStyle);
            GUI.SetNextControlName(OffsetInputControlName);
            var nextText = GUI.TextField(new Rect(150, 176, 140, 36), offsetInputText ?? string.Empty, textFieldStyle);
            offsetInputFocused = GUI.GetNameOfFocusedControl() == OffsetInputControlName;
            if (nextText != offsetInputText)
            {
                offsetInputText = nextText;
                ApplyOffsetInputText();
            }

            GUI.Label(new Rect(306, 176, 520, 40), $"effective {GetEffectiveFirstBeatOffsetSeconds():0.000}s, delta {runtimeOffsetAdjustmentSeconds:+0.000;-0.000;0.000}s", labelStyle);
            if (GUI.Button(new Rect(840, 176, 90, 36), "Reset", buttonStyle))
                ResetRuntimeOffsetAdjustment();

#if UNITY_EDITOR
            if (GUI.Button(new Rect(944, 176, 90, 36), "Save", buttonStyle))
                SaveRuntimeOffsetAdjustment();
#endif

            GUI.Label(new Rect(24, 216, 1200, 40), "Type seconds to update timing live. Use Reset to discard the temporary adjustment, Save to write it to the track asset.", labelStyle);
        }

        void ApplyOffsetInputText()
        {
            if (track == null || string.IsNullOrWhiteSpace(offsetInputText))
                return;

            var normalized = offsetInputText.Trim().Replace(',', '.');
            if (!float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var offset))
                return;

            runtimeOffsetAdjustmentSeconds = Mathf.Max(0f, offset) - track.FirstBeatOffsetSeconds;
        }

        void SyncOffsetInputText()
        {
            offsetInputText = GetEffectiveFirstBeatOffsetSeconds().ToString("0.000", CultureInfo.InvariantCulture);
        }

        bool IsOffsetInputFocused()
        {
            return offsetInputFocused;
        }
    }
}
