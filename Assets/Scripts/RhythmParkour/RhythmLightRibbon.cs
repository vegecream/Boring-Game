using UnityEngine;

namespace RhythmParkour
{
    [ExecuteAlways]
    [RequireComponent(typeof(LineRenderer))]
    public sealed class RhythmLightRibbon : MonoBehaviour
    {
        [SerializeField, Min(4)] int segments = 96;
        [SerializeField] Vector3 startPoint = new Vector3(0f, 0.8f, 0f);
        [SerializeField] Vector3 startTangent = new Vector3(-2.2f, 1.1f, 8f);
        [SerializeField] Vector3 endTangent = new Vector3(2.2f, 1.5f, 18f);
        [SerializeField] Vector3 endPoint = new Vector3(0f, 1.0f, 28f);

        [Header("Shape")]
        [SerializeField, Min(0.01f)] float width = 0.75f;
        [SerializeField] AnimationCurve widthCurve = new AnimationCurve(
            new Keyframe(0f, 0.08f),
            new Keyframe(0.12f, 0.75f),
            new Keyframe(0.5f, 1f),
            new Keyframe(0.88f, 0.75f),
            new Keyframe(1f, 0.08f));

        [Header("Color")]
        [SerializeField] Color farColor = new Color(0.0f, 0.55f, 1.0f, 0.2f);
        [SerializeField] Color middleColor = new Color(0.75f, 0.12f, 1.0f, 0.55f);
        [SerializeField] Color nearColor = new Color(1.0f, 0.08f, 0.28f, 0.38f);

        [Header("Pulse")]
        [SerializeField] bool animatePulse = true;
        [SerializeField, Min(1f)] float bpm = 120f;
        [SerializeField, Range(0f, 1f)] float manualPulse;
        [SerializeField] string beatPulseProperty = "_BeatPulse";

        LineRenderer lineRenderer;
        MaterialPropertyBlock propertyBlock;
        Vector3[] positions;

        void Reset()
        {
            ApplyLineSettings();
            Rebuild();
        }

        void OnEnable()
        {
            ApplyLineSettings();
            Rebuild();
        }

        void OnValidate()
        {
            ApplyLineSettings();
            Rebuild();
        }

        void Update()
        {
            if (lineRenderer == null)
                lineRenderer = GetComponent<LineRenderer>();

            UpdatePulse();
        }

        public void Rebuild()
        {
            if (lineRenderer == null)
                lineRenderer = GetComponent<LineRenderer>();

            var count = Mathf.Max(4, segments);
            if (positions == null || positions.Length != count)
                positions = new Vector3[count];

            for (var i = 0; i < count; i++)
            {
                var t = count <= 1 ? 0f : i / (float)(count - 1);
                positions[i] = EvaluateCubic(startPoint, startTangent, endTangent, endPoint, t);
            }

            lineRenderer.positionCount = count;
            lineRenderer.SetPositions(positions);
            lineRenderer.colorGradient = BuildGradient();
        }

        void ApplyLineSettings()
        {
            if (lineRenderer == null)
                lineRenderer = GetComponent<LineRenderer>();

            lineRenderer.useWorldSpace = false;
            lineRenderer.alignment = LineAlignment.View;
            lineRenderer.textureMode = LineTextureMode.Tile;
            lineRenderer.numCornerVertices = 8;
            lineRenderer.numCapVertices = 8;
            lineRenderer.widthMultiplier = Mathf.Max(0.01f, width);
            lineRenderer.widthCurve = widthCurve ?? AnimationCurve.Linear(0f, 1f, 1f, 1f);
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
        }

        void UpdatePulse()
        {
            if (lineRenderer == null)
                return;

            if (propertyBlock == null)
                propertyBlock = new MaterialPropertyBlock();

            var pulse = manualPulse;
            if (animatePulse)
            {
                var phase = Time.realtimeSinceStartup * bpm / 60f;
                pulse = Mathf.Pow(Mathf.Sin(phase * Mathf.PI) * 0.5f + 0.5f, 3.2f);
            }

            lineRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(beatPulseProperty, Mathf.Clamp01(pulse));
            lineRenderer.SetPropertyBlock(propertyBlock);
        }

        Gradient BuildGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(farColor, 0f),
                    new GradientColorKey(middleColor, 0.52f),
                    new GradientColorKey(nearColor, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(farColor.a, 0f),
                    new GradientAlphaKey(middleColor.a, 0.52f),
                    new GradientAlphaKey(nearColor.a, 1f)
                });
            return gradient;
        }

        static Vector3 EvaluateCubic(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
        {
            var inv = 1f - t;
            return inv * inv * inv * a
                + 3f * inv * inv * t * b
                + 3f * inv * t * t * c
                + t * t * t * d;
        }
    }
}
