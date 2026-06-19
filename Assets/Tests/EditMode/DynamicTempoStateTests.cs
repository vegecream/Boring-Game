using NUnit.Framework;

namespace RhythmParkour.Tests
{
    public sealed class DynamicTempoStateTests
    {
        [Test]
        public void StartsAtNormalTempo()
        {
            var tempo = new DynamicTempoState();

            Assert.That(tempo.Scale, Is.EqualTo(1f));
            Assert.That(tempo.IsDistorted, Is.False);
            Assert.That(tempo.IsFailed, Is.False);
        }

        [Test]
        public void MissImmediatelyLowersTempo()
        {
            var tempo = new DynamicTempoState();

            tempo.RegisterMiss();

            Assert.That(tempo.Scale, Is.EqualTo(0.97f).Within(0.0001f));
        }

        [Test]
        public void SuccessGraduallyRestoresTempoWithoutExceedingNormal()
        {
            var tempo = new DynamicTempoState();
            tempo.RegisterMiss();

            tempo.RegisterSuccess();
            tempo.RegisterSuccess();
            tempo.RegisterSuccess();
            tempo.RegisterSuccess();

            Assert.That(tempo.Scale, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void DistortionAndFailureThresholdsFollowTempoScale()
        {
            var tempo = new DynamicTempoState();

            for (var i = 0; i < 4; i++)
            {
                tempo.RegisterMiss();
            }

            Assert.That(tempo.Scale, Is.LessThan(0.9f));
            Assert.That(tempo.IsDistorted, Is.True);
            Assert.That(tempo.IsFailed, Is.False);

            for (var i = 0; i < 5; i++)
            {
                tempo.RegisterMiss();
            }

            Assert.That(tempo.IsFailed, Is.True);
        }

        [Test]
        public void BeatClockUsesTempoScaleForBeatInterval()
        {
            var clock = new BeatClock(120f);

            Assert.That(clock.GetSecondsPerBeat(1f), Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(clock.GetSecondsPerBeat(0.5f), Is.EqualTo(1f).Within(0.0001f));
        }
    }
}
