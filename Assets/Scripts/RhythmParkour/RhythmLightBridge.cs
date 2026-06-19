using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RhythmParkour
{
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(BoxCollider))]
    public sealed class RhythmLightBridge : MonoBehaviour
    {
        [SerializeField, Min(1f)] float length = 28f;
        [SerializeField, Min(0.5f)] float width = 3.2f;
        [SerializeField, Min(0.01f)] float surfaceThickness = 0.12f;
        [SerializeField, Min(1)] int lengthSegments = 28;

        [Header("Color Ramp")]
        [SerializeField] Color farColor = new Color(0.0f, 0.45f, 1.0f, 0.72f);
        [SerializeField] Color middleColor = new Color(0.62f, 0.12f, 1.0f, 0.88f);
        [SerializeField] Color nearColor = new Color(1.0f, 0.08f, 0.28f, 0.78f);

        [Header("Pulse")]
        [SerializeField] bool animatePulse = true;
        [SerializeField, Min(1f)] float bpm = 120f;
        [SerializeField, Range(0f, 1f)] float manualPulse;
        [SerializeField] string beatPulseProperty = "_BeatPulse";
        [SerializeField] string bridgeLengthProperty = "_BridgeLength";
        [SerializeField] string bridgeWidthProperty = "_BridgeWidth";

        MeshFilter meshFilter;
        MeshRenderer meshRenderer;
        BoxCollider bridgeCollider;
        MaterialPropertyBlock propertyBlock;
        Mesh mesh;

        public float Length
        {
            get => length;
            set
            {
                length = Mathf.Max(1f, value);
                Rebuild();
            }
        }

        public float Width
        {
            get => width;
            set
            {
                width = Mathf.Max(0.5f, value);
                Rebuild();
            }
        }

        void Reset()
        {
            Rebuild();
        }

        void OnEnable()
        {
            Rebuild();
        }

        void OnValidate()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorApplication.delayCall += () =>
                {
                    if (this != null)
                        Rebuild();
                };
                return;
            }
#endif

            Rebuild();
        }

        void Update()
        {
            UpdatePulse();
        }

        public void Rebuild()
        {
            EnsureComponents();
            BuildBridgeMesh();
            UpdateCollider();
        }

        void EnsureComponents()
        {
            if (meshFilter == null)
                meshFilter = GetComponent<MeshFilter>();

            if (meshRenderer == null)
                meshRenderer = GetComponent<MeshRenderer>();

            if (bridgeCollider == null)
                bridgeCollider = GetComponent<BoxCollider>();

            if (mesh == null)
            {
                mesh = new Mesh { name = "Rhythm Light Bridge Mesh" };
                meshFilter.sharedMesh = mesh;
            }
            else if (meshFilter.sharedMesh != mesh)
            {
                meshFilter.sharedMesh = mesh;
            }
        }

        void BuildBridgeMesh()
        {
            var segmentCount = Mathf.Max(1, lengthSegments);
            var columnCount = segmentCount + 1;
            var vertices = new Vector3[columnCount * 2];
            var normals = new Vector3[vertices.Length];
            var uvs = new Vector2[vertices.Length];
            var colors = new Color[vertices.Length];
            var triangles = new int[segmentCount * 6];

            for (var i = 0; i < columnCount; i++)
            {
                var t = i / (float)segmentCount;
                var z = Mathf.Lerp(0f, length, t);
                var color = EvaluateGradient(t);
                var leftIndex = i * 2;
                var rightIndex = leftIndex + 1;

                vertices[leftIndex] = new Vector3(-width * 0.5f, 0f, z);
                vertices[rightIndex] = new Vector3(width * 0.5f, 0f, z);
                normals[leftIndex] = Vector3.up;
                normals[rightIndex] = Vector3.up;
                uvs[leftIndex] = new Vector2(t, 0f);
                uvs[rightIndex] = new Vector2(t, 1f);
                colors[leftIndex] = color;
                colors[rightIndex] = color;
            }

            var tri = 0;
            for (var i = 0; i < segmentCount; i++)
            {
                var left = i * 2;
                var right = left + 1;
                var nextLeft = left + 2;
                var nextRight = left + 3;

                triangles[tri++] = left;
                triangles[tri++] = nextLeft;
                triangles[tri++] = right;
                triangles[tri++] = right;
                triangles[tri++] = nextLeft;
                triangles[tri++] = nextRight;
            }

            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
        }

        void UpdateCollider()
        {
            if (bridgeCollider == null)
                return;

            bridgeCollider.center = new Vector3(0f, -surfaceThickness * 0.5f, length * 0.5f);
            bridgeCollider.size = new Vector3(width, surfaceThickness, length);
        }

        void UpdatePulse()
        {
            EnsureComponents();

            if (propertyBlock == null)
                propertyBlock = new MaterialPropertyBlock();

            var pulse = manualPulse;
            if (animatePulse)
            {
                var phase = Time.realtimeSinceStartup * bpm / 60f;
                pulse = Mathf.Pow(Mathf.Sin(phase * Mathf.PI) * 0.5f + 0.5f, 3.2f);
            }

            meshRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(beatPulseProperty, Mathf.Clamp01(pulse));
            propertyBlock.SetFloat(bridgeLengthProperty, length);
            propertyBlock.SetFloat(bridgeWidthProperty, width);
            meshRenderer.SetPropertyBlock(propertyBlock);
        }

        Color EvaluateGradient(float t)
        {
            if (t < 0.55f)
                return Color.Lerp(farColor, middleColor, Mathf.SmoothStep(0f, 1f, t / 0.55f));

            return Color.Lerp(middleColor, nearColor, Mathf.SmoothStep(0f, 1f, (t - 0.55f) / 0.45f));
        }
    }
}
