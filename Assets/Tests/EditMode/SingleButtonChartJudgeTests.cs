using NUnit.Framework;

namespace RhythmParkour.Tests
{
    public sealed class SingleButtonChartJudgeTests
    {
        [Test]
        public void TryHitAcceptsNextEventInsideWindow()
        {
            var judge = new SingleButtonChartJudge(new[]
            {
                new RhythmActionEvent(beat: 4f, durationBeats: 1f, RhythmActionType.Step, RhythmHand.Left, RhythmDirection.None)
            }, hitWindowBeats: 0.25f);

            var hit = judge.TryHit(currentBeat: 4.2f, out var hitEvent);

            Assert.That(hit, Is.True);
            Assert.That(hitEvent.Beat, Is.EqualTo(4f));
            Assert.That(judge.SuccessCount, Is.EqualTo(1));
        }

        [Test]
        public void TryHitRejectsInputOutsideWindow()
        {
            var judge = new SingleButtonChartJudge(new[]
            {
                new RhythmActionEvent(beat: 4f, durationBeats: 1f, RhythmActionType.Step, RhythmHand.Left, RhythmDirection.None)
            }, hitWindowBeats: 0.25f);

            var hit = judge.TryHit(currentBeat: 3.5f, out _);

            Assert.That(hit, Is.False);
            Assert.That(judge.SuccessCount, Is.EqualTo(0));
        }

        [Test]
        public void ConsumeMissesSkipsEventsOlderThanWindow()
        {
            var judge = new SingleButtonChartJudge(new[]
            {
                new RhythmActionEvent(beat: 1f, durationBeats: 1f, RhythmActionType.Step, RhythmHand.Left, RhythmDirection.None),
                new RhythmActionEvent(beat: 2f, durationBeats: 1f, RhythmActionType.Step, RhythmHand.Right, RhythmDirection.None)
            }, hitWindowBeats: 0.25f);

            var missed = judge.ConsumeMisses(currentBeat: 1.4f);

            Assert.That(missed, Is.EqualTo(1));
            Assert.That(judge.MissCount, Is.EqualTo(1));
            Assert.That(judge.NextEvent.Beat, Is.EqualTo(2f));
        }
    }
}
