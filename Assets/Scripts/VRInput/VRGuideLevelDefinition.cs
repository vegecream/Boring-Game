using System;
using UnityEngine;

namespace BoringRun.VRInput
{
    [CreateAssetMenu(fileName = "VRGuideLevelDefinition", menuName = "Boring Run/VR Guide Level")]
    public sealed class VRGuideLevelDefinition : ScriptableObject
    {
        [SerializeField] string displayName = "Guide Level";
        [SerializeField, TextArea] string shortInstruction = "Perform the highlighted action.";
        [SerializeField, TextArea] string successInstruction = "Good. Continue to the next guide.";
        [SerializeField, TextArea] string failureHint = "Try again with the shown hand and direction.";

        [Header("Expected Input")]
        [SerializeField] VRParkourActionType expectedActionType = VRParkourActionType.Step;
        [SerializeField] VRHand expectedHand = VRHand.Both;
        [SerializeField] VRHandDirection expectedDirection = VRHandDirection.Unknown;
        [SerializeField] bool matchHand = true;
        [SerializeField] bool matchDirection = true;
        [SerializeField, Min(0f)] float requiredHoldSeconds;

        [Header("Completion")]
        [SerializeField, Min(1)] int requiredSuccessCount = 3;
        [SerializeField, Min(0f)] float timeoutSeconds = 30f;

        public string DisplayName => displayName;
        public string ShortInstruction => shortInstruction;
        public string SuccessInstruction => successInstruction;
        public string FailureHint => failureHint;
        public VRParkourActionType ExpectedActionType => expectedActionType;
        public VRHand ExpectedHand => expectedHand;
        public VRHandDirection ExpectedDirection => expectedDirection;
        public bool MatchHand => matchHand;
        public bool MatchDirection => matchDirection;
        public float RequiredHoldSeconds => requiredHoldSeconds;
        public int RequiredSuccessCount => Mathf.Max(1, requiredSuccessCount);
        public float TimeoutSeconds => Mathf.Max(0f, timeoutSeconds);

        public bool Matches(VRParkourInputEvent inputEvent)
        {
            if (inputEvent.actionType != expectedActionType)
                return false;

            if (matchHand && inputEvent.hand != expectedHand)
                return false;

            if (matchDirection && inputEvent.direction != expectedDirection)
                return false;

            if (inputEvent.holdDuration + Mathf.Epsilon < requiredHoldSeconds)
                return false;

            return true;
        }

        public string BuildStatusMessage(int successCount)
        {
            return $"{displayName}: {shortInstruction} ({Mathf.Clamp(successCount, 0, RequiredSuccessCount)}/{RequiredSuccessCount})";
        }

        public string BuildMismatchMessage(VRParkourInputEvent inputEvent)
        {
            return $"{displayName}: {failureHint} Expected {DescribeExpectedInput()}, received {DescribeInput(inputEvent)}.";
        }

        public string BuildCompletionMessage()
        {
            return $"{displayName}: {successInstruction}";
        }

        public string DescribeExpectedInput()
        {
            var hand = matchHand ? expectedHand.ToString() : "AnyHand";
            var direction = matchDirection ? expectedDirection.ToString() : "AnyDirection";
            var hold = requiredHoldSeconds > 0f ? $" hold {requiredHoldSeconds:0.0}s" : string.Empty;
            return $"{expectedActionType} / {hand} / {direction}{hold}";
        }

        static string DescribeInput(VRParkourInputEvent inputEvent)
        {
            var hold = inputEvent.holdDuration > 0f ? $" hold {inputEvent.holdDuration:0.0}s" : string.Empty;
            return $"{inputEvent.actionType} / {inputEvent.hand} / {inputEvent.direction}{hold}";
        }

        void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(displayName))
                displayName = name;

            requiredSuccessCount = Math.Max(1, requiredSuccessCount);
            timeoutSeconds = Mathf.Max(0f, timeoutSeconds);
            requiredHoldSeconds = Mathf.Max(0f, requiredHoldSeconds);
        }
    }
}
