using NUnit.Framework;

namespace RhythmParkour.Tests
{
    public sealed class SingleButtonChartSessionTests
    {
        [Test]
        public void PressInsideWindowHitsAndKeepsNormalTempo()
        {
            var session = new SingleButtonChartSession(new[]
            {
                new RhythmActionEvent(beat: 2f, durationBeats: 1f, RhythmActionType.Step, RhythmHand.Left, RhythmDirection.None)
            }, hitWindowBeats: 0.25f);

            var result = session.Press(currentBeat: 2f);

            Assert.That(result.Kind, Is.EqualTo(SingleButtonChartInputKind.Hit));
            Assert.That(result.Event.Beat, Is.EqualTo(2f));
            Assert.That(session.TempoScale, Is.EqualTo(1f));
        }

        [Test]
        public void BadTimingPressLowersTempo()
        {
            var session = new SingleButtonChartSession(new[]
            {
                new RhythmActionEvent(beat: 2f, durationBeats: 1f, RhythmActionType.Step, RhythmHand.Left, RhythmDirection.None)
            }, hitWindowBeats: 0.25f);

            var result = session.Press(currentBeat: 1f);

            Assert.That(result.Kind, Is.EqualTo(SingleButtonChartInputKind.BadTiming));
            Assert.That(session.TempoScale, Is.EqualTo(0.97f).Within(0.0001f));
        }

        [Test]
        public void MissedWallsLowerTempoOncePerMissedEvent()
        {
            var session = new SingleButtonChartSession(new[]
            {
                new RhythmActionEvent(beat: 2f, durationBeats: 1f, RhythmActionType.Step, RhythmHand.Left, RhythmDirection.None),
                new RhythmActionEvent(beat: 3f, durationBeats: 1f, RhythmActionType.Step, RhythmHand.Right, RhythmDirection.None)
            }, hitWindowBeats: 0.25f);

            var missed = session.ConsumeMisses(currentBeat: 3.4f);

            Assert.That(missed, Is.EqualTo(2));
            Assert.That(session.TempoScale, Is.EqualTo(0.94f).Within(0.0001f));
        }

        [Test]
        public void SuccessAfterMissGraduallyRestoresTempo()
        {
            var session = new SingleButtonChartSession(new[]
            {
                new RhythmActionEvent(beat: 2f, durationBeats: 1f, RhythmActionType.Step, RhythmHand.Left, RhythmDirection.None),
                new RhythmActionEvent(beat: 3f, durationBeats: 1f, RhythmActionType.Step, RhythmHand.Right, RhythmDirection.None)
            }, hitWindowBeats: 0.25f);

            session.Press(currentBeat: 1f);
            session.Press(currentBeat: 2f);

            Assert.That(session.TempoScale, Is.EqualTo(0.98f).Within(0.0001f));
        }

        [Test]
        public void DistortionAmountStartsAfterTempoDropsBelowNinetyPercent()
        {
            var session = new SingleButtonChartSession(new[]
            {
                new RhythmActionEvent(beat: 2f, durationBeats: 1f, RhythmActionType.Step, RhythmHand.Left, RhythmDirection.None)
            }, hitWindowBeats: 0.25f);

            session.Press(currentBeat: 1f);
            session.Press(currentBeat: 1f);
            session.Press(currentBeat: 1f);

            Assert.That(session.TempoScale, Is.EqualTo(0.91f).Within(0.0001f));
            Assert.That(session.DistortionAmount, Is.EqualTo(0f));

            session.Press(currentBeat: 1f);

            Assert.That(session.TempoScale, Is.EqualTo(0.88f).Within(0.0001f));
            Assert.That(session.DistortionAmount, Is.GreaterThan(0f));
        }

        [Test]
        public void DistortionAmountReachesFullAtFailureThreshold()
        {
            var session = new SingleButtonChartSession(new[]
            {
                new RhythmActionEvent(beat: 2f, durationBeats: 1f, RhythmActionType.Step, RhythmHand.Left, RhythmDirection.None)
            }, hitWindowBeats: 0.25f);

            for (var i = 0; i < 9; i++)
            {
                session.Press(currentBeat: 1f);
            }

            Assert.That(session.TempoScale, Is.LessThan(0.75f));
            Assert.That(session.DistortionAmount, Is.EqualTo(1f));
        }

        [Test]
        public void DropoutPulseOnlyAppearsDuringDistortionWindow()
        {
            Assert.That(SingleButtonChartSession.GetDropoutPulse(0f, 0f), Is.EqualTo(0f));
            Assert.That(SingleButtonChartSession.GetDropoutPulse(1f, 0.01f), Is.EqualTo(1f));
        }
    }
}
