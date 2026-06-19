using UnityEngine;

namespace BoringRun.VRInput
{
    public sealed class VRGuideLevelMarker : MonoBehaviour
    {
        [SerializeField] VRGuideLevelController controller;
        [SerializeField] VRGuideLevelDefinition level;

        [Header("Scene Objects")]
        [SerializeField] GameObject[] activeOnlyObjects;
        [SerializeField] GameObject[] inactiveOnlyObjects;
        [SerializeField] Renderer[] markerRenderers;
        [SerializeField] Color activeColor = new Color(0.15f, 0.95f, 0.55f, 1f);
        [SerializeField] Color inactiveColor = new Color(0.25f, 0.35f, 0.45f, 1f);
        [SerializeField] Color completedColor = new Color(0.55f, 0.8f, 1f, 1f);

        [Header("Floating Sign")]
        [SerializeField] Transform signRoot;
        [SerializeField] TextMesh signText;
        [SerializeField] bool hideSignWhenInactive = true;
        [SerializeField] bool faceMainCamera = true;
        [SerializeField] Transform lookTargetOverride;
        [SerializeField, Min(0f)] float activePulseScale = 0.08f;
        [SerializeField, Min(0.1f)] float pulseRate = 2f;

        Vector3 signBaseScale = Vector3.one;

        public VRGuideLevelDefinition Level => level;
        public bool IsCurrent => controller != null && controller.CurrentLevel == level;

        void Reset()
        {
            controller = FindObjectOfType<VRGuideLevelController>();
            signRoot = transform;
            signText = GetComponentInChildren<TextMesh>();
        }

        void Awake()
        {
            if (signRoot == null)
                signRoot = transform;

            signBaseScale = signRoot.localScale;
        }

        void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        void Update()
        {
            if (IsCurrent && activePulseScale > 0f)
            {
                var pulse = 1f + Mathf.Sin(Time.time * pulseRate * Mathf.PI * 2f) * activePulseScale;
                signRoot.localScale = signBaseScale * pulse;
            }

            if (faceMainCamera)
                FaceTargetCamera();
        }

        void OnDisable()
        {
            Unsubscribe();
        }

        public void Refresh()
        {
            var isCurrent = IsCurrent;
            var isCompleted = isCurrent && controller.CurrentLevelCompleted;
            SetObjectsActive(activeOnlyObjects, isCurrent);
            SetObjectsActive(inactiveOnlyObjects, !isCurrent);

            var signActive = isCurrent || !hideSignWhenInactive;
            if (signRoot != null)
                signRoot.gameObject.SetActive(signActive);

            if (signText != null)
                signText.text = GetSignMessage(isCurrent);

            ApplyMarkerColor(isCompleted ? completedColor : isCurrent ? activeColor : inactiveColor);

            if (!isCurrent && signRoot != null)
                signRoot.localScale = signBaseScale;
        }

        void Subscribe()
        {
            var source = controller != null ? controller : FindObjectOfType<VRGuideLevelController>();
            if (source == null)
                return;

            controller = source;
            controller.LevelStarted.AddListener(HandleLevelChanged);
            controller.LevelCompleted.AddListener(HandleLevelChanged);
            controller.MessageChanged.AddListener(HandleMessageChanged);
            controller.SequenceCompleted.AddListener(Refresh);
        }

        void Unsubscribe()
        {
            if (controller == null)
                return;

            controller.LevelStarted.RemoveListener(HandleLevelChanged);
            controller.LevelCompleted.RemoveListener(HandleLevelChanged);
            controller.MessageChanged.RemoveListener(HandleMessageChanged);
            controller.SequenceCompleted.RemoveListener(Refresh);
        }

        void HandleLevelChanged(VRGuideLevelDefinition changedLevel)
        {
            Refresh();
        }

        void HandleMessageChanged(string message)
        {
            if (!IsCurrent || signText == null)
                return;

            signText.text = message;
        }

        string GetSignMessage(bool isCurrent)
        {
            if (level == null)
                return "Guide marker";

            if (isCurrent && controller != null)
                return controller.CurrentMessage;

            return level.DisplayName;
        }

        void FaceTargetCamera()
        {
            var target = lookTargetOverride != null
                ? lookTargetOverride
                : Camera.main != null ? Camera.main.transform : null;

            if (target == null || signRoot == null)
                return;

            var toTarget = signRoot.position - target.position;
            if (toTarget.sqrMagnitude <= 0.0001f)
                return;

            signRoot.rotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
        }

        void ApplyMarkerColor(Color color)
        {
            for (var i = 0; i < markerRenderers.Length; i++)
            {
                var markerRenderer = markerRenderers[i];
                if (markerRenderer == null)
                    continue;

                markerRenderer.material.color = color;
            }
        }

        static void SetObjectsActive(GameObject[] objects, bool isActive)
        {
            for (var i = 0; i < objects.Length; i++)
            {
                if (objects[i] != null)
                    objects[i].SetActive(isActive);
            }
        }
    }
}
