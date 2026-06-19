using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RhythmParkour.Editor
{
    public static class SynthwaveBlockPreviewBuilder
    {
        private const string RequestPath = "Assets/Editor/RhythmParkour/RebuildSynthwaveBlockPreview.request";
        private const string ScenePath = "Assets/Scenes/SynthwaveBlockPreview.unity";
        private const string MaterialFolder = "Assets/Materials/Synthwave";
        private const string ShaderName = "RhythmParkour/Synthwave Grid Block";

        [InitializeOnLoadMethod]
        private static void RebuildIfRequested()
        {
            if (!File.Exists(RequestPath))
            {
                return;
            }

            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    EditorApplication.isPlaying = false;
                    EditorApplication.delayCall += RebuildIfRequested;
                    return;
                }

                BuildScene();
                AssetDatabase.DeleteAsset(RequestPath);
                AssetDatabase.Refresh();
            };
        }

        [MenuItem("Rhythm Parkour/Rebuild Synthwave Block Preview")]
        public static void BuildScene()
        {
            Directory.CreateDirectory(MaterialFolder);

            var shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError($"[RhythmParkour] Missing shader: {ShaderName}");
                return;
            }

            var cyan = CreateSynthwaveMaterial(
                "M_SynthwaveBlock_Cyan",
                shader,
                new Color(0.02f, 0.018f, 0.08f, 1f),
                new Color(0.05f, 0.95f, 1.0f, 1f),
                new Color(0.9f, 0.08f, 1.0f, 1f),
                0.5f,
                0.026f,
                0f);

            var magenta = CreateSynthwaveMaterial(
                "M_SynthwaveBlock_Magenta",
                shader,
                new Color(0.055f, 0.014f, 0.07f, 1f),
                new Color(0.95f, 0.08f, 1.0f, 1f),
                new Color(0.05f, 0.95f, 1.0f, 1f),
                0.45f,
                0.03f,
                0.25f);

            var red = CreateSynthwaveMaterial(
                "M_SynthwaveBlock_Red",
                shader,
                new Color(0.07f, 0.012f, 0.018f, 1f),
                new Color(1.0f, 0.2f, 0.12f, 1f),
                new Color(1.0f, 0.08f, 0.55f, 1f),
                0.42f,
                0.032f,
                0.55f);

            var blue = CreateSynthwaveMaterial(
                "M_SynthwaveBlock_BlueGoal",
                shader,
                new Color(0.014f, 0.025f, 0.09f, 1f),
                new Color(0.12f, 0.35f, 1.0f, 1f),
                new Color(0.0f, 0.95f, 1.0f, 1f),
                0.6f,
                0.024f,
                0.1f);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientLight = new Color(0.01f, 0.01f, 0.025f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.004f, 0.003f, 0.016f);
            RenderSettings.fogDensity = 0.018f;

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.002f, 0.001f, 0.014f);
            camera.fieldOfView = 48f;
            camera.transform.position = new Vector3(6.2f, 4.6f, -8.5f);
            camera.transform.rotation = Quaternion.Euler(27f, -36f, 0f);
            cameraObject.AddComponent<AudioListener>();

            var lightObject = new GameObject("Soft Fill Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.55f;
            light.color = new Color(0.6f, 0.78f, 1f);
            light.transform.rotation = Quaternion.Euler(40f, -35f, 0f);

            CreateCube("Step Platform - Cyan", new Vector3(-2.8f, 0f, 0f), new Vector3(2.3f, 0.35f, 1.25f), cyan);
            CreateCube("Side Grab Block - Magenta", new Vector3(0f, 1.15f, 1.1f), new Vector3(0.95f, 1.5f, 0.6f), magenta);
            CreateCube("Danger Timing Wall - Red", new Vector3(2.6f, 0.7f, 0.2f), new Vector3(1.7f, 1.4f, 0.32f), red);
            CreateCube("Goal Portal Bar - Blue", new Vector3(0f, 2.35f, 2.6f), new Vector3(5.4f, 0.32f, 0.32f), blue);
            CreateCube("Goal Portal Left Rail - Blue", new Vector3(-2.55f, 1.15f, 2.6f), new Vector3(0.32f, 2.1f, 0.32f), blue);
            CreateCube("Goal Portal Right Rail - Blue", new Vector3(2.55f, 1.15f, 2.6f), new Vector3(0.32f, 2.1f, 0.32f), blue);

            CreateCube("Open Space Reference Floor", new Vector3(0f, -0.28f, 1.2f), new Vector3(8f, 0.08f, 6.5f), cyan);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[RhythmParkour] Rebuilt {ScenePath} with {ShaderName}.");
        }

        private static Material CreateSynthwaveMaterial(
            string name,
            Shader shader,
            Color baseColor,
            Color gridColor,
            Color edgeColor,
            float gridSpacing,
            float lineThickness,
            float pulse)
        {
            var path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.SetColor("_BaseColor", baseColor);
            material.SetColor("_GridColor", gridColor);
            material.SetColor("_EdgeColor", edgeColor);
            material.SetFloat("_GridSpacing", gridSpacing);
            material.SetFloat("_LineThickness", lineThickness);
            material.SetFloat("_GridIntensity", 1.8f);
            material.SetFloat("_EdgeThickness", 0.008f);
            material.SetFloat("_EdgeIntensity", 2.4f);
            material.SetFloat("_RimIntensity", 0.55f);
            material.SetFloat("_RimPower", 3.8f);
            material.SetFloat("_Pulse", pulse);
            material.SetFloat("_PulseStrength", 1.25f);
            material.SetFloat("_ScrollSpeedX", 0.055f);
            material.SetFloat("_ScrollSpeedY", 0.12f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject CreateCube(string name, Vector3 position, Vector3 scale, Material material)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.position = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            return cube;
        }
    }
}
