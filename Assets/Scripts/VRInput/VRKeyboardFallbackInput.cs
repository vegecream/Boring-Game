using RhythmParkour;

namespace BoringRun.VRInput
{
    public static class VRKeyboardFallbackInput
    {
        public static VRInputSnapshot CreateHoldSnapshot(
            RhythmActionEvent expected,
            bool isPressed,
            bool wasPressedThisFrame,
            bool wasReleasedThisFrame,
            float holdDuration,
            float secondsPerBeat)
        {
            var hand = ToVRHand(expected != null ? expected.Hand : RhythmHand.Right);
            if (hand == VRHand.Both)
                hand = VRHand.Right;

            var frame = new VRHandInputFrame
            {
                isTracked = true,
                isPressed = isPressed,
                wasPressedThisFrame = wasPressedThisFrame,
                wasReleasedThisFrame = wasReleasedThisFrame,
                holdDuration = isPressed ? holdDuration : 0f,
                direction = ToVRDirection(expected != null ? expected.Direction : RhythmDirection.Up)
            };

            return hand == VRHand.Left
                ? VRInputSnapshot.FromHands(frame, default, bothHandsPressedTogether: false, secondsPerBeat)
                : VRInputSnapshot.FromHands(default, frame, bothHandsPressedTogether: false, secondsPerBeat);
        }

        static VRHand ToVRHand(RhythmHand hand)
        {
            switch (hand)
            {
                case RhythmHand.Left:
                    return VRHand.Left;
                case RhythmHand.Both:
                    return VRHand.Both;
                case RhythmHand.Right:
                case RhythmHand.None:
                default:
                    return VRHand.Right;
            }
        }

        static VRHandDirection ToVRDirection(RhythmDirection direction)
        {
            switch (direction)
            {
                case RhythmDirection.Down:
                    return VRHandDirection.Down;
                case RhythmDirection.Left:
                    return VRHandDirection.Left;
                case RhythmDirection.Right:
                    return VRHandDirection.Right;
                case RhythmDirection.Up:
                case RhythmDirection.None:
                default:
                    return VRHandDirection.Up;
            }
        }
    }
}
