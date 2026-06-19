using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RhythmParkour
{
    [CreateAssetMenu(fileName = "RhythmActionChart", menuName = "Rhythm Parkour/Action Chart")]
    public sealed class RhythmActionChart : ScriptableObject
    {
        [SerializeField] private List<RhythmActionEvent> events = new();

        public IReadOnlyList<RhythmActionEvent> Events => events;

        public void SetEvents(IEnumerable<RhythmActionEvent> newEvents)
        {
            events = (newEvents ?? Enumerable.Empty<RhythmActionEvent>())
                .Where(evt => evt != null)
                .OrderBy(evt => evt.Beat)
                .ThenBy(evt => evt.ActionType)
                .ToList();
        }

        public void ConfigureForTests(IEnumerable<RhythmActionEvent> newEvents)
        {
            SetEvents(newEvents);
        }

        private void OnValidate()
        {
            events = events
                .Where(evt => evt != null)
                .OrderBy(evt => evt.Beat)
                .ThenBy(evt => evt.ActionType)
                .ToList();
        }
    }
}
