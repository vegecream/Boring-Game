using BoringRun.VRInput;
using NUnit.Framework;

namespace RhythmParkour.Tests
{
    public sealed class VRExpectedActionInputMatcherTests
    {
        [Test]
        public void StepMatchesOnlyExpectedHandPress()
        {
            var expected = new RhythmActionEvent(beat: 2f, durationBeats: 1f, RhythmActionType.Step, RhythmHand.Left, RhythmDirection.None);
            var input = VRInputSnapshot.FromHands(
                new VRHandInputFrame { isPressed = true, wasPressedThisFrame = true, direction = VRHandDirection.Unknown },
                new VRHandInputFrame { isPressed = true, wasPressedThisFrame = true, direction = VRHandDirection.Unknown },
                bothHandsPressedTogether: true);

            Assert.That(VRExpectedActionInputMatcher.Matches(expected, input), Is.True);

            expected = new RhythmActionEvent(beat: 2f, durationBeats: 1f, RhythmActionType.Step, RhythmHand.Right, RhythmDirection.None);
            input = VRInputSnapshot.FromHands(
                new VRHandInputFrame { isPressed = true, wasPressedThisFrame = true, direction = VRHandDirection.Unknown },
                new VRHandInputFrame { isPressed = true, wasPressedThisFrame = false, direction = VRHandDirection.Unknown },
                bothHandsPressedTogether: true);

            Assert.That(VRExpectedActionInputMatcher.Matches(expected, input), Is.False);
        }

        [Test]
        public void SideGrabRequiresExpectedHandAndDirection()
        {
            var expected = new RhythmActionEvent(beat: 6f, durationBeats: 1f, RhythmActionType.SideGrab, RhythmHand.Right, RhythmDirection.Left);
            var matching = VRInputSnapshot.FromHands(
                default,
                new VRHandInputFrame { isPressed = true, wasPressedThisFrame = true, direction = VRHandDirection.Left },
                bothHandsPressedTogether: false);
            var wrongDirection = VRInputSnapshot.FromHands(
                default,
                new VRHandInputFrame { isPressed = true, wasPressedThisFrame = true, direction = VRHandDirection.Right },
                bothHandsPressedTogether: false);

            Assert.That(VRExpectedActionInputMatcher.Matches(expected, matching), Is.True);
            Assert.That(VRExpectedActionInputMatcher.Matches(expected, wrongDirection), Is.False);
        }

        [Test]
        public void SlideAndLongJumpMatchTwoHandPose()
        {
            var slide = new RhythmActionEvent(beat: 10f, durationBeats: 1f, RhythmActionType.Slide, RhythmHand.Both, RhythmDirection.Down);
            var longJump = new RhythmActionEvent(beat: 12f, durationBeats: 1f, RhythmActionType.LongJump, RhythmHand.Both, RhythmDirection.Up);

            var downChord = VRInputSnapshot.FromHands(
                new VRHandInputFrame { isPressed = true, wasPressedThisFrame = true, direction = VRHandDirection.Down },
                new VRHandInputFrame { isPressed = true, wasPressedThisFrame = true, direction = VRHandDirection.Down },
                bothHandsPressedTogether: true);
            var upChord = VRInputSnapshot.FromHands(
                new VRHandInputFrame { isPressed = true, wasPressedThisFrame = true, direction = VRHandDirection.Up },
                new VRHandInputFrame { isPressed = true, wasPressedThisFrame = true, direction = VRHandDirection.Up },
                bothHandsPressedTogether: true);

            Assert.That(VRExpectedActionInputMatcher.Matches(slide, downChord), Is.True);
            Assert.That(VRExpectedActionInputMatcher.Matches(slide, upChord), Is.False);
            Assert.That(VRExpectedActionInputMatcher.Matches(longJump, upChord), Is.True);
        }

        [Test]
        public void TwoHandActionRejectsPreHeldChord()
        {
            var expected = new RhythmActionEvent(beat: 10f, durationBeats: 1f, RhythmActionType.Slide, RhythmHand.Both, RhythmDirection.Down);
            var preHeld = VRInputSnapshot.FromHands(
                new VRHandInputFrame { isPressed = true, wasPressedThisFrame = false, direction = VRHandDirection.Down },
                new VRHandInputFrame { isPressed = true, wasPressedThisFrame = false, direction = VRHandDirection.Down },
                bothHandsPressedTogether: true);

            Assert.That(VRExpectedActionInputMatcher.Matches(expected, preHeld), Is.False);
        }

        [Test]
        public void GrappleRequiresExpectedHandUpHoldProgress()
        {
            var expected = new RhythmActionEvent(beat: 17f, durationBeats: 2f, RhythmActionType.Grapple, RhythmHand.Right, RhythmDirection.Up);
            var matching = VRInputSnapshot.FromHands(
                default,
                new VRHandInputFrame { isPressed = true, direction = VRHandDirection.Up, holdDuration = 1.7f },
                bothHandsPressedTogether: false);
            var tooShort = VRInputSnapshot.FromHands(
                default,
                new VRHandInputFrame { isPressed = true, direction = VRHandDirection.Up, holdDuration = 1.2f },
                bothHandsPressedTogether: false);

            Assert.That(VRExpectedActionInputMatcher.Matches(expected, matching, minimumHoldCompletionPercent: 0.8f), Is.True);
            Assert.That(VRExpectedActionInputMatcher.Matches(expected, tooShort, minimumHoldCompletionPercent: 0.8f), Is.False);
        }

        [Test]
        public void GrappleDefaultRequiresFullHoldProgress()
        {
            var expected = new RhythmActionEvent(beat: 17f, durationBeats: 2f, RhythmActionType.Grapple, RhythmHand.Right, RhythmDirection.Up);
            var fullHold = VRInputSnapshot.FromHands(
                default,
                new VRHandInputFrame { isPressed = true, direction = VRHandDirection.Up, holdDuration = 2f },
                bothHandsPressedTogether: false);
            var partialHold = VRInputSnapshot.FromHands(
                default,
                new VRHandInputFrame { isPressed = true, direction = VRHandDirection.Up, holdDuration = 1.99f },
                bothHandsPressedTogether: false);

            Assert.That(VRExpectedActionInputMatcher.Matches(expected, fullHold), Is.True);
            Assert.That(VRExpectedActionInputMatcher.Matches(expected, partialHold), Is.False);
        }

        [Test]
        public void GrappleStartMatchesPressedExpectedHandUp()
        {
            var expected = new RhythmActionEvent(beat: 17f, durationBeats: 2f, RhythmActionType.Grapple, RhythmHand.Right, RhythmDirection.Up);
            var matching = VRInputSnapshot.FromHands(
                default,
                new VRHandInputFrame { isPressed = true, direction = VRHandDirection.Up },
                bothHandsPressedTogether: false);
            var wrongDirection = VRInputSnapshot.FromHands(
                default,
                new VRHandInputFrame { isPressed = true, direction = VRHandDirection.Left },
                bothHandsPressedTogether: false);

            Assert.That(VRExpectedActionInputMatcher.MatchesGrappleStart(expected, matching), Is.True);
            Assert.That(VRExpectedActionInputMatcher.MatchesGrappleStart(expected, wrongDirection), Is.False);
        }

        [Test]
        public void GrappleUpdateIntervalUsesQuarterBeat()
        {
            Assert.That(VRExpectedActionInputMatcher.ShouldEmitGrappleUpdate(lastUpdateBeat: 8f, currentBeat: 8.24f), Is.False);
            Assert.That(VRExpectedActionInputMatcher.ShouldEmitGrappleUpdate(lastUpdateBeat: 8f, currentBeat: 8.25f), Is.True);
        }
    }
}
