using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RhythmParkour.Editor
{
    public static class PortalPreviewBuilder
    {
        const string RequestPath = "Assets/Editor/RhythmParkour/RebuildPortalPreview.request";
        const string ScenePath = "Assets/Scenes/PortalPreview.unity";
        const string MaterialFolder = "Assets/Materials/Portal";
        const string PortalMaterialPath = MaterialFolder + "/M_RhythmPortal_Energy.mat";
        const string FrameMaterialPath = MaterialFolder + "/M_RhythmPortal_Frame.mat";
        const string ImportedPortalMaterialPath = "Assets/VOiD1 Gaming - Free Portal Shader Unity URP/Materials/Type B.mat";

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

        [MenuItem("Rhythm Parkour/Rebuild Portal Preview")]
        public static void BuildScene()
        {
            Directory.CreateDirectory(MaterialFolder);

            var skybox = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Stage/M_KeijiroCalmSynthwaveSkybox.mat");
            var portal = CreatePortalMaterial();
            var frame = CreateFrameMaterial();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = skybox;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.055f, 0.055f, 0.08f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.015f, 0.01f, 0.05f);
            RenderSettings.fogDensity = 0.006f;

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.fieldOfView = 58f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200f;
            camera.transform.position = new Vector3(0f, 1.45f, -9.5f);
            camera.transform.rotation = Quaternion.Euler(5f, 0f, 0f);
            cameraObject.AddComponent<AudioListener>();

            var lightObject = new GameObject("Soft Portal Key Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.38f;
            light.color = new Color(0.55f, 0.75f, 1f);
            light.transform.rotation = Quaternion.Euler(35f, -20f, 0f);

            var portalRoot = new GameObject("Rhythm Portal Preview");
            CreatePortalFace(portalRoot.transform, portal);
            CreateFramePiece("Portal Frame Top", portalRoot.transform, new Vector3(0f, 3.05f, 0f), new Vector3(5.2f, 0.22f, 0.35f), frame);
            CreateFramePiece("Portal Frame Bottom", portalRoot.transform, new Vector3(0f, -0.05f, 0f), new Vector3(5.2f, 0.22f, 0.35f), frame);
            CreateFramePiece("Portal Frame Left", portalRoot.transform, new Vector3(-2.55f, 1.5f, 0f), new Vector3(0.22f, 3.25f, 0.35f), frame);
            CreateFramePiece("Portal Frame Right", portalRoot.transform, new Vector3(2.55f, 1.5f, 0f), new Vector3(0.22f, 3.25f, 0.35f), frame);

            var platform = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Synthwave/M_SynthwaveBlock_Cyan.mat");
            CreateFramePiece("Simple Preview Floor", null, new Vector3(0f, -0.35f, -2.2f), new Vector3(4.8f, 0.18f, 4.6f), platform);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[RhythmParkour] Rebuilt {ScenePath}.");
        }

        static Material CreatePortalMaterial()
        {
            var source = AssetDatabase.LoadAssetAtPath<Material>(ImportedPortalMaterialPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(PortalMaterialPath);
            if (material == null)
            {
                material = new Material(source);
                AssetDatabase.CreateAsset(material, PortalMaterialPath);
            }

            material.shader = source.shader;
            material.CopyPropertiesFromMaterial(source);
            material.SetColor("Color_739931D1", new Color(0.55f, 0.08f, 2.4f, 1f));
            material.SetFloat("Vector1_FBBAF542", 0.32f);
            material.SetFloat("Vector1_964BC046", 8f);
            material.SetFloat("Vector1_469FF4D", 14f);
            material.SetFloat("Vector1_DBF011C", 5.5f);
            material.SetFloat("ENUM_1BB38181", 1f);
            material.EnableKeyword("ENUM_1BB38181_B");
            material.DisableKeyword("ENUM_1BB38181_A");
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material CreateFrameMaterial()
        {
            var shader = Shader.Find("RhythmParkour/Synthwave Grid Block");
            var material = AssetDatabase.LoadAssetAtPath<Material>(FrameMaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, FrameMaterialPath);
            }

            material.shader = shader;
            material.SetColor("_BaseColor", new Color(0.02f, 0.08f, 0.18f, 1f));
            material.SetColor("_GridColor", new Color(0.1f, 0.9f, 1.6f, 1f));
            material.SetColor("_EdgeColor", new Color(1.1f, 0.04f, 1.4f, 1f));
            material.SetFloat("_GridSpacing", 0.42f);
            material.SetFloat("_LineThickness", 0.045f);
            material.SetFloat("_GridIntensity", 2.4f);
            material.SetFloat("_EdgeThickness", 0.014f);
            material.SetFloat("_EdgeIntensity", 2.2f);
            material.SetFloat("_RimIntensity", 0.45f);
            material.SetFloat("_Pulse", 0.18f);
            EditorUtility.SetDirty(material);
            return material;
        }

        static void CreatePortalFace(Transform parent, Material material)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Portal Energy Surface";
            quad.transform.SetParent(parent, false);
            quad.transform.localPosition = new Vector3(0f, 1.5f, 0.03f);
            quad.transform.localScale = new Vector3(4.15f, 2.55f, 1f);
            quad.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(quad.GetComponent<Collider>());
        }

        static GameObject CreateFramePiece(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            if (parent != null)
            {
                cube.transform.SetParent(parent, false);
            }

            cube.transform.localPosition = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            return cube;
        }
    }
}
