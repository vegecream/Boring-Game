using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RhythmParkour.Editor
{
    public static class SynthwaveStageBackgroundBuilder
    {
        const string RequestPath = "Assets/Editor/RhythmParkour/RebuildSynthwaveStageBackground.request";
        const string ScenePath = "Assets/Scenes/SynthwaveStageBackgroundPreview.unity";
        const string MaterialFolder = "Assets/Materials/Stage";
        const string SkyboxPath = MaterialFolder + "/M_KeijiroCalmSynthwaveSkybox.mat";

        [InitializeOnLoadMethod]
        static void RebuildIfRequested()
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

        [MenuItem("Rhythm Parkour/Rebuild Synthwave Stage Background Preview")]
        public static void BuildScene()
        {
            Directory.CreateDirectory(MaterialFolder);

            var skybox = CreateSkyboxMaterial();
            var platform = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Synthwave/M_SynthwaveBlock_Cyan.mat");
            var goal = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Synthwave/M_SynthwaveBlock_BlueGoal.mat");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = skybox;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.045f, 0.045f, 0.075f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.006f, 0.006f, 0.028f);
            RenderSettings.fogDensity = 0.004f;

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.fieldOfView = 58f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 500f;
            camera.transform.position = new Vector3(0f, 2.2f, -13f);
            camera.transform.rotation = Quaternion.Euler(8f, 0f, 0f);
            cameraObject.AddComponent<AudioListener>();

            var lightObject = new GameObject("Soft Blue Key Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.45f;
            light.color = new Color(0.55f, 0.75f, 1f);
            light.transform.rotation = Quaternion.Euler(36f, -25f, 0f);

            CreateCube("Preview Platform A", new Vector3(0f, 0f, 0f), new Vector3(3.8f, 0.32f, 1.6f), platform);
            CreateCube("Preview Platform B", new Vector3(0f, 0.18f, 9f), new Vector3(3.2f, 0.28f, 1.4f), platform);
            CreateCube("Preview Platform C", new Vector3(0f, 0.36f, 18f), new Vector3(2.7f, 0.26f, 1.25f), platform);

            CreateCube("Quiet Blue Goal Top", new Vector3(0f, 3.1f, 32f), new Vector3(6.2f, 0.28f, 0.28f), goal);
            CreateCube("Quiet Blue Goal Left", new Vector3(-3f, 1.6f, 32f), new Vector3(0.28f, 2.8f, 0.28f), goal);
            CreateCube("Quiet Blue Goal Right", new Vector3(3f, 1.6f, 32f), new Vector3(0.28f, 2.8f, 0.28f), goal);

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
            return material;
        }

        static GameObject CreateCube(string name, Vector3 position, Vector3 scale, Material material)
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
