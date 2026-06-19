using System.Collections.Generic;
using System.Linq;
using RhythmParkour;
using UnityEngine;

namespace BoringRun.VRInput
{
    public enum VRRhythmJudgmentKind
    {
        Hit,
        WrongInput,
        BadTiming
    }

    public readonly struct VRRhythmJudgmentResult
    {
        public VRRhythmJudgmentResult(VRRhythmJudgmentKind kind, RhythmActionEvent evt, float beatDelta)
        {
            Kind = kind;
            Event = evt;
            BeatDelta = beatDelta;
        }

        public VRRhythmJudgmentKind Kind { get; }

        public RhythmActionEvent Event { get; }

        public float BeatDelta { get; }
    }

    public sealed class VRRhythmActionJudge
    {
        readonly IReadOnlyList<RhythmActionEvent> events;
        readonly float hitWindowBeats;
        int nextEventIndex;
        bool grappleStartConfirmed;

        public VRRhythmActionJudge(IEnumerable<RhythmActionEvent> events, float hitWindowBeats)
        {
            this.events = (events ?? Enumerable.Empty<RhythmActionEvent>())
                .Where(evt => evt != null)
                .OrderBy(evt => evt.Beat)
                .ThenBy(evt => evt.ActionType)
                .ToArray();
            this.hitWindowBeats = hitWindowBeats < 0f ? 0f : hitWindowBeats;
        }

        public int SuccessCount { get; private set; }

        public int MissCount { get; private set; }

        public bool IsComplete => nextEventIndex >= events.Count;

        public RhythmActionEvent NextEvent => IsComplete ? null : events[nextEventIndex];

        public bool IsInsideWindow(float currentBeat)
        {
            if (IsComplete)
                return false;

            var nextEvent = events[nextEventIndex];
            return currentBeat >= nextEvent.Beat - hitWindowBeats
                && currentBeat <= nextEvent.Beat + hitWindowBeats;
        }

        public bool IsInsideJudgmentWindow(float currentBeat)
        {
            if (IsComplete)
                return false;

            var nextEvent = events[nextEventIndex];
            if (nextEvent.ActionType == RhythmActionType.Grapple)
            {
                if (!grappleStartConfirmed)
                    return IsInsideWindow(currentBeat);

                var completionBeat = GetGrappleCompletionBeat(nextEvent);
                return currentBeat >= completionBeat - hitWindowBeats
                    && currentBeat <= completionBeat + hitWindowBeats;
            }

            return currentBeat >= nextEvent.Beat - hitWindowBeats
                && currentBeat <= GetMissDeadlineBeat(nextEvent);
        }

        public bool TryConfirmGrappleStart(float currentBeat, VRInputSnapshot input)
        {
            var nextEvent = NextEvent;
            if (nextEvent == null || nextEvent.ActionType != RhythmActionType.Grapple)
                return false;

            if (grappleStartConfirmed)
                return false;

            if (!IsInsideWindow(currentBeat))
                return false;

            if (!VRExpectedActionInputMatcher.MatchesGrappleStart(nextEvent, input)
                && !VRExpectedActionInputMatcher.MatchesGrappleHeldHand(nextEvent, input))
                return false;

            grappleStartConfirmed = true;
            return true;
        }

        public bool TryFailGrappleContinuation(float currentBeat, VRInputSnapshot input, out VRRhythmJudgmentResult result)
        {
            result = default;

            var nextEvent = NextEvent;
            if (nextEvent == null || nextEvent.ActionType != RhythmActionType.Grapple || !grappleStartConfirmed)
                return false;

            if (currentBeat <= nextEvent.Beat + hitWindowBeats)
                return false;

            if (currentBeat > GetMissDeadlineBeat(nextEvent))
                return false;

            if (!VRExpectedActionInputMatcher.MatchesGrappleReleasedHand(nextEvent, input))
                return false;

            var beatDelta = currentBeat - nextEvent.Beat;
            nextEventIndex++;
            ResetGrappleState();
            MissCount++;
            result = new VRRhythmJudgmentResult(VRRhythmJudgmentKind.WrongInput, nextEvent, beatDelta);
            return true;
        }

        public VRRhythmJudgmentResult JudgeInput(float currentBeat, VRInputSnapshot input)
        {
            var nextEvent = NextEvent;
            if (nextEvent == null)
                return new VRRhythmJudgmentResult(VRRhythmJudgmentKind.BadTiming, null, 0f);

            var beatDelta = currentBeat - nextEvent.Beat;
            if (!IsInsideJudgmentWindow(currentBeat))
                return new VRRhythmJudgmentResult(VRRhythmJudgmentKind.BadTiming, nextEvent, beatDelta);

            var matched = MatchesJudgedInput(nextEvent, input);
            nextEventIndex++;
            ResetGrappleState();
            if (matched)
            {
                SuccessCount++;
                return new VRRhythmJudgmentResult(VRRhythmJudgmentKind.Hit, nextEvent, beatDelta);
            }

            MissCount++;
            return new VRRhythmJudgmentResult(VRRhythmJudgmentKind.WrongInput, nextEvent, beatDelta);
        }

        public VRRhythmJudgmentResult JudgeTimingOnly(float currentBeat)
        {
            var nextEvent = NextEvent;
            if (nextEvent == null)
                return new VRRhythmJudgmentResult(VRRhythmJudgmentKind.BadTiming, null, 0f);

            var beatDelta = currentBeat - nextEvent.Beat;
            if (!IsInsideWindow(currentBeat))
                return new VRRhythmJudgmentResult(VRRhythmJudgmentKind.BadTiming, nextEvent, beatDelta);

            nextEventIndex++;
            ResetGrappleState();
            SuccessCount++;
            return new VRRhythmJudgmentResult(VRRhythmJudgmentKind.Hit, nextEvent, beatDelta);
        }

        public int ConsumeMisses(float currentBeat)
        {
            return ConsumeMisses(currentBeat, null);
        }

        public int ConsumeMisses(float currentBeat, ICollection<RhythmActionEvent> missedEvents)
        {
            var missed = 0;

            while (!IsComplete && currentBeat > GetMissDeadlineBeat(events[nextEventIndex]))
            {
                missedEvents?.Add(events[nextEventIndex]);
                nextEventIndex++;
                ResetGrappleState();
                MissCount++;
                missed++;
            }

            return missed;
        }

        float GetMissDeadlineBeat(RhythmActionEvent evt)
        {
            if (evt.ActionType != RhythmActionType.Grapple)
                return evt.Beat + hitWindowBeats;

            if (!grappleStartConfirmed)
                return evt.Beat + hitWindowBeats;

            return GetGrappleCompletionBeat(evt) + hitWindowBeats;
        }

        static float GetGrappleCompletionBeat(RhythmActionEvent evt)
        {
            return evt.Beat + Mathf.Max(0f, evt.DurationBeats);
        }

        void ResetGrappleState()
        {
            grappleStartConfirmed = false;
        }

        bool MatchesJudgedInput(RhythmActionEvent evt, VRInputSnapshot input)
        {
            if (evt.ActionType == RhythmActionType.Grapple)
                return grappleStartConfirmed && !VRExpectedActionInputMatcher.MatchesGrappleReleasedHand(evt, input);

            return VRExpectedActionInputMatcher.Matches(evt, input);
        }
    }

    public sealed class VRRhythmActionSession
    {
        readonly VRRhythmActionJudge judge;
        readonly DynamicTempoState tempoState;
        readonly bool enableMissTempoPenalty;

        public VRRhythmActionSession(IEnumerable<RhythmActionEvent> events, float hitWindowBeats, bool enableMissTempoPenalty = true)
        {
            judge = new VRRhythmActionJudge(events, hitWindowBeats);
            tempoState = new DynamicTempoState();
            this.enableMissTempoPenalty = enableMissTempoPenalty;
        }

        public int SuccessCount => judge.SuccessCount;

        public int MissCount => judge.MissCount;

        public float TempoScale => tempoState.Scale;

        public float DistortionAmount
        {
            get
            {
                const float startScale = 0.9f;
                const float fullScale = 0.75f;
                var amount = (startScale - TempoScale) / (startScale - fullScale);
                return Mathf.Clamp01(amount);
            }
        }

        public bool IsDistorted => tempoState.IsDistorted;

        public bool IsFailed => tempoState.IsFailed;

        public RhythmActionEvent NextEvent => judge.NextEvent;

        public static float GetDropoutPulse(float distortionAmount, float phase)
        {
            if (distortionAmount <= 0f)
                return 0f;

            return phase < distortionAmount * 0.18f ? 1f : 0f;
        }

        public bool IsInsideWindow(float currentBeat)
        {
            return judge.IsInsideWindow(currentBeat);
        }

        public bool IsInsideJudgmentWindow(float currentBeat)
        {
            return judge.IsInsideJudgmentWindow(currentBeat);
        }

        public bool TryConfirmGrappleStart(float currentBeat, VRInputSnapshot input)
        {
            return judge.TryConfirmGrappleStart(currentBeat, input);
        }

        public bool TryFailGrappleContinuation(float currentBeat, VRInputSnapshot input, out VRRhythmJudgmentResult result)
        {
            if (!judge.TryFailGrappleContinuation(currentBeat, input, out result))
                return false;

            RegisterMissTempoPenalty();
            return true;
        }

        public VRRhythmJudgmentResult JudgeInput(float currentBeat, VRInputSnapshot input)
        {
            var result = judge.JudgeInput(currentBeat, input);
            if (result.Kind == VRRhythmJudgmentKind.Hit)
            {
                tempoState.RegisterSuccess();
            }
            else
            {
                RegisterMissTempoPenalty();
            }

            return result;
        }

        public VRRhythmJudgmentResult JudgeTimingOnly(float currentBeat)
        {
            var result = judge.JudgeTimingOnly(currentBeat);
            if (result.Kind == VRRhythmJudgmentKind.Hit)
            {
                tempoState.RegisterSuccess();
            }
            else
            {
                RegisterMissTempoPenalty();
            }

            return result;
        }

        public int ConsumeMisses(float currentBeat)
        {
            return ConsumeMisses(currentBeat, null);
        }

        public int ConsumeMisses(float currentBeat, ICollection<RhythmActionEvent> missedEvents)
        {
            var missed = judge.ConsumeMisses(currentBeat, missedEvents);
            for (var i = 0; i < missed; i++)
            {
                RegisterMissTempoPenalty();
            }

            return missed;
        }

        void RegisterMissTempoPenalty()
        {
            if (enableMissTempoPenalty)
                tempoState.RegisterMiss();
        }
    }
}
