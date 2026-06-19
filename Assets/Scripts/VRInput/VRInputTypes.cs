using System;

namespace BoringRun.VRInput
{
    public enum VRParkourActionType
    {
        Step,
        SideGrab,
        Slide,
        LongJump,
        TurnLeft,
        TurnRight,
        GrappleHoldStarted,
        GrappleHoldUpdated,
        GrappleHoldEnded
    }

    public enum VRHand
    {
        Left,
        Right,
        Both
    }

    public enum VRInputButton
    {
        Trigger,
        Grip,
        PrimaryButton,
        SecondaryButton
    }

    public enum VRHandDirection
    {
        Unknown,
        Up,
        Down,
        Left,
        Right,
        Forward
    }

    [Serializable]
    public struct VRHandInputFrame
    {
        public bool isTracked;
        public bool isPressed;
        public bool wasPressedThisFrame;
        public bool wasReleasedThisFrame;
        public float holdDuration;
        public VRHandDirection direction;
    }

    [Serializable]
    public struct VRParkourInputEvent
    {
        public VRParkourActionType actionType;
        public VRHand hand;
        public VRHandDirection direction;
        public float holdDuration;

        public VRParkourInputEvent(VRParkourActionType actionType, VRHand hand, VRHandDirection direction, float holdDuration)
        {
            this.actionType = actionType;
            this.hand = hand;
            this.direction = direction;
            this.holdDuration = holdDuration;
        }
    }

    [Serializable]
    public struct VRPlayerTurnFrame
    {
        public bool isTracked;
        public bool wasTurnedThisFrame;
        public VRHandDirection direction;
        public float yawDeltaDegrees;
    }

    [Serializable]
    public readonly struct VRInputSnapshot
    {
        public readonly VRHandInputFrame leftHand;
        public readonly VRHandInputFrame rightHand;
        public readonly bool bothHandsPressedTogether;
        public readonly float secondsPerBeat;

        public VRInputSnapshot(VRHandInputFrame leftHand, VRHandInputFrame rightHand, bool bothHandsPressedTogether, float secondsPerBeat)
        {
            this.leftHand = leftHand;
            this.rightHand = rightHand;
            this.bothHandsPressedTogether = bothHandsPressedTogether;
            this.secondsPerBeat = secondsPerBeat > 0f ? secondsPerBeat : 1f;
        }

        public static VRInputSnapshot FromHands(VRHandInputFrame leftHand, VRHandInputFrame rightHand, bool bothHandsPressedTogether, float secondsPerBeat = 1f)
        {
            return new VRInputSnapshot(leftHand, rightHand, bothHandsPressedTogether, secondsPerBeat);
        }

        public VRHandInputFrame GetHand(VRHand hand)
        {
            switch (hand)
            {
                case VRHand.Left:
                    return leftHand;
                case VRHand.Right:
                    return rightHand;
                case VRHand.Both:
                    return new VRHandInputFrame
                    {
                        isTracked = leftHand.isTracked && rightHand.isTracked,
                        isPressed = leftHand.isPressed && rightHand.isPressed,
                        wasPressedThisFrame = leftHand.wasPressedThisFrame || rightHand.wasPressedThisFrame,
                        wasReleasedThisFrame = leftHand.wasReleasedThisFrame || rightHand.wasReleasedThisFrame,
                        holdDuration = Math.Min(leftHand.holdDuration, rightHand.holdDuration),
                        direction = leftHand.direction == rightHand.direction ? leftHand.direction : VRHandDirection.Unknown
                    };
                default:
                    return default;
            }
        }

        public float GetHoldBeats(VRHand hand)
        {
            return GetHand(hand).holdDuration / secondsPerBeat;
        }
    }
}
