using RhythmParkour;
using UnityEngine;

namespace BoringRun.VRInput
{
    public static class VRExpectedActionInputMatcher
    {
        public const float DefaultGrappleUpdateIntervalBeats = 0.25f;

        public static bool Matches(
            RhythmActionEvent expected,
            VRInputSnapshot input,
            float minimumHoldCompletionPercent = 1f)
        {
            if (expected == null)
                return false;

            switch (expected.ActionType)
            {
                case RhythmActionType.Step:
                    return MatchesStep(expected, input);
                case RhythmActionType.SideGrab:
                    return MatchesSideGrab(expected, input);
                case RhythmActionType.Slide:
                    return MatchesTwoHandAction(expected, input, VRHandDirection.Down);
                case RhythmActionType.LongJump:
                    return MatchesTwoHandAction(expected, input, VRHandDirection.Up);
                case RhythmActionType.Grapple:
                    return MatchesGrapple(expected, input, minimumHoldCompletionPercent);
                default:
                    return false;
            }
        }

        public static bool ShouldEmitGrappleUpdate(
            float lastUpdateBeat,
            float currentBeat,
            float updateIntervalBeats = DefaultGrappleUpdateIntervalBeats)
        {
            if (lastUpdateBeat < 0f)
                return true;

            var safeInterval = Mathf.Max(0.0001f, updateIntervalBeats);
            return currentBeat - lastUpdateBeat >= safeInterval - 0.0001f;
        }

        public static bool MatchesGrappleStart(RhythmActionEvent expected, VRInputSnapshot input)
        {
            if (expected == null || expected.ActionType != RhythmActionType.Grapple)
                return false;

            if (!TryGetExpectedHand(expected.Hand, out var hand) || hand == VRHand.Both)
                return false;

            if (expected.Direction != RhythmDirection.None && ToVRDirection(expected.Direction) != VRHandDirection.Up)
                return false;

            var frame = input.GetHand(hand);
            return frame.isPressed && frame.direction == VRHandDirection.Up;
        }

        public static bool MatchesGrappleHeldHand(RhythmActionEvent expected, VRInputSnapshot input)
        {
            if (expected == null || expected.ActionType != RhythmActionType.Grapple)
                return false;

            if (!TryGetExpectedHand(expected.Hand, out var hand) || hand == VRHand.Both)
                return false;

            return input.GetHand(hand).isPressed;
        }

        public static bool MatchesGrappleReleasedHand(RhythmActionEvent expected, VRInputSnapshot input)
        {
            if (expected == null || expected.ActionType != RhythmActionType.Grapple)
                return false;

            if (!TryGetExpectedHand(expected.Hand, out var hand) || hand == VRHand.Both)
                return false;

            return input.GetHand(hand).wasReleasedThisFrame;
        }

        static bool MatchesStep(RhythmActionEvent expected, VRInputSnapshot input)
        {
            if (!TryGetExpectedHand(expected.Hand, out var hand) || hand == VRHand.Both)
                return false;

            return input.GetHand(hand).wasPressedThisFrame;
        }

        static bool MatchesSideGrab(RhythmActionEvent expected, VRInputSnapshot input)
        {
            if (!TryGetExpectedHand(expected.Hand, out var hand) || hand == VRHand.Both)
                return false;

            if (!TryGetSideDirection(expected.Direction, out var direction))
                return false;

            var frame = input.GetHand(hand);
            return frame.wasPressedThisFrame && frame.direction == direction;
        }

        static bool MatchesTwoHandAction(RhythmActionEvent expected, VRInputSnapshot input, VRHandDirection direction)
        {
            if (expected.Hand != RhythmHand.None && expected.Hand != RhythmHand.Both)
                return false;

            if (expected.Direction != RhythmDirection.None && ToVRDirection(expected.Direction) != direction)
                return false;

            return input.bothHandsPressedTogether
                && input.leftHand.isPressed
                && input.rightHand.isPressed
                && input.GetHand(VRHand.Both).wasPressedThisFrame
                && input.leftHand.direction == direction
                && input.rightHand.direction == direction;
        }

        static bool MatchesGrapple(RhythmActionEvent expected, VRInputSnapshot input, float minimumHoldCompletionPercent)
        {
            if (!TryGetExpectedHand(expected.Hand, out var hand) || hand == VRHand.Both)
                return false;

            if (expected.Direction != RhythmDirection.None && ToVRDirection(expected.Direction) != VRHandDirection.Up)
                return false;

            if (!MatchesGrappleStart(expected, input))
                return false;

            var requiredHoldBeats = expected.DurationBeats * Mathf.Clamp01(minimumHoldCompletionPercent);
            return input.GetHoldBeats(hand) >= requiredHoldBeats;
        }

        static bool TryGetExpectedHand(RhythmHand rhythmHand, out VRHand hand)
        {
            switch (rhythmHand)
            {
                case RhythmHand.Left:
                    hand = VRHand.Left;
                    return true;
                case RhythmHand.Right:
                    hand = VRHand.Right;
                    return true;
                case RhythmHand.Both:
                    hand = VRHand.Both;
                    return true;
                default:
                    hand = default;
                    return false;
            }
        }

        static bool TryGetSideDirection(RhythmDirection rhythmDirection, out VRHandDirection direction)
        {
            direction = ToVRDirection(rhythmDirection);
            return direction == VRHandDirection.Left || direction == VRHandDirection.Right;
        }

        static VRHandDirection ToVRDirection(RhythmDirection rhythmDirection)
        {
            switch (rhythmDirection)
            {
                case RhythmDirection.Up:
                    return VRHandDirection.Up;
                case RhythmDirection.Down:
                    return VRHandDirection.Down;
                case RhythmDirection.Left:
                    return VRHandDirection.Left;
                case RhythmDirection.Right:
                    return VRHandDirection.Right;
                default:
                    return VRHandDirection.Unknown;
            }
        }
    }
}
