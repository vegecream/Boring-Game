using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BoringRun.VRInput
{
    [CreateAssetMenu(fileName = "VRGuideSequence", menuName = "Boring Run/VR Guide Sequence")]
    public sealed class VRGuideSequence : ScriptableObject
    {
        [SerializeField] List<VRGuideLevelDefinition> levels = new();

        public IReadOnlyList<VRGuideLevelDefinition> Levels => levels;

        public int Count => levels.Count(level => level != null);

        public VRGuideLevelDefinition GetLevel(int index)
        {
            if (index < 0 || index >= levels.Count)
                return null;

            return levels[index];
        }

        void OnValidate()
        {
            levels = levels.Where(level => level != null).ToList();
        }
    }
}
