using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace RhythmParkour.Tests
{
    public sealed class RhythmChartConfigTests
    {
        [Test]
        public void TrackConvertsBeatIndexToSecondsUsingBpmOffsetAndTempoScale()
        {
            var track = ScriptableObject.CreateInstance<RhythmTrackConfig>();
            track.ConfigureForTests(audioClip: null, baseBpm: 120f, firstBeatOffsetSeconds: 0.25f, chart: null);

            Assert.That(track.GetSecondsAtBeat(8f, tempoScale: 1f), Is.EqualTo(4.25f).Within(0.0001f));
            Assert.That(track.GetSecondsAtBeat(8f, tempoScale: 0.5f), Is.EqualTo(8.25f).Within(0.0001f));

            Object.DestroyImmediate(track);
        }

        [Test]
        public void ActionChartReturnsEventsInBeatOrder()
        {
            var chart = ScriptableObject.CreateInstance<RhythmActionChart>();
            chart.ConfigureForTests(new[]
            {
                new RhythmActionEvent(beat: 8f, durationBeats: 1f, RhythmActionType.Slide, RhythmHand.Both, RhythmDirection.Down),
                new RhythmActionEvent(beat: 2f, durationBeats: 1f, RhythmActionType.Step, RhythmHand.Right, RhythmDirection.None),
                new RhythmActionEvent(beat: 1f, durationBeats: 1f, RhythmActionType.Step, RhythmHand.Left, RhythmDirection.None)
            });

            var beats = chart.Events.Select(evt => evt.Beat).ToArray();

            Assert.That(beats, Is.EqualTo(new[] { 1f, 2f, 8f }));

            Object.DestroyImmediate(chart);
        }

        [Test]
        public void TrackReferencesActionChart()
        {
            var chart = ScriptableObject.CreateInstance<RhythmActionChart>();
            var track = ScriptableObject.CreateInstance<RhythmTrackConfig>();

            track.ConfigureForTests(audioClip: null, baseBpm: 120f, firstBeatOffsetSeconds: 0f, chart: chart);

            Assert.That(track.Chart, Is.SameAs(chart));

            Object.DestroyImmediate(track);
            Object.DestroyImmediate(chart);
        }
    }
}
