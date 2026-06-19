using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace RhythmParkour.Editor
{
    public static class NeoCityBareImportTestBuilder
    {
        const string RequestPath = "Assets/Editor/RhythmParkour/RebuildNeoCityBareImportTest.request";
        const string ScenePath = "Assets/Scenes/NeoCityBareImportTest.unity";
        const string KitRoot = "Assets/External/KitBash3D/NeoCity/neocity";
        const string SourceModelPath = KitRoot + "/KB3D_NEC_BldgSM_A.fbx";
        const string MaterialFolder = "Assets/Materials/Stage";
        const string ForcedUnlitMaterialPath = MaterialFolder + "/M_NeoCityBareForcedUnlit.mat";
        const string ReferenceMaterialPath = MaterialFolder + "/M_NeoCityBareReference.mat";

        [InitializeOnLoadMethod]
        static void RebuildIfRequested()
        {
            if (!File.Exists(RequestPath))
                return;

            EditorApplication.delayCall += () =>
            {
                BuildScene();
                AssetDatabase.DeleteAsset(RequestPath);
                AssetDatabase.Refresh();
            };
        }

        [MenuItem("Rhythm Parkour/Rebuild Neo City Bare Import Test")]
        public static void BuildScene()
        {
            Directory.CreateDirectory(MaterialFolder);
            ConfigureModelImporter();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.78f, 0.82f, 0.88f);

            var reference = CreateUrpMaterial(ReferenceMaterialPath, new Color(0.18f, 0.22f, 0.28f, 1f));
            var forcedUnlit = CreateUnlitMaterial(ForcedUnlitMaterialPath, new Color(0.0f, 0.9f, 1f, 1f));

            CreateCamera();
            CreateLight();
            CreateReferenceObjects(reference);

            var original = InstantiateModel("Original KitBash Import", new Vector3(-16f, 0f, 0f), null);
            var forced = InstantiateModel("Forced Cyan Unlit Import", new Vector3(16f, 0f, 0f), forcedUnlit);
            FrameImportedModel(original, forced);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[RhythmParkour] Rebuilt {ScenePath}.");
        }

        static void ConfigureModelImporter()
        {
            var importer = AssetImporter.GetAtPath(SourceModelPath) as ModelImporter;
            if (importer == null)
                return;

            var changed = false;
            if (importer.importCameras)
            {
                importer.importCameras = false;
                changed = true;
            }

            if (importer.importLights)
            {
                importer.importLights = false;
                changed = true;
            }

            if (importer.addCollider)
            {
                importer.addCollider = false;
                changed = true;
            }

            if (importer.importVisibility)
            {
                importer.importVisibility = false;
                changed = true;
            }

            if (changed)
                importer.SaveAndReimport();
        }

        static GameObject InstantiateModel(string name, Vector3 targetCenter, Material overrideMaterial)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(SourceModelPath);
            if (asset == null)
            {
                Debug.LogWarning($"[RhythmParkour] Missing KitBash asset at {SourceModelPath}.");
                return null;
            }

            var instance = PrefabUtility.InstantiatePrefab(asset) as GameObject;
            if (instance == null)
                return null;

            instance.name = name;
            instance.transform.localScale = Vector3.one * 0.55f;

            foreach (var collider in instance.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(collider);
            foreach (var camera in instance.GetComponentsInChildren<Camera>())
                Object.DestroyImmediate(camera);
            foreach (var listener in instance.GetComponentsInChildren<AudioListener>())
                Object.DestroyImmediate(listener);
            foreach (var light in instance.GetComponentsInChildren<Light>())
                Object.DestroyImmediate(light);

            foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
            {
                renderer.enabled = true;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                renderer.allowOcclusionWhenDynamic = false;

                if (overrideMaterial != null)
                    FillRendererMaterials(renderer, overrideMaterial);
            }

            MoveBoundsCenterTo(instance, targetCenter);
            return instance;
        }

        static void MoveBoundsCenterTo(GameObject instance, Vector3 targetCenter)
        {
            if (!TryGetBounds(instance, out var bounds))
                return;

            instance.transform.position += targetCenter - bounds.center;
        }

        static void FrameImportedModel(GameObject left, GameObject right)
        {
            if (!TryGetCombinedBounds(left, right, out var bounds))
                return;

            var camera = Camera.main;
            if (camera == null)
                return;

            camera.transform.position = bounds.center + new Vector3(0f, bounds.size.y * 0.42f, -Mathf.Max(58f, bounds.size.z * 2.3f));
            camera.transform.rotation = Quaternion.Euler(16f, 0f, 0f);
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(18f, bounds.size.y * 0.62f, bounds.size.x * 0.34f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 800f;
        }

        static bool TryGetCombinedBounds(GameObject a, GameObject b, out Bounds bounds)
        {
            var hasBounds = false;
            bounds = default;

            if (a != null && TryGetBounds(a, out var aBounds))
            {
                bounds = aBounds;
                hasBounds = true;
            }

            if (b != null && TryGetBounds(b, out var bBounds))
            {
                if (hasBounds)
                    bounds.Encapsulate(bBounds);
                else
                    bounds = bBounds;
                hasBounds = true;
            }

            return hasBounds;
        }

        static bool TryGetBounds(GameObject root, out Bounds bounds)
        {
            bounds = default;
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return false;

            bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return true;
        }

        static void FillRendererMaterials(Renderer renderer, Material material)
        {
            var materials = renderer.sharedMaterials;
            if (materials.Length == 0)
            {
                renderer.sharedMaterial = material;
                return;
            }

            for (var i = 0; i < materials.Length; i++)
                materials[i] = material;
            renderer.sharedMaterials = materials;
        }

        static void CreateCamera()
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.06f, 0.075f, 1f);
            cameraObject.AddComponent<AudioListener>();
        }

        static void CreateLight()
        {
            var lightObject = new GameObject("Plain Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.4f;
            light.color = Color.white;
            light.shadows = LightShadows.None;
            light.transform.rotation = Quaternion.Euler(45f, -25f, 0f);
        }

        static void CreateReferenceObjects(Material material)
        {
            CreateCube("Reference Ground", new Vector3(0f, -0.08f, 0f), new Vector3(54f, 0.12f, 30f), material);
            CreateCube("Visible Unity Cube", new Vector3(0f, 2f, -12f), new Vector3(4f, 4f, 4f), material);
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

        static Material CreateUrpMaterial(string path, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0.35f);
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material CreateUnlitMaterial(string path, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.SetColor("_BaseColor", color);
            material.SetColor("_Color", color);
            material.SetFloat("_Surface", 0f);
            material.SetFloat("_AlphaClip", 0f);
            material.SetFloat("_Cull", 0f);
            material.renderQueue = 2000;
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
