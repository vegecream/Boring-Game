using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace RhythmParkour.Editor
{
    public static class NeoCityFallingPreviewBuilder
    {
        const string RequestPath = "Assets/Editor/RhythmParkour/RebuildNeoCityFallingPreview.request";
        const string AssetTestRequestPath = "Assets/Editor/RhythmParkour/RebuildNeoCityAssetVisibilityTest.request";
        const string AllAssetTestRequestPath = "Assets/Editor/RhythmParkour/RebuildNeoCityAllAssetImportTest.request";
        const string ScenePath = "Assets/Scenes/NeoCityFallingPreview.unity";
        const string AssetTestScenePath = "Assets/Scenes/NeoCityAssetVisibilityTest.unity";
        const string AllAssetTestScenePath = "Assets/Scenes/NeoCityAllAssetImportTest.unity";
        const string KitRoot = "Assets/External/KitBash3D/NeoCity/neocity";
        const string KitMaterialFolder = KitRoot + "/Materials";
        const string StageMaterialFolder = "Assets/Materials/Stage";
        const string VfxMaterialFolder = "Assets/Materials/VFX";
        const string SkyboxPath = StageMaterialFolder + "/M_KeijiroCalmSynthwaveSkybox.mat";
        const string BridgeMaterialPath = VfxMaterialFolder + "/M_RhythmLightBridge.mat";
        const string CitySilhouetteMaterialPath = StageMaterialFolder + "/M_NeoCityFarSilhouette.mat";
        const string CyanLightMaterialPath = StageMaterialFolder + "/M_NeoCityWindowCyan.mat";
        const string MagentaLightMaterialPath = StageMaterialFolder + "/M_NeoCityWindowMagenta.mat";
        const string RedLightMaterialPath = StageMaterialFolder + "/M_NeoCityDepthRed.mat";
        const string CyanPanelMaterialPath = StageMaterialFolder + "/M_NeoCityWindowCyanUnlit.mat";
        const string MagentaPanelMaterialPath = StageMaterialFolder + "/M_NeoCityWindowMagentaUnlit.mat";
        const string RedPanelMaterialPath = StageMaterialFolder + "/M_NeoCityDepthRedUnlit.mat";
        const string DebugVisibleMaterialPath = StageMaterialFolder + "/M_NeoCityDebugVisible.mat";
        const string DebugTexturedMaterialFolder = StageMaterialFolder + "/NeoCityDebugTextured";

        static readonly string[] BuildingPaths =
        {
            KitRoot + "/KB3D_NEC_BldgLG_A.fbx",
            KitRoot + "/KB3D_NEC_BldgLG_B.fbx",
            KitRoot + "/KB3D_NEC_BldgLG_C.fbx",
            KitRoot + "/KB3D_NEC_BldgMD_A.fbx",
            KitRoot + "/KB3D_NEC_BldgMD_B.fbx",
            KitRoot + "/KB3D_NEC_BldgMD_C.fbx",
            KitRoot + "/KB3D_NEC_BldgSM_A.fbx",
            KitRoot + "/KB3D_NEC_BldgSM_B.fbx",
            KitRoot + "/KB3D_NEC_BldgSM_C.fbx"
        };

        [InitializeOnLoadMethod]
        static void RebuildIfRequested()
        {
            if (!File.Exists(RequestPath) && !File.Exists(AssetTestRequestPath) && !File.Exists(AllAssetTestRequestPath))
                return;

            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    EditorApplication.isPlaying = false;
                    EditorApplication.delayCall += RebuildIfRequested;
                    return;
                }

                if (File.Exists(RequestPath))
                {
                    BuildScene();
                    AssetDatabase.DeleteAsset(RequestPath);
                }

                if (File.Exists(AssetTestRequestPath))
                {
                    BuildAssetVisibilityTestScene();
                    AssetDatabase.DeleteAsset(AssetTestRequestPath);
                }

                if (File.Exists(AllAssetTestRequestPath))
                {
                    BuildAllAssetImportTestScene();
                    AssetDatabase.DeleteAsset(AllAssetTestRequestPath);
                }

                AssetDatabase.Refresh();
            };
        }

        [MenuItem("Rhythm Parkour/Rebuild Neo City Falling Preview")]
        public static void BuildScene()
        {
            Directory.CreateDirectory(StageMaterialFolder);
            Directory.CreateDirectory(VfxMaterialFolder);

            ConfigureKitModelImporters();
            ConvertKitMaterialsToUrp();

            var bridgeMaterial = CreateBridgeMaterial();
            var citySilhouette = CreateUrpMaterial(CitySilhouetteMaterialPath, new Color(0.06f, 0.09f, 0.16f, 1f), new Color(0.03f, 0.11f, 0.22f, 1f), 0f);
            var cyan = CreateUrpMaterial(CyanLightMaterialPath, new Color(0.05f, 0.45f, 0.85f, 1f), new Color(0.2f, 1.4f, 2.2f, 1f), 1f);
            var magenta = CreateUrpMaterial(MagentaLightMaterialPath, new Color(0.35f, 0.04f, 0.36f, 1f), new Color(1.4f, 0.15f, 1.3f, 1f), 1f);
            var red = CreateUrpMaterial(RedLightMaterialPath, new Color(0.48f, 0.03f, 0.06f, 1f), new Color(1.35f, 0.08f, 0.08f, 1f), 1f);
            var cyanPanel = CreateUnlitMaterial(CyanPanelMaterialPath, new Color(0.05f, 1.05f, 1.35f, 1f), null);
            var magentaPanel = CreateUnlitMaterial(MagentaPanelMaterialPath, new Color(1.1f, 0.15f, 1.15f, 1f), null);
            var redPanel = CreateUnlitMaterial(RedPanelMaterialPath, new Color(1.25f, 0.12f, 0.12f, 1f), null);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.38f, 0.43f, 0.52f);
            RenderSettings.fog = false;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.008f, 0.01f, 0.04f);
            RenderSettings.fogDensity = 0.0065f;

            CreateCamera();
            CreateLight();
            CreateBridge(bridgeMaterial);
            CreateCityField(citySilhouette, cyanPanel, magentaPanel, redPanel);
            CreateDepthReferenceLights(cyan, magenta, red);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[RhythmParkour] Rebuilt {ScenePath}.");
        }

        [MenuItem("Rhythm Parkour/Rebuild Neo City Asset Visibility Test")]
        public static void BuildAssetVisibilityTestScene()
        {
            Directory.CreateDirectory(StageMaterialFolder);
            ConfigureKitModelImporters();
            ConvertKitMaterialsToUrp();

            var skybox = CreateSkyboxMaterial();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = skybox;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.28f, 0.32f, 0.42f);
            RenderSettings.fog = false;

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.fieldOfView = 55f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 500f;
            camera.transform.position = new Vector3(0f, 30f, -58f);
            camera.transform.rotation = Quaternion.Euler(18f, 0f, 0f);
            cameraObject.AddComponent<AudioListener>();

            var lightObject = new GameObject("Visibility Test Key Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.5f;
            light.color = new Color(0.85f, 0.93f, 1f);
            light.transform.rotation = Quaternion.Euler(45f, -20f, 0f);

            var fillObject = new GameObject("Visibility Test Fill Light");
            var fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.intensity = 6500f;
            fill.range = 150f;
            fill.color = new Color(0.35f, 0.65f, 1f);
            fill.transform.position = new Vector3(-22f, 28f, -20f);

            var debugVisible = CreateUnlitMaterial(DebugVisibleMaterialPath, new Color(0.15f, 0.9f, 1f, 1f), null);
            InstantiateBuilding(null, new CityPlacement(0, -28f, 0f, 18f, 8f, 0f, null));
            var solidDebug = InstantiateBuilding(null, new CityPlacement(0, 0f, 0f, 18f, 8f, 0f, debugVisible));
            var texturedDebug = InstantiateBuilding(null, new CityPlacement(0, 28f, 0f, 18f, 8f, 0f, null));
            if (texturedDebug != null)
                ApplyUnlitTextureCopies(texturedDebug);

            var referenceMaterial = CreateUrpMaterial(StageMaterialFolder + "/M_NeoCityScaleReference.mat", new Color(0.04f, 0.07f, 0.13f, 1f), new Color(0.02f, 0.08f, 0.12f, 1f), 0f);
            CreateCube("Left Original Material Label Block", new Vector3(-28f, 0.7f, -18f), new Vector3(14f, 1.2f, 3f), referenceMaterial);
            CreateCube("Center Forced Cyan Unlit Label Block", new Vector3(0f, 0.7f, -18f), new Vector3(14f, 1.2f, 3f), debugVisible);
            CreateCube("Right Forced Textured Unlit Label Block", new Vector3(28f, 0.7f, -18f), new Vector3(14f, 1.2f, 3f), referenceMaterial);
            CreateCube("Scale Reference Platform", new Vector3(0f, -0.08f, 0f), new Vector3(110f, 0.1f, 72f), referenceMaterial);

            EditorSceneManager.SaveScene(scene, AssetTestScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[RhythmParkour] Rebuilt {AssetTestScenePath}.");
        }

        [MenuItem("Rhythm Parkour/Rebuild Neo City All Asset Import Test")]
        public static void BuildAllAssetImportTestScene()
        {
            Directory.CreateDirectory(StageMaterialFolder);
            ConfigureKitModelImporters();

            var skybox = CreateSkyboxMaterial();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = skybox;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.72f, 0.76f, 0.84f);
            RenderSettings.fog = false;

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.backgroundColor = new Color(0.12f, 0.13f, 0.16f, 1f);
            camera.fieldOfView = 46f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 350f;
            camera.transform.position = new Vector3(0f, 33f, -58f);
            camera.transform.rotation = Quaternion.Euler(25f, 0f, 0f);
            cameraObject.AddComponent<AudioListener>();

            var keyObject = new GameObject("Neutral Import Test Key Light");
            var key = keyObject.AddComponent<Light>();
            key.type = LightType.Directional;
            key.intensity = 2.6f;
            key.color = new Color(0.95f, 0.98f, 1f);
            key.transform.rotation = Quaternion.Euler(48f, -28f, 0f);

            var fillObject = new GameObject("Neutral Import Test Fill Light");
            var fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.intensity = 42000f;
            fill.range = 120f;
            fill.color = new Color(0.65f, 0.78f, 1f);
            fill.transform.position = new Vector3(-18f, 26f, -18f);

            var groundMaterial = CreateUrpMaterial(StageMaterialFolder + "/M_NeoCityAssetTestGround.mat", new Color(0.18f, 0.19f, 0.22f, 1f), Color.black, 0f);
            CreateCube("Neutral Ground Plane", new Vector3(0f, -0.06f, 14f), new Vector3(88f, 0.1f, 56f), groundMaterial);

            var root = new GameObject("Raw Imported KitBash Neo City Buildings");
            for (var i = 0; i < BuildingPaths.Length; i++)
            {
                var row = i / 3;
                var column = i % 3;
                var target = new Vector3((column - 1) * 26f, 0f, row * 22f);
                var instance = InstantiateRawBuildingForTest(root.transform, i, target, 17f);
                if (instance != null)
                    CreateAssetLabel(Path.GetFileNameWithoutExtension(BuildingPaths[i]), target + new Vector3(0f, 0.05f, -8.2f), root.transform);
            }

            EditorSceneManager.SaveScene(scene, AllAssetTestScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[RhythmParkour] Rebuilt {AllAssetTestScenePath}.");
        }

        static void ConvertKitMaterialsToUrp()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogWarning("[RhythmParkour] URP Lit shader not found; KitBash materials were left unchanged.");
                return;
            }

            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { KitMaterialFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                    continue;

                var baseMap = material.GetTexture("_MainTex");
                var normalMap = material.GetTexture("_BumpMap");
                var metallicMap = material.GetTexture("_MetallicGlossMap");
                var emissionMap = material.GetTexture("_EmissionMap");
                var baseColor = material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
                var emissionColor = material.HasProperty("_EmissionColor") ? material.GetColor("_EmissionColor") : Color.black;
                var smoothness = material.HasProperty("_GlossMapScale") ? material.GetFloat("_GlossMapScale") : 0.35f;

                material.shader = shader;
                material.SetColor("_BaseColor", baseColor);
                material.SetTexture("_BaseMap", baseMap);
                material.SetTexture("_BumpMap", normalMap);
                material.SetTexture("_MetallicGlossMap", metallicMap);
                material.SetFloat("_Smoothness", Mathf.Clamp01(smoothness));
                material.SetFloat("_Metallic", metallicMap != null ? 0.55f : 0f);
                material.SetFloat("_Surface", 0f);
                material.SetFloat("_Cull", 2f);

                if (normalMap != null)
                    material.EnableKeyword("_NORMALMAP");
                else
                    material.DisableKeyword("_NORMALMAP");

                if (emissionMap != null || emissionColor.maxColorComponent > 0.01f || material.name.Contains("Light") || material.name.Contains("Screen"))
                {
                    var boost = material.name.Contains("Light") || material.name.Contains("Screen") ? 2.4f : 1.15f;
                    material.SetTexture("_EmissionMap", emissionMap);
                    material.SetColor("_EmissionColor", emissionColor.maxColorComponent > 0.01f ? emissionColor * boost : new Color(0.4f, 1.1f, 1.6f, 1f));
                    material.EnableKeyword("_EMISSION");
                    material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }
                else
                {
                    material.SetColor("_EmissionColor", Color.black);
                    material.DisableKeyword("_EMISSION");
                    material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                }

                EditorUtility.SetDirty(material);
            }
        }

        static void ConfigureKitModelImporters()
        {
            foreach (var path in BuildingPaths)
            {
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null)
                    continue;

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
            material.SetColor("_Color1", new Color(0f, 0.68f, 1f, 1f));
            material.SetColor("_Color2", new Color(0.035f, 0.12f, 0.44f, 1f));
            material.SetColor("_Color3", new Color(0.38f, 0.055f, 0.38f, 1f));
            material.SetFloat("_Exponent1", 0.9f);
            material.SetFloat("_Exponent2", 0.9f);
            material.SetFloat("_Intensity", 1.55f);
            material.SetFloat("_Pulse", 0f);
            material.SetFloat("_PulseStrength", 0f);
            material.SetColor("_CloudColor", new Color(0.36f, 0.78f, 1f, 1f));
            material.SetFloat("_CloudStrength", 0.21f);
            material.SetFloat("_CloudCoverage", 0.56f);
            material.SetFloat("_CloudHeight", 0.61f);
            material.SetFloat("_CloudThickness", 0.2f);
            material.SetFloat("_CloudScale", 2.6f);
            material.SetFloat("_CloudDrift", 0.01f);
            EditorUtility.SetDirty(material);
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
            material.SetColor("_BaseColor", new Color(0.055f, 0.18f, 0.5f, 1f));
            material.SetColor("_OutlineColor", new Color(0.65f, 0.98f, 1f, 1f));
            material.SetFloat("_Alpha", 0.36f);
            material.SetFloat("_Emission", 1.45f);
            material.SetFloat("_OutlineWorldWidth", 0.075f);
            material.SetFloat("_OutlineIntensity", 5.0f);
            material.SetFloat("_PulseStrength", 0.2f);
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material CreateUrpMaterial(string path, Color baseColor, Color emissionColor, float metallic)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.SetColor("_BaseColor", baseColor);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", 0.55f);
            material.SetColor("_EmissionColor", emissionColor);
            if (emissionColor.maxColorComponent > 0.01f)
                material.EnableKeyword("_EMISSION");
            else
                material.DisableKeyword("_EMISSION");
            EditorUtility.SetDirty(material);
            return material;
        }

        static void CreateCamera()
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.045f, 0.052f, 0.074f, 1f);
            camera.fieldOfView = 68f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 650f;
            camera.transform.position = new Vector3(0f, 4.5f, -11.5f);
            camera.transform.rotation = Quaternion.Euler(14.5f, 0f, 0f);
            cameraObject.AddComponent<AudioListener>();
        }

        static void CreateLight()
        {
            var lightObject = new GameObject("Cold Overhead Key Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 2.1f;
            light.color = new Color(0.58f, 0.78f, 1f);
            light.transform.rotation = Quaternion.Euler(44f, -20f, 0f);

            var cityFillObject = new GameObject("City Visibility Fill Light");
            var cityFill = cityFillObject.AddComponent<Light>();
            cityFill.type = LightType.Point;
            cityFill.intensity = 18000f;
            cityFill.range = 220f;
            cityFill.color = new Color(0.32f, 0.55f, 1f);
            cityFill.transform.position = new Vector3(-18f, -18f, 48f);
        }

        static void CreateBridge(Material bridgeMaterial)
        {
            var start = CreateCube("High Start Platform", new Vector3(0f, 0f, 0f), new Vector3(5.2f, 0.24f, 2.0f), bridgeMaterial);
            Object.DestroyImmediate(start.GetComponent<BoxCollider>());

            var bridgeObject = new GameObject("Suspended Light Route");
            bridgeObject.transform.position = new Vector3(0f, 0.56f, 1.2f);
            bridgeObject.AddComponent<MeshFilter>();
            var renderer = bridgeObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = bridgeMaterial;
            bridgeObject.AddComponent<BoxCollider>();
            var bridge = bridgeObject.AddComponent<RhythmLightBridge>();

            var serialized = new SerializedObject(bridge);
            serialized.FindProperty("length").floatValue = 58f;
            serialized.FindProperty("width").floatValue = 3.15f;
            serialized.FindProperty("surfaceThickness").floatValue = 0.14f;
            serialized.FindProperty("lengthSegments").intValue = 72;
            serialized.FindProperty("bpm").floatValue = 120f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            bridge.Rebuild();
        }

        static void CreateCityField(Material silhouette, Material cyan, Material magenta, Material red)
        {
            var root = new GameObject("Falling City Field");
            var placements = new[]
            {
                new CityPlacement(6, -10f, -16f, 24f, 0.34f, 18f, null),
                new CityPlacement(7, 12f, -20f, 32f, 0.32f, -18f, null),
                new CityPlacement(3, -24f, -34f, 48f, 0.28f, 34f, null),
                new CityPlacement(4, 26f, -40f, 58f, 0.30f, -28f, null),
                new CityPlacement(8, -8f, -52f, 74f, 0.42f, 0f, null),
                new CityPlacement(5, 20f, -64f, 92f, 0.34f, 22f, null),
                new CityPlacement(1, -32f, -76f, 112f, 0.26f, -16f, silhouette),
                new CityPlacement(2, 34f, -88f, 128f, 0.28f, 28f, silhouette),
                new CityPlacement(0, -18f, -102f, 146f, 0.30f, 8f, silhouette),
                new CityPlacement(3, 12f, -118f, 164f, 0.36f, -24f, silhouette)
            };

            foreach (var placement in placements)
                InstantiateBuilding(root.transform, placement);

            for (var i = 0; i < 18; i++)
            {
                var side = i % 2 == 0 ? -1f : 1f;
                var x = side * (9f + i * 0.78f);
                var z = 22f + i * 7.4f;
                var y = -10f - i * 3.45f;
                var height = 4.6f + (i % 3) * 1.15f;
                var material = i % 3 == 0 ? magenta : i % 5 == 0 ? red : cyan;
                var panel = CreateCube("Distant Window Light " + i, new Vector3(x, y, z), new Vector3(0.95f, height, 0.32f), material, root.transform);
                panel.transform.rotation = Quaternion.Euler(0f, side < 0f ? -18f : 18f, 0f);
            }
        }

        static GameObject InstantiateRawBuildingForTest(Transform root, int assetIndex, Vector3 targetCenter, float targetHeight)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(BuildingPaths[assetIndex]);
            if (asset == null)
            {
                Debug.LogWarning($"[RhythmParkour] Missing KitBash building asset at {BuildingPaths[assetIndex]}.");
                return null;
            }

            var instance = PrefabUtility.InstantiatePrefab(asset) as GameObject;
            if (instance == null)
                return null;

            instance.name = Path.GetFileNameWithoutExtension(BuildingPaths[assetIndex]) + " Raw Import Test";
            instance.transform.SetParent(root, false);
            instance.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            foreach (var collider in instance.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(collider);
            foreach (var importedCamera in instance.GetComponentsInChildren<Camera>())
                Object.DestroyImmediate(importedCamera);
            foreach (var importedListener in instance.GetComponentsInChildren<AudioListener>())
                Object.DestroyImmediate(importedListener);
            foreach (var importedLight in instance.GetComponentsInChildren<Light>())
                Object.DestroyImmediate(importedLight);

            foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
                ConfigureImportedRenderer(renderer);

            NormalizeHeightAndPlace(instance, targetCenter, targetHeight);
            return instance;
        }

        static void NormalizeHeightAndPlace(GameObject instance, Vector3 targetCenter, float targetHeight)
        {
            if (!TryGetBounds(instance, out var initialBounds) || initialBounds.size.y <= 0.01f)
                return;

            instance.transform.localScale *= targetHeight / initialBounds.size.y;
            if (!TryGetBounds(instance, out var scaledBounds))
                return;

            var target = targetCenter + Vector3.up * scaledBounds.extents.y;
            instance.transform.position += target - scaledBounds.center;
        }

        static void CreateAssetLabel(string text, Vector3 position, Transform root)
        {
            var labelObject = new GameObject(text + " Label");
            labelObject.transform.SetParent(root, false);
            labelObject.transform.position = position;
            labelObject.transform.rotation = Quaternion.Euler(78f, 0f, 0f);

            var label = labelObject.AddComponent<TextMesh>();
            label.text = text;
            label.fontSize = 42;
            label.characterSize = 0.12f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = new Color(0.82f, 0.9f, 1f, 1f);
        }

        static GameObject InstantiateBuilding(Transform root, CityPlacement placement)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(BuildingPaths[placement.AssetIndex]);
            if (asset == null)
            {
                Debug.LogWarning($"[RhythmParkour] Missing KitBash building asset at {BuildingPaths[placement.AssetIndex]}.");
                return null;
            }

            var instance = PrefabUtility.InstantiatePrefab(asset) as GameObject;
            if (instance == null)
                return null;

            instance.name = Path.GetFileNameWithoutExtension(BuildingPaths[placement.AssetIndex]) + " Depth Instance";
            if (root != null)
                instance.transform.SetParent(root, false);
            instance.transform.position = new Vector3(placement.X, placement.Y, placement.Z);
            instance.transform.rotation = Quaternion.Euler(0f, placement.Yaw, 0f);
            instance.transform.localScale = Vector3.one * placement.Scale;

            foreach (var collider in instance.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(collider);
            foreach (var importedCamera in instance.GetComponentsInChildren<Camera>())
                Object.DestroyImmediate(importedCamera);
            foreach (var importedListener in instance.GetComponentsInChildren<AudioListener>())
                Object.DestroyImmediate(importedListener);
            foreach (var importedLight in instance.GetComponentsInChildren<Light>())
                Object.DestroyImmediate(importedLight);

            foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
            {
                ConfigureImportedRenderer(renderer);
                if (placement.OverrideMaterial != null)
                    FillRendererMaterials(renderer, placement.OverrideMaterial);
            }

            MoveBoundsCenterTo(instance, new Vector3(placement.X, placement.Y, placement.Z));
            return instance;
        }

        static void MoveBoundsCenterTo(GameObject instance, Vector3 targetCenter)
        {
            if (!TryGetBounds(instance, out var bounds))
                return;

            instance.transform.position += targetCenter - bounds.center;
        }

        static bool TryGetBounds(GameObject instance, out Bounds bounds)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>();
            bounds = default;
            if (renderers.Length == 0)
                return false;

            bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return true;
        }

        static void ConfigureImportedRenderer(Renderer renderer)
        {
            renderer.enabled = true;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.allowOcclusionWhenDynamic = false;
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

        static void ApplyUnlitTextureCopies(GameObject instance)
        {
            Directory.CreateDirectory(DebugTexturedMaterialFolder);
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
            {
                var sourceMaterials = renderer.sharedMaterials;
                var debugMaterials = new Material[sourceMaterials.Length];
                for (var i = 0; i < sourceMaterials.Length; i++)
                {
                    var source = sourceMaterials[i];
                    if (source == null)
                        continue;

                    var texture = source.HasProperty("_BaseMap") ? source.GetTexture("_BaseMap") : source.HasProperty("_MainTex") ? source.GetTexture("_MainTex") : null;
                    var color = source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") : source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white;
                    var safeName = source.name.Replace("/", "_").Replace("\\", "_");
                    var materialPath = $"{DebugTexturedMaterialFolder}/{safeName}_Unlit.mat";
                    debugMaterials[i] = CreateUnlitMaterial(materialPath, color, texture);
                }

                renderer.sharedMaterials = debugMaterials;
            }
        }

        static Material CreateUnlitMaterial(string path, Color baseColor, Texture texture)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Texture");

            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", baseColor);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", baseColor);
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", texture);
            if (material.HasProperty("_Cull"))
                material.SetFloat("_Cull", 0f);
            material.renderQueue = 2000;
            EditorUtility.SetDirty(material);
            return material;
        }

        static void CreateDepthReferenceLights(Material cyan, Material magenta, Material red)
        {
            var root = new GameObject("Vertical Height Light References");
            for (var i = 0; i < 10; i++)
            {
                var side = i % 2 == 0 ? -1f : 1f;
                var z = 8f + i * 9.5f;
                var x = side * (5.4f + i * 0.42f);
                var material = i > 7 ? red : i % 3 == 0 ? magenta : cyan;
                CreateCube("Suspended Drop Marker " + i, new Vector3(x, -22f - i * 7.5f, z), new Vector3(0.08f, 14f, 0.08f), material, root.transform);
            }
        }

        static GameObject CreateCube(string name, Vector3 position, Vector3 scale, Material material, Transform parent = null)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.position = position;
            cube.transform.localScale = scale;
            if (parent != null)
                cube.transform.SetParent(parent, true);
            cube.GetComponent<Renderer>().sharedMaterial = material;
            return cube;
        }

        readonly struct CityPlacement
        {
            public readonly int AssetIndex;
            public readonly float X;
            public readonly float Y;
            public readonly float Z;
            public readonly float Scale;
            public readonly float Yaw;
            public readonly Material OverrideMaterial;

            public CityPlacement(int assetIndex, float x, float y, float z, float scale, float yaw, Material overrideMaterial)
            {
                AssetIndex = assetIndex;
                X = x;
                Y = y;
                Z = z;
                Scale = scale;
                Yaw = yaw;
                OverrideMaterial = overrideMaterial;
            }
        }
    }
}
