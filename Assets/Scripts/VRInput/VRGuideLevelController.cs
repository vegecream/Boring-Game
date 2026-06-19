using System;
using RhythmParkour;
using UnityEngine;
using UnityEngine.Events;

namespace BoringRun.VRInput
{
    [Serializable]
    public sealed class VRGuideLevelUnityEvent : UnityEvent<VRGuideLevelDefinition>
    {
    }

    [Serializable]
    public sealed class VRGuideMessageUnityEvent : UnityEvent<string>
    {
    }

    public sealed class VRGuideLevelController : MonoBehaviour
    {
        [SerializeField] VRGuideSequence sequence;
        [SerializeField] VRParkourInputEvents inputEvents;
        [SerializeField] bool autoStart = true;
        [SerializeField] bool advanceAutomatically = true;
        [SerializeField, Min(0f)] float autoAdvanceDelaySeconds = 1.2f;

        [Header("Editor Debug")]
        [SerializeField] bool showDesktopOverlay = true;
        [SerializeField] KeyCode restartKey = KeyCode.R;
        [SerializeField] KeyCode nextKey = KeyCode.N;

        [Header("Events")]
        [SerializeField] VRGuideLevelUnityEvent levelStarted = new VRGuideLevelUnityEvent();
        [SerializeField] VRGuideLevelUnityEvent levelCompleted = new VRGuideLevelUnityEvent();
        [SerializeField] UnityEvent sequenceCompleted = new UnityEvent();
        [SerializeField] VRGuideMessageUnityEvent messageChanged = new VRGuideMessageUnityEvent();

        int currentLevelIndex = -1;
        int successCount;
        float levelStartedAt;
        float levelCompletedAt = -1f;
        bool currentLevelCompleted;
        bool sequenceIsComplete;
        string currentMessage = "Guide not started.";

        public VRGuideLevelDefinition CurrentLevel => sequence != null ? sequence.GetLevel(currentLevelIndex) : null;
        public int CurrentLevelIndex => currentLevelIndex;
        public int SuccessCount => successCount;
        public bool CurrentLevelCompleted => currentLevelCompleted;
        public bool SequenceIsComplete => sequenceIsComplete;
        public string CurrentMessage => currentMessage;

        public VRGuideLevelUnityEvent LevelStarted => levelStarted;
        public VRGuideLevelUnityEvent LevelCompleted => levelCompleted;
        public UnityEvent SequenceCompleted => sequenceCompleted;
        public VRGuideMessageUnityEvent MessageChanged => messageChanged;

        void Reset()
        {
            inputEvents = FindObjectOfType<VRParkourInputEvents>();
        }

        void OnEnable()
        {
            Subscribe();
        }

        void Start()
        {
            if (autoStart)
                BeginLevel(0);
        }

        void Update()
        {
            if (NewInputKeyboard.WasPressedThisFrame(restartKey))
            {
                RestartCurrentLevel();
                return;
            }

            if (NewInputKeyboard.WasPressedThisFrame(nextKey))
            {
                AdvanceLevel();
                return;
            }

            if (currentLevelCompleted && advanceAutomatically && Time.time - levelCompletedAt >= autoAdvanceDelaySeconds)
                AdvanceLevel();

            UpdateTimeoutMessage();
        }

        void OnDisable()
        {
            Unsubscribe();
        }

        public void BeginLevel(int levelIndex)
        {
            if (sequence == null || sequence.Count <= 0)
            {
                currentLevelIndex = -1;
                sequenceIsComplete = true;
                SetMessage("No guide sequence assigned.");
                sequenceCompleted.Invoke();
                return;
            }

            if (levelIndex >= sequence.Count)
            {
                currentLevelIndex = sequence.Count;
                sequenceIsComplete = true;
                currentLevelCompleted = true;
                SetMessage("Guide sequence complete.");
                sequenceCompleted.Invoke();
                return;
            }

            currentLevelIndex = Mathf.Clamp(levelIndex, 0, sequence.Count - 1);
            successCount = 0;
            levelStartedAt = Time.time;
            levelCompletedAt = -1f;
            currentLevelCompleted = false;
            sequenceIsComplete = false;

            var level = CurrentLevel;
            if (level == null)
            {
                AdvanceLevel();
                return;
            }

            SetMessage(level.BuildStatusMessage(successCount));
            levelStarted.Invoke(level);
        }

        public void RestartGuide()
        {
            BeginLevel(0);
        }

        public void RestartCurrentLevel()
        {
            BeginLevel(Mathf.Max(0, currentLevelIndex));
        }

        public void AdvanceLevel()
        {
            BeginLevel(currentLevelIndex + 1);
        }

        void Subscribe()
        {
            var source = inputEvents != null ? inputEvents : FindObjectOfType<VRParkourInputEvents>();
            if (source == null)
                return;

            inputEvents = source;
            inputEvents.InputRecognized += HandleInputRecognized;
        }

        void Unsubscribe()
        {
            if (inputEvents != null)
                inputEvents.InputRecognized -= HandleInputRecognized;
        }

        void HandleInputRecognized(VRParkourInputEvent inputEvent)
        {
            var level = CurrentLevel;
            if (level == null || currentLevelCompleted || sequenceIsComplete)
                return;

            if (!level.Matches(inputEvent))
            {
                SetMessage(level.BuildMismatchMessage(inputEvent));
                return;
            }

            successCount++;
            if (successCount >= level.RequiredSuccessCount)
            {
                CompleteCurrentLevel(level);
                return;
            }

            SetMessage(level.BuildStatusMessage(successCount));
        }

        void CompleteCurrentLevel(VRGuideLevelDefinition level)
        {
            currentLevelCompleted = true;
            levelCompletedAt = Time.time;
            SetMessage(level.BuildCompletionMessage());
            levelCompleted.Invoke(level);
        }

        void UpdateTimeoutMessage()
        {
            var level = CurrentLevel;
            if (level == null || currentLevelCompleted || level.TimeoutSeconds <= 0f)
                return;

            if (Time.time - levelStartedAt < level.TimeoutSeconds)
                return;

            SetMessage($"{level.DisplayName}: {level.FailureHint} Press {restartKey} to retry or {nextKey} to skip.");
            levelStartedAt = Time.time;
        }

        void SetMessage(string message)
        {
            if (currentMessage == message)
                return;

            currentMessage = message;
            messageChanged.Invoke(currentMessage);
        }

        void OnGUI()
        {
            if (!showDesktopOverlay)
                return;

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                normal = { textColor = Color.white },
                wordWrap = true
            };

            GUI.Label(new Rect(24, 260, 1040, 90), currentMessage, style);
        }
    }
}
