using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RhythmParkour.Editor
{
    public static class RhythmLightRibbonPreviewBuilder
    {
        const string RequestPath = "Assets/Editor/RhythmParkour/RebuildRhythmLightRibbonPreview.request";
        const string ScenePath = "Assets/Scenes/RhythmLightRibbonPreview.unity";
        const string MaterialFolder = "Assets/Materials/VFX";
        const string BridgeMaterialPath = MaterialFolder + "/M_RhythmLightBridge.mat";
        const string StageMaterialFolder = "Assets/Materials/Stage";
        const string SkyboxPath = StageMaterialFolder + "/M_KeijiroCalmSynthwaveSkybox.mat";

        [InitializeOnLoadMethod]
        static void RebuildIfRequested()
        {
            if (!File.Exists(RequestPath))
                return;

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

        [MenuItem("Rhythm Parkour/Rebuild Rhythm Light Ribbon Preview")]
        public static void BuildScene()
        {
            Directory.CreateDirectory(MaterialFolder);
            Directory.CreateDirectory(StageMaterialFolder);

            var skybox = CreateSkyboxMaterial();
            var platform = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Synthwave/M_SynthwaveBlock_Cyan.mat");
            var magenta = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Synthwave/M_SynthwaveBlock_Magenta.mat");
            var goal = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Synthwave/M_SynthwaveBlock_BlueGoal.mat");
            var bridgeMaterial = CreateBridgeMaterial();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = skybox;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.035f, 0.045f, 0.075f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.006f, 0.007f, 0.03f);
            RenderSettings.fogDensity = 0.005f;

            CreateCamera();
            CreateLight();

            CreateCube("Departure Platform", new Vector3(0f, 0f, 0f), new Vector3(4.5f, 0.26f, 2.2f), platform);
            CreateCube("Arrival Platform", new Vector3(0f, 0.08f, 31.4f), new Vector3(4.0f, 0.24f, 2.0f), goal);
            CreateCube("Side Rhythm Marker A", new Vector3(-2.7f, 0.9f, 8f), new Vector3(0.16f, 1.1f, 0.16f), magenta);
            CreateCube("Side Rhythm Marker B", new Vector3(2.7f, 1.15f, 16f), new Vector3(0.16f, 1.5f, 0.16f), magenta);
            CreateCube("Side Rhythm Marker C", new Vector3(-2.7f, 1.0f, 24f), new Vector3(0.16f, 1.25f, 0.16f), magenta);

            var bridgeObject = new GameObject("Walkable Non-Parkour Light Bridge");
            bridgeObject.transform.position = new Vector3(0f, 0.86f, 1.25f);
            bridgeObject.AddComponent<MeshFilter>();
            var renderer = bridgeObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = bridgeMaterial;
            bridgeObject.AddComponent<BoxCollider>();
            var bridge = bridgeObject.AddComponent<RhythmLightBridge>();

            var serialized = new SerializedObject(bridge);
            serialized.FindProperty("length").floatValue = 28.4f;
            serialized.FindProperty("width").floatValue = 3.2f;
            serialized.FindProperty("surfaceThickness").floatValue = 0.16f;
            serialized.FindProperty("lengthSegments").intValue = 40;
            serialized.FindProperty("bpm").floatValue = 120f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            bridge.Rebuild();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[RhythmParkour] Rebuilt {ScenePath}.");
        }

        static Material CreateSkyboxMaterial()
        {
            var shader = Shader.Find("RhythmParkour/OpenSource/Keijiro Horizontal Skybox URP");
            var material = AssetDatabase.LoadAssetAtPath<Material>(SkyboxPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, SkyboxPath);
            }

            material.shader = shader;
            ApplySkyboxSettings(material);
            return material;
        }

        static Material CreateBridgeMaterial()
        {
            var shader = Shader.Find("RhythmParkour/Rhythm Light Bridge");
            var material = AssetDatabase.LoadAssetAtPath<Material>(BridgeMaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, BridgeMaterialPath);
            }

            material.shader = shader;
            material.SetColor("_BaseColor", new Color(0.07f, 0.22f, 0.62f, 1f));
            material.SetColor("_OutlineColor", new Color(0.62f, 0.96f, 1f, 1f));
            material.SetFloat("_Alpha", 0.42f);
            material.SetFloat("_Emission", 1.55f);
            material.SetFloat("_OutlineWorldWidth", 0.08f);
            material.SetFloat("_OutlineIntensity", 5.2f);
            material.SetFloat("_PulseStrength", 0.32f);
            EditorUtility.SetDirty(material);
            return material;
        }

        static void ApplySkyboxSettings(Material material)
        {
            material.SetColor("_Color1", new Color(0f, 0.7f, 1f, 1f));
            material.SetColor("_Color2", new Color(0.045f, 0.13f, 0.5f, 1f));
            material.SetColor("_Color3", new Color(0.46f, 0.08f, 0.48f, 1f));
            material.SetFloat("_Exponent1", 0.85f);
            material.SetFloat("_Exponent2", 0.85f);
            material.SetFloat("_Intensity", 1.55f);
            material.SetFloat("_Pulse", 0f);
            material.SetColor("_PulseColor", new Color(0f, 1f, 1f, 1f));
            material.SetFloat("_PulseStrength", 0f);
            material.SetColor("_CloudColor", new Color(0.38f, 0.78f, 1f, 1f));
            material.SetFloat("_CloudStrength", 0.24f);
            material.SetFloat("_CloudCoverage", 0.54f);
            material.SetFloat("_CloudHeight", 0.62f);
            material.SetFloat("_CloudThickness", 0.24f);
            material.SetFloat("_CloudScale", 2.7f);
            material.SetFloat("_CloudDrift", 0.012f);
            EditorUtility.SetDirty(material);
        }

        static void CreateCamera()
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.fieldOfView = 64f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 500f;
            camera.transform.position = new Vector3(0f, 3.0f, -8.8f);
            camera.transform.rotation = Quaternion.Euler(10.5f, 0f, 0f);
            cameraObject.AddComponent<AudioListener>();
        }

        static void CreateLight()
        {
            var lightObject = new GameObject("Soft Bridge Key Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.35f;
            light.color = new Color(0.5f, 0.75f, 1f);
            light.transform.rotation = Quaternion.Euler(36f, -20f, 0f);
        }

        static GameObject CreateCube(string name, Vector3 position, Vector3 scale, Material material)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.position = position;
            cube.transform.localScale = scale;
            var renderer = cube.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            return cube;
        }
    }
}
