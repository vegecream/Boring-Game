using System.Collections.Generic;
using UnityEngine;

namespace RhythmParkour
{
    public static class RhythmTrackPositionMapper
    {
        public const float DefaultGrappleExtraUnitsPerBeat = 1.18f;

        public static float GetZ(
            float beat,
            IReadOnlyList<RhythmActionEvent> events,
            float unitsPerBeat,
            float grappleExtraUnitsPerBeat = DefaultGrappleExtraUnitsPerBeat)
        {
            var z = beat * unitsPerBeat;
            if (events == null || grappleExtraUnitsPerBeat <= 0f)
                return z;

            for (var i = 0; i < events.Count; i++)
            {
                var evt = events[i];
                if (evt == null || evt.ActionType != RhythmActionType.Grapple)
                    continue;

                var duration = Mathf.Max(0f, evt.DurationBeats);
                if (duration <= 0f || beat <= evt.Beat)
                    continue;

                var progress = Mathf.Clamp01((beat - evt.Beat) / duration);
                z += duration * grappleExtraUnitsPerBeat * Mathf.SmoothStep(0f, 1f, progress);
            }

            return z;
        }

        public static float GetSegmentLength(
            RhythmActionEvent evt,
            IReadOnlyList<RhythmActionEvent> events,
            float unitsPerBeat,
            float grappleExtraUnitsPerBeat = DefaultGrappleExtraUnitsPerBeat)
        {
            if (evt == null)
                return 0f;

            var start = GetZ(evt.Beat, events, unitsPerBeat, grappleExtraUnitsPerBeat);
            var end = GetZ(evt.Beat + Mathf.Max(0f, evt.DurationBeats), events, unitsPerBeat, grappleExtraUnitsPerBeat);
            return Mathf.Max(0f, end - start);
        }
    }
}
