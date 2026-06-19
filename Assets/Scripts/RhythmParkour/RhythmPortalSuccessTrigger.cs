using UnityEngine;
using UnityEngine.Events;

namespace RhythmParkour
{
    public sealed class RhythmPortalSuccessTrigger : MonoBehaviour
    {
        [SerializeField] Transform playerRoot;
        [SerializeField] MonoBehaviour actionSource;
        [SerializeField] float triggerRadius = 2.2f;
        [SerializeField] bool requirePlaybackStarted = true;
        [SerializeField] UnityEvent completed = new UnityEvent();

        bool isCompleted;
        string message;

        public bool IsCompleted => isCompleted;

        public UnityEvent Completed => completed;

        public void SetTriggerRadius(float radius)
        {
            triggerRadius = Mathf.Max(0.1f, radius);
        }

        void Awake()
        {
            ResolveReferences();
        }

        void Update()
        {
            if (isCompleted)
                return;

            ResolveReferences();
            if (!CanComplete())
                return;

            var target = playerRoot != null ? playerRoot : Camera.main != null ? Camera.main.transform : null;
            if (target == null)
                return;

            var flatDistance = Vector2.Distance(
                new Vector2(target.position.x, target.position.z),
                new Vector2(transform.position.x, transform.position.z));

            if (flatDistance <= triggerRadius)
                Complete();
        }

        void OnTriggerEnter(Collider other)
        {
            if (isCompleted || !CanComplete())
                return;

            if (playerRoot == null || other.transform == playerRoot || other.transform.IsChildOf(playerRoot))
                Complete();
        }

        void OnGUI()
        {
            if (!isCompleted)
                return;

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 42,
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = new Color(0.35f, 1f, 1f, 1f) }
            };

            GUI.Label(new Rect(0f, 82f, Screen.width, 64f), message, style);
        }

        void Complete()
        {
            isCompleted = true;
            message = "PORTAL REACHED";
            Debug.Log("[RhythmPortalSuccessTrigger] Portal reached.");
            completed.Invoke();
        }

        bool CanComplete()
        {
            if (requirePlaybackStarted && actionSource != null && !ReadBoolProperty(actionSource, "IsGameStarted", true))
                return false;

            return true;
        }

        void ResolveReferences()
        {
            if (actionSource == null)
                actionSource = FindActionSource();

            if (playerRoot != null)
                return;

            if (actionSource != null)
                playerRoot = actionSource.transform;

            if (playerRoot == null && Camera.main != null)
                playerRoot = Camera.main.transform;
        }

        static MonoBehaviour FindActionSource()
        {
            var behaviours = FindObjectsOfType<MonoBehaviour>();
            for (var i = 0; i < behaviours.Length; i++)
            {
                var behaviour = behaviours[i];
                if (behaviour != null && behaviour.GetType().Name == "VRRhythmActionPrototype")
                    return behaviour;
            }

            return null;
        }

        static bool ReadBoolProperty(MonoBehaviour source, string propertyName, bool fallback)
        {
            if (source == null)
                return fallback;

            var property = source.GetType().GetProperty(propertyName);
            if (property == null || property.PropertyType != typeof(bool))
                return fallback;

            return (bool)property.GetValue(source);
        }
    }
}
