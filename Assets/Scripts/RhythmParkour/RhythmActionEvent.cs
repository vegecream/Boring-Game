using System;
using UnityEngine;

namespace RhythmParkour
{
    public enum RhythmActionType
    {
        Step,
        SideGrab,
        Slide,
        LongJump,
        Grapple
    }

    public enum RhythmHand
    {
        None,
        Left,
        Right,
        Both
    }

    public enum RhythmDirection
    {
        None,
        Up,
        Down,
        Left,
        Right
    }

    [Serializable]
    public sealed class RhythmActionEvent
    {
        [SerializeField] private float beat;
        [SerializeField] private float durationBeats = 1f;
        [SerializeField] private RhythmActionType actionType;
        [SerializeField] private RhythmHand hand;
        [SerializeField] private RhythmDirection direction;

        public RhythmActionEvent(float beat, float durationBeats, RhythmActionType actionType, RhythmHand hand, RhythmDirection direction)
        {
            this.beat = Mathf.Max(0f, beat);
            this.durationBeats = Mathf.Max(0f, durationBeats);
            this.actionType = actionType;
            this.hand = hand;
            this.direction = direction;
        }

        public float Beat => beat;

        public float DurationBeats => durationBeats;

        public RhythmActionType ActionType => actionType;

        public RhythmHand Hand => hand;

        public RhythmDirection Direction => direction;
    }
}
