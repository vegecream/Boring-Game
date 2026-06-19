using BoringRun.VRInput;
using NUnit.Framework;

namespace RhythmParkour.Tests
{
    public sealed class VRRhythmActionSessionTests
    {
        [Test]
        public void MatchingInputInsideWindowHitsAndAdvances()
        {
            var expected = new RhythmActionEvent(beat: 4f, durationBeats: 1f, RhythmActionType.Step, RhythmHand.Left, RhythmDirection.None);
            var session = new VRRhythmActionSession(new[] { expected }, hitWindowBeats: 0.25f);
            var input = VRInputSnapshot.FromHands(
                new VRHandInputFrame { isPressed = true, wasPressedThisFrame = true, direction = VRHandDirection.Unknown },
                default,
                bothHandsPressedTogether: false);

            var result = session.JudgeInput(currentBeat: 4.1f, input);

            Assert.That(result.Kind, Is.EqualTo(VRRhythmJudgmentKind.Hit));
            Assert.That(result.Event, Is.SameAs(expected));
            Assert.That(result.BeatDelta, Is.EqualTo(0.1f).Within(0.0001f));
            Assert.That(session.SuccessCount, Is.EqualTo(1));
            Assert.That(session.MissCount, Is.EqualTo(0));
            Assert.That(session.NextEvent, Is.Null);
        }

        [Test]
        public void WrongInputInsideWindowConsumesExpectedActionAsMiss()
        {
            var expected = new RhythmActionEvent(beat: 6f, durationBeats: 1f, RhythmActionType.SideGrab, RhythmHand.Right, RhythmDirection.Left);
            var session = new VRRhythmActionSession(new[] { expected }, hitWindowBeats: 0.25f);
            var wrongDirection = VRInputSnapshot.FromHands(
                default,
                new VRHandInputFrame { isPressed = true, wasPressedThisFrame = true, direction = VRHandDirection.Right },
                bothHandsPressedTogether: false);

            var result = session.JudgeInput(currentBeat: 6f, wrongDirection);

            Assert.That(result.Kind, Is.EqualTo(VRRhythmJudgmentKind.WrongInput));
            Assert.That(result.Event, Is.SameAs(expected));
            Assert.That(session.SuccessCount, Is.EqualTo(0));
            Assert.That(session.MissCount, Is.EqualTo(1));
            Assert.That(session.NextEvent, Is.Null);
            Assert.That(session.TempoScale, Is.LessThan(1f));
        }

        [Test]
        public void InputOutsideWindowIsBadTimingAndKeepsNextAction()
        {
            var expected = new RhythmActionEvent(beat: 4f, durationBeats: 1f, RhythmActionType.Step, RhythmHand.Left, RhythmDirection.None);
            var session = new VRRhythmActionSession(new[] { expected }, hitWindowBeats: 0.25f);
            var input = VRInputSnapshot.FromHands(
                new VRHandInputFrame { isPressed = true, wasPressedThisFrame = true, direction = VRHandDirection.Unknown },
                default,
                bothHandsPressedTogether: false);

            var result = session.JudgeInput(currentBeat: 3.5f, input);

            Assert.That(result.Kind, Is.EqualTo(VRRhythmJudgmentKind.BadTiming));
            Assert.That(result.Event, Is.SameAs(expected));
            Assert.That(session.SuccessCount, Is.EqualTo(0));
            Assert.That(session.MissCount, Is.EqualTo(0));
            Assert.That(session.NextEvent, Is.SameAs(expected));
            Assert.That(session.TempoScale, Is.LessThan(1f));
        }

        [Test]
        public void TimingOnlyInputInsideWindowHitsWithoutMatchingActionShape()
        {
            var expected = new RhythmActionEvent(beat: 6f, durationBeats: 1f, RhythmActionType.SideGrab, RhythmHand.Right, RhythmDirection.Left);
            var session = new VRRhythmActionSession(new[] { expected }, hitWindowBeats: 0.25f);

            var result = session.JudgeTimingOnly(currentBeat: 6.05f);

            Assert.That(result.Kind, Is.EqualTo(VRRhythmJudgmentKind.Hit));
            Assert.That(result.Event, Is.SameAs(expected));
            Assert.That(result.BeatDelta, Is.EqualTo(0.05f).Within(0.0001f));
            Assert.That(session.SuccessCount, Is.EqualTo(1));
            Assert.That(session.MissCount, Is.EqualTo(0));
            Assert.That(session.NextEvent, Is.Null);
        }

        [Test]
        public void ConsumingPassedActionsRegistersMisses()
        {
            var expected = new RhythmActionEvent(beat: 4f, durationBeats: 1f, RhythmActionType.Step, RhythmHand.Left, RhythmDirection.None);
            var session = new VRRhythmActionSession(new[] { expected }, hitWindowBeats: 0.25f);

            var missed = session.ConsumeMisses(currentBeat: 4.26f);

            Assert.That(missed, Is.EqualTo(1));
            Assert.That(session.SuccessCount, Is.EqualTo(0));
            Assert.That(session.MissCount, Is.EqualTo(1));
            Assert.That(session.NextEvent, Is.Null);
            Assert.That(session.TempoScale, Is.LessThan(1f));
        }

        [Test]
        public void GrappleUsesExpectedDurationInBeats()
        {
            var expected = new RhythmActionEvent(beat: 8f, durationBeats: 2f, RhythmActionType.Grapple, RhythmHand.Right, RhythmDirection.Up);
            var session = new VRRhythmActionSession(new[] { expected }, hitWindowBeats: 0.25f);
            var startInput = VRInputSnapshot.FromHands(
                default,
                new VRHandInputFrame { isPressed = true, direction = VRHandDirection.Up },
                bothHandsPressedTogether: false,
                secondsPerBeat: 1f);
            var input = VRInputSnapshot.FromHands(
                default,
                new VRHandInputFrame { isPressed = true, direction = VRHandDirection.Up, holdDuration = 2f },
                bothHandsPressedTogether: false,
                secondsPerBeat: 1f);

            Assert.That(session.TryConfirmGrappleStart(currentBeat: 8f, startInput), Is.True);

            var result = session.JudgeInput(currentBeat: 10f, input);

            Assert.That(result.Kind, Is.EqualTo(VRRhythmJudgmentKind.Hit));
            Assert.That(session.SuccessCount, Is.EqualTo(1));
        }

        [Test]
        public void KeyboardFallbackHoldUsesSameGrappleDurationMatching()
        {
            var expected = new RhythmActionEvent(beat: 8f, durationBeats: 2f, RhythmActionType.Grapple, RhythmHand.Right, RhythmDirection.Up);
            var session = new VRRhythmActionSession(new[] { expected }, hitWindowBeats: 0.25f);
            var startInput = VRKeyboardFallbackInput.CreateHoldSnapshot(
                expected,
                isPressed: true,
                wasPressedThisFrame: true,
                wasReleasedThisFrame: false,
                holdDuration: 0f,
                secondsPerBeat: 1f);
            var input = VRKeyboardFallbackInput.CreateHoldSnapshot(
                expected,
                isPressed: true,
                wasPressedThisFrame: false,
                wasReleasedThisFrame: false,
                holdDuration: 2f,
                secondsPerBeat: 1f);

            Assert.That(session.TryConfirmGrappleStart(currentBeat: 8f, startInput), Is.True);

            var result = session.JudgeInput(currentBeat: 10f, input);

            Assert.That(result.Kind, Is.EqualTo(VRRhythmJudgmentKind.Hit));
            Assert.That(session.SuccessCount, Is.EqualTo(1));
        }

        [Test]
        public void GrappleMissesAfterStartWindowWhenStartWasNotConfirmed()
        {
            var expected = new RhythmActionEvent(beat: 8f, durationBeats: 8f, RhythmActionType.Grapple, RhythmHand.Right, RhythmDirection.Up);
            var session = new VRRhythmActionSession(new[] { expected }, hitWindowBeats: 0.25f);

            var missed = session.ConsumeMisses(currentBeat: 8.26f);

            Assert.That(missed, Is.EqualTo(1));
            Assert.That(session.NextEvent, Is.Null);
            Assert.That(session.MissCount, Is.EqualTo(1));
        }

        [Test]
        public void GrappleStartConfirmationExtendsMissDeadlineToHoldEnd()
        {
            var expected = new RhythmActionEvent(beat: 8f, durationBeats: 8f, RhythmActionType.Grapple, RhythmHand.Right, RhythmDirection.Up);
            var session = new VRRhythmActionSession(new[] { expected }, hitWindowBeats: 0.25f);
            var startInput = VRKeyboardFallbackInput.CreateHoldSnapshot(
                expected,
                isPressed: true,
                wasPressedThisFrame: true,
                wasReleasedThisFrame: false,
                holdDuration: 0f,
                secondsPerBeat: 0.5f);

            Assert.That(session.TryConfirmGrappleStart(currentBeat: 8f, startInput), Is.True);

            var missed = session.ConsumeMisses(currentBeat: 8.26f);

            Assert.That(missed, Is.EqualTo(0));
            Assert.That(session.NextEvent, Is.SameAs(expected));
        }

        [Test]
        public void GrappleJudgmentWindowDoesNotOpenUntilCompletionAfterStart()
        {
            var expected = new RhythmActionEvent(beat: 8f, durationBeats: 8f, RhythmActionType.Grapple, RhythmHand.Right, RhythmDirection.Up);
            var session = new VRRhythmActionSession(new[] { expected }, hitWindowBeats: 0.25f);
            var startInput = VRKeyboardFallbackInput.CreateHoldSnapshot(
                expected,
                isPressed: true,
                wasPressedThisFrame: true,
                wasReleasedThisFrame: false,
                holdDuration: 0f,
                secondsPerBeat: 0.5f);

            Assert.That(session.TryConfirmGrappleStart(currentBeat: 8f, startInput), Is.True);

            Assert.That(session.IsInsideJudgmentWindow(currentBeat: 8.26f), Is.False);
            Assert.That(session.IsInsideJudgmentWindow(currentBeat: 15.75f), Is.True);
        }

        [Test]
        public void GrappleCanHitAtHoldCompletionBeat()
        {
            var expected = new RhythmActionEvent(beat: 8f, durationBeats: 8f, RhythmActionType.Grapple, RhythmHand.Right, RhythmDirection.Up);
            var session = new VRRhythmActionSession(new[] { expected }, hitWindowBeats: 0.25f);
            var startInput = VRKeyboardFallbackInput.CreateHoldSnapshot(
                expected,
                isPressed: true,
                wasPressedThisFrame: true,
                wasReleasedThisFrame: false,
                holdDuration: 0f,
                secondsPerBeat: 0.5f);
            var input = VRKeyboardFallbackInput.CreateHoldSnapshot(
                expected,
                isPressed: true,
                wasPressedThisFrame: false,
                wasReleasedThisFrame: false,
                holdDuration: 4f,
                secondsPerBeat: 0.5f);

            Assert.That(session.TryConfirmGrappleStart(currentBeat: 8f, startInput), Is.True);

            var result = session.JudgeInput(currentBeat: 16f, input);

            Assert.That(result.Kind, Is.EqualTo(VRRhythmJudgmentKind.Hit));
            Assert.That(session.SuccessCount, Is.EqualTo(1));
        }

        [Test]
        public void GrappleAcceptsHeldExpectedHandAtCompletionBeat()
        {
            var expected = new RhythmActionEvent(beat: 8f, durationBeats: 8f, RhythmActionType.Grapple, RhythmHand.Right, RhythmDirection.Up);
            var session = new VRRhythmActionSession(new[] { expected }, hitWindowBeats: 0.25f);
            var startInput = VRKeyboardFallbackInput.CreateHoldSnapshot(
                expected,
                isPressed: true,
                wasPressedThisFrame: true,
                wasReleasedThisFrame: false,
                holdDuration: 0f,
                secondsPerBeat: 0.5f);
            var partialHold = VRKeyboardFallbackInput.CreateHoldSnapshot(
                expected,
                isPressed: true,
                wasPressedThisFrame: false,
                wasReleasedThisFrame: false,
                holdDuration: 3.95f,
                secondsPerBeat: 0.5f);

            Assert.That(session.TryConfirmGrappleStart(currentBeat: 8f, startInput), Is.True);

            var result = session.JudgeInput(currentBeat: 16f, partialHold);

            Assert.That(result.Kind, Is.EqualTo(VRRhythmJudgmentKind.Hit));
            Assert.That(session.SuccessCount, Is.EqualTo(1));
        }

        [Test]
        public void GrappleFailsWhenAuthoredTriggerReleaseIsDetected()
        {
            var expected = new RhythmActionEvent(beat: 8f, durationBeats: 8f, RhythmActionType.Grapple, RhythmHand.Right, RhythmDirection.Up);
            var session = new VRRhythmActionSession(new[] { expected }, hitWindowBeats: 0.25f);
            var startInput = VRKeyboardFallbackInput.CreateHoldSnapshot(
                expected,
                isPressed: true,
                wasPressedThisFrame: true,
                wasReleasedThisFrame: false,
                holdDuration: 0f,
                secondsPerBeat: 0.5f);
            var releasedInput = VRKeyboardFallbackInput.CreateHoldSnapshot(
                expected,
                isPressed: false,
                wasPressedThisFrame: false,
                wasReleasedThisFrame: true,
                holdDuration: 1f,
                secondsPerBeat: 0.5f);

            Assert.That(session.TryConfirmGrappleStart(currentBeat: 8f, startInput), Is.True);

            Assert.That(session.TryFailGrappleContinuation(currentBeat: 10f, releasedInput, out var result), Is.True);
            Assert.That(result.Kind, Is.EqualTo(VRRhythmJudgmentKind.WrongInput));
            Assert.That(result.Event, Is.SameAs(expected));
            Assert.That(session.MissCount, Is.EqualTo(1));
            Assert.That(session.NextEvent, Is.Null);
        }

        [Test]
        public void GrappleIgnoresDroppedContinuationSamplesWithoutRelease()
        {
            var expected = new RhythmActionEvent(beat: 8f, durationBeats: 8f, RhythmActionType.Grapple, RhythmHand.Right, RhythmDirection.Up);
            var session = new VRRhythmActionSession(new[] { expected }, hitWindowBeats: 0.25f);
            var startInput = VRKeyboardFallbackInput.CreateHoldSnapshot(
                expected,
                isPressed: true,
                wasPressedThisFrame: true,
                wasReleasedThisFrame: false,
                holdDuration: 0f,
                secondsPerBeat: 0.5f);
            var droppedInput = VRKeyboardFallbackInput.CreateHoldSnapshot(
                expected,
                isPressed: false,
                wasPressedThisFrame: false,
                wasReleasedThisFrame: false,
                holdDuration: 1f,
                secondsPerBeat: 0.5f);

            Assert.That(session.TryConfirmGrappleStart(currentBeat: 8f, startInput), Is.True);

            Assert.That(session.TryFailGrappleContinuation(currentBeat: 9.01f, droppedInput, out _), Is.False);
            Assert.That(session.TryFailGrappleContinuation(currentBeat: 10.01f, droppedInput, out _), Is.False);
            Assert.That(session.TryFailGrappleContinuation(currentBeat: 11.01f, droppedInput, out _), Is.False);

            var result = session.JudgeInput(currentBeat: 16f, droppedInput);

            Assert.That(result.Kind, Is.EqualTo(VRRhythmJudgmentKind.Hit));
            Assert.That(session.SuccessCount, Is.EqualTo(1));
        }

        [Test]
        public void GrappleContinuationRecoversAfterDroppedSample()
        {
            var expected = new RhythmActionEvent(beat: 8f, durationBeats: 8f, RhythmActionType.Grapple, RhythmHand.Right, RhythmDirection.Up);
            var session = new VRRhythmActionSession(new[] { expected }, hitWindowBeats: 0.25f);
            var startInput = VRKeyboardFallbackInput.CreateHoldSnapshot(
                expected,
                isPressed: true,
                wasPressedThisFrame: true,
                wasReleasedThisFrame: false,
                holdDuration: 0f,
                secondsPerBeat: 0.5f);
            var droppedInput = VRKeyboardFallbackInput.CreateHoldSnapshot(
                expected,
                isPressed: false,
                wasPressedThisFrame: false,
                wasReleasedThisFrame: false,
                holdDuration: 1f,
                secondsPerBeat: 0.5f);
            var heldInput = VRKeyboardFallbackInput.CreateHoldSnapshot(
                expected,
                isPressed: true,
                wasPressedThisFrame: false,
                wasReleasedThisFrame: false,
                holdDuration: 1f,
                secondsPerBeat: 0.5f);

            Assert.That(session.TryConfirmGrappleStart(currentBeat: 8f, startInput), Is.True);

            Assert.That(session.TryFailGrappleContinuation(currentBeat: 10f, droppedInput, out _), Is.False);
            Assert.That(session.TryFailGrappleContinuation(currentBeat: 11f, heldInput, out _), Is.False);
            Assert.That(session.TryFailGrappleContinuation(currentBeat: 12f, droppedInput, out _), Is.False);
            Assert.That(session.MissCount, Is.EqualTo(0));
            Assert.That(session.NextEvent, Is.SameAs(expected));
        }

        [Test]
        public void GrappleAllowsDirectionDriftDuringHold()
        {
            var expected = new RhythmActionEvent(beat: 8f, durationBeats: 8f, RhythmActionType.Grapple, RhythmHand.Right, RhythmDirection.Up);
            var session = new VRRhythmActionSession(new[] { expected }, hitWindowBeats: 0.25f);
            var startInput = VRKeyboardFallbackInput.CreateHoldSnapshot(
                expected,
                isPressed: true,
                wasPressedThisFrame: true,
                wasReleasedThisFrame: false,
                holdDuration: 0f,
                secondsPerBeat: 0.5f);
            var wrongDirection = VRInputSnapshot.FromHands(
                default,
                new VRHandInputFrame { isPressed = true, direction = VRHandDirection.Left, holdDuration = 1f },
                bothHandsPressedTogether: false,
                secondsPerBeat: 0.5f);

            Assert.That(session.TryConfirmGrappleStart(currentBeat: 8f, startInput), Is.True);

            Assert.That(session.TryFailGrappleContinuation(currentBeat: 10f, wrongDirection, out _), Is.False);
            Assert.That(session.MissCount, Is.EqualTo(0));
            Assert.That(session.NextEvent, Is.SameAs(expected));
        }

        [Test]
        public void GrappleRequiresAuthoredHeldControllerForVrPlayback()
        {
            var expected = new RhythmActionEvent(beat: 8f, durationBeats: 8f, RhythmActionType.Grapple, RhythmHand.Right, RhythmDirection.Up);
            var session = new VRRhythmActionSession(new[] { expected }, hitWindowBeats: 0.25f);
            var rightHeldInput = VRInputSnapshot.FromHands(
                default,
                new VRHandInputFrame { isPressed = true, direction = VRHandDirection.Unknown, holdDuration = 4f },
                bothHandsPressedTogether: false,
                secondsPerBeat: 0.5f);
            var leftHeldInput = VRInputSnapshot.FromHands(
                new VRHandInputFrame { isPressed = true, direction = VRHandDirection.Unknown, holdDuration = 4f },
                default,
                bothHandsPressedTogether: false,
                secondsPerBeat: 0.5f);

            Assert.That(session.TryConfirmGrappleStart(currentBeat: 8f, leftHeldInput), Is.False);
            Assert.That(session.TryConfirmGrappleStart(currentBeat: 8f, rightHeldInput), Is.True);

            var result = session.JudgeInput(currentBeat: 16f, rightHeldInput);

            Assert.That(result.Kind, Is.EqualTo(VRRhythmJudgmentKind.Hit));
            Assert.That(session.SuccessCount, Is.EqualTo(1));
        }

        [Test]
        public void GrappleContinuationAcceptsHeldUpInputDuringHold()
        {
            var expected = new RhythmActionEvent(beat: 8f, durationBeats: 8f, RhythmActionType.Grapple, RhythmHand.Right, RhythmDirection.Up);
            var session = new VRRhythmActionSession(new[] { expected }, hitWindowBeats: 0.25f);
            var startInput = VRKeyboardFallbackInput.CreateHoldSnapshot(
                expected,
                isPressed: true,
                wasPressedThisFrame: true,
                wasReleasedThisFrame: false,
                holdDuration: 0f,
                secondsPerBeat: 0.5f);
            var heldInput = VRKeyboardFallbackInput.CreateHoldSnapshot(
                expected,
                isPressed: true,
                wasPressedThisFrame: false,
                wasReleasedThisFrame: false,
                holdDuration: 1f,
                secondsPerBeat: 0.5f);

            Assert.That(session.TryConfirmGrappleStart(currentBeat: 8f, startInput), Is.True);

            Assert.That(session.TryFailGrappleContinuation(currentBeat: 10f, heldInput, out _), Is.False);
            Assert.That(session.MissCount, Is.EqualTo(0));
            Assert.That(session.NextEvent, Is.SameAs(expected));
        }

        [Test]
        public void DistortionAmountStartsAfterTempoDropsBelowNinetyPercent()
        {
            var events = new[]
            {
                new RhythmActionEvent(beat: 1f, durationBeats: 1f, RhythmActionType.Step, RhythmHand.Left, RhythmDirection.None),
                new RhythmActionEvent(beat: 2f, durationBeats: 1f, RhythmActionType.Step, RhythmHand.Left, RhythmDirection.None),
                new RhythmActionEvent(beat: 3f, durationBeats: 1f, RhythmActionType.Step, RhythmHand.Left, RhythmDirection.None),
                new RhythmActionEvent(beat: 4f, durationBeats: 1f, RhythmActionType.Step, RhythmHand.Left, RhythmDirection.None)
            };
            var session = new VRRhythmActionSession(events, hitWindowBeats: 0.25f);

            session.ConsumeMisses(currentBeat: 1.26f);
            session.ConsumeMisses(currentBeat: 2.26f);
            session.ConsumeMisses(currentBeat: 3.26f);

            Assert.That(session.TempoScale, Is.EqualTo(0.91f).Within(0.0001f));
            Assert.That(session.DistortionAmount, Is.EqualTo(0f));

            session.ConsumeMisses(currentBeat: 4.26f);

            Assert.That(session.TempoScale, Is.EqualTo(0.88f).Within(0.0001f));
            Assert.That(session.DistortionAmount, Is.GreaterThan(0f));
        }

        [Test]
        public void KeyboardFallbackHoldCanBeTooShortForGrapple()
        {
            var expected = new RhythmActionEvent(beat: 8f, durationBeats: 2f, RhythmActionType.Grapple, RhythmHand.Right, RhythmDirection.Up);
            var session = new VRRhythmActionSession(new[] { expected }, hitWindowBeats: 0.25f);
            var startInput = VRKeyboardFallbackInput.CreateHoldSnapshot(
                expected,
                isPressed: true,
                wasPressedThisFrame: true,
                wasReleasedThisFrame: false,
                holdDuration: 0f,
                secondsPerBeat: 1f);
            var input = VRKeyboardFallbackInput.CreateHoldSnapshot(
                expected,
                isPressed: true,
                wasPressedThisFrame: false,
                wasReleasedThisFrame: false,
                holdDuration: 1.2f,
                secondsPerBeat: 1f);

            Assert.That(session.TryConfirmGrappleStart(currentBeat: 8f, startInput), Is.True);

            var result = session.JudgeInput(currentBeat: 10f, input);

            Assert.That(result.Kind, Is.EqualTo(VRRhythmJudgmentKind.WrongInput));
            Assert.That(session.SuccessCount, Is.EqualTo(0));
            Assert.That(session.MissCount, Is.EqualTo(1));
        }
    }
}
