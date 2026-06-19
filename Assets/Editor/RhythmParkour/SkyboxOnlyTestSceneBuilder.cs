using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RhythmParkour.Editor
{
    public static class SkyboxOnlyTestSceneBuilder
    {
        const string RequestPath = "Assets/Editor/RhythmParkour/RebuildSkyboxOnlyTest.request";
        const string ScenePath = "Assets/Scenes/SkyboxOnlyTest.unity";
        const string MaterialPath = "Assets/Materials/Stage/M_SkyboxOnlyHighContrast.mat";

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

        [MenuItem("Rhythm Parkour/Rebuild Skybox Only Test")]
        public static void BuildScene()
        {
            Directory.CreateDirectory("Assets/Materials/Stage");

            var skybox = CreateSkyboxMaterial();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.skybox = skybox;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.white * 0.08f;
            RenderSettings.fog = false;

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.backgroundColor = Color.magenta;
            camera.fieldOfView = 75f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 1000f;
            camera.transform.position = Vector3.zero;
            camera.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            cameraObject.AddComponent<AudioListener>();

            var markerMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            markerMaterial.SetColor("_BaseColor", new Color(0.1f, 0.95f, 1f, 1f));
            CreateMarkerCube("Small Cyan Center Marker", new Vector3(0f, -1.3f, 8f), markerMaterial);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[RhythmParkour] Rebuilt {ScenePath}.");
        }

        static Material CreateSkyboxMaterial()
        {
            var shader = Shader.Find("RhythmParkour/OpenSource/Keijiro Horizontal Skybox URP");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            material.shader = shader;
            material.SetColor("_Color1", new Color(0.0f, 0.7f, 1.0f, 1f));
            material.SetColor("_Color2", new Color(0.045f, 0.13f, 0.5f, 1f));
            material.SetColor("_Color3", new Color(0.46f, 0.08f, 0.48f, 1f));
            material.SetFloat("_Exponent1", 0.85f);
            material.SetFloat("_Exponent2", 0.85f);
            material.SetFloat("_Intensity", 1.55f);
            material.SetFloat("_Pulse", 0f);
            material.SetColor("_PulseColor", Color.cyan);
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

        static void CreateMarkerCube(string name, Vector3 position, Material material)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.position = position;
            cube.transform.localScale = Vector3.one * 0.5f;
            cube.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
