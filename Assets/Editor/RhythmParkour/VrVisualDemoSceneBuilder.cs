using System.IO;
using BoringRun.VRInput;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace RhythmParkour.Editor
{
    public static class VrVisualDemoSceneBuilder
    {
        const string SceneFolder = "Assets/Scenes/VisualDemos";
        const string MaterialFolder = "Assets/Materials/VisualDemos";
        const string VolumeFolder = "Assets/Settings/Volumes/VisualDemos";
        const string GrappleScenePath = SceneFolder + "/GrapplePendulumMotionDemo.unity";
        const string CityRushScenePath = SceneFolder + "/ForwardCityRushVisualDemo.unity";
        const string CityRushVisibilityDebugScenePath = SceneFolder + "/ForwardCityRushVisibilityDebug.unity";
        const string SkyboxMaterialPath = MaterialFolder + "/M_VisualDemo_Skybox.mat";
        const string ImportTestSkyboxPath = "Assets/Materials/Stage/M_KeijiroCalmSynthwaveSkybox.mat";
        const string RushUnlitMaterialFolder = MaterialFolder + "/NeoCityRushUnlit";
        const string RebuildAllRequestPath = "Assets/Editor/RhythmParkour/RebuildVRVisualDemos.request";
        const string RebuildCityRushRequestPath = "Assets/Editor/RhythmParkour/RebuildForwardCityRushVisualDemo.request";
        const string RebuildCityRushVisibilityDebugRequestPath = "Assets/Editor/RhythmParkour/RebuildForwardCityRushVisibilityDebug.request";
        const string KitRoot = "Assets/External/KitBash3D/NeoCity/neocity";

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
        static void StartRequestWatcher()
        {
            EditorApplication.update -= RebuildIfRequested;
            EditorApplication.update += RebuildIfRequested;
        }

        static void RebuildIfRequested()
        {
            var rebuildAll = File.Exists(RebuildAllRequestPath);
            var rebuildCity = File.Exists(RebuildCityRushRequestPath);
            var rebuildCityDebug = File.Exists(RebuildCityRushVisibilityDebugRequestPath);
            if (!rebuildAll && !rebuildCity && !rebuildCityDebug)
                return;

            EditorApplication.update -= RebuildIfRequested;
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    EditorApplication.isPlaying = false;
                    EditorApplication.update += RebuildIfRequested;
                    return;
                }

                if (rebuildAll)
                {
                    BuildAllVisualDemos();
                    AssetDatabase.DeleteAsset(RebuildAllRequestPath);
                }
                else
                {
                    if (rebuildCity)
                    {
                        BuildForwardCityRushDemo();
                        AssetDatabase.DeleteAsset(RebuildCityRushRequestPath);
                    }

                    if (rebuildCityDebug)
                    {
                        BuildForwardCityRushVisibilityDebugDemo();
                        AssetDatabase.DeleteAsset(RebuildCityRushVisibilityDebugRequestPath);
                    }
                }

                AssetDatabase.Refresh();
            };
        }

        [MenuItem("Rhythm Parkour/Rebuild VR Visual Demo Scenes")]
        public static void BuildAllVisualDemos()
        {
            EnsureFolders();
            BuildGrapplePendulumDemo();
            BuildForwardCityRushDemo();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[RhythmParkour] Rebuilt VR visual demo scenes.");
        }

        [MenuItem("Rhythm Parkour/Rebuild Forward City Rush Visibility Debug")]
        public static void BuildForwardCityRushVisibilityDebugDemo()
        {
            EnsureFolders();
            ConfigureKitModelImporters();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ConfigureImportTestEnvironment();

            var materials = VisualMaterials.Create(MaterialFolder);
            var rig = CreateVrRig("Forward Rush Visibility Debug Rig");
            DisablePostProcessing(rig);
            CreateVisualInputSystem(rig);
            CreateCityRushStage(materials, rig, null, enableSpeedFeel: false, label: "Forward City Rush Visibility Debug\nVR rig + import-test lighting");

            EditorSceneManager.SaveScene(scene, CityRushVisibilityDebugScenePath);
            Debug.Log($"[RhythmParkour] Rebuilt {CityRushVisibilityDebugScenePath}.");
        }

        [MenuItem("Rhythm Parkour/Validate VR Visual Demo Scenes")]
        public static void ValidateVisualDemos()
        {
            ValidateScene(
                GrappleScenePath,
                requirePendulum: true,
                requireCityRush: false,
                minRushBuildings: 0,
                minColossalRotators: 0);

            ValidateScene(
                CityRushScenePath,
                requirePendulum: false,
                requireCityRush: true,
                minRushBuildings: 38,
                minColossalRotators: 3);

            Debug.Log("[RhythmParkour] VR visual demo scene validation passed.");
        }

        [MenuItem("Rhythm Parkour/Rebuild Grapple Pendulum Motion Demo")]
        public static void BuildGrapplePendulumDemo()
        {
            EnsureFolders();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ConfigureEnvironment();

            var materials = VisualMaterials.Create(MaterialFolder);
            var rig = CreateVrRig("Pendulum Camera Rig");
            var speedVolume = CreateSpeedVolume("GrapplePendulumMotionDemo");
            CreateVolumetricCloudVolume("GrapplePendulumMotionDemo");
            var grappleEvent = new RhythmActionEvent(2f, 8f, RhythmActionType.Grapple, RhythmHand.Right, RhythmDirection.Up);
            CreatePendulumStage(materials, rig, grappleEvent, secondsPerBeat: 0.5f);
            AttachSpeedFeel(rig, speedVolume, true);
            CreateVisualInputSystem(rig);

            EditorSceneManager.SaveScene(scene, GrappleScenePath);
            Debug.Log($"[RhythmParkour] Rebuilt {GrappleScenePath}.");
        }

        [MenuItem("Rhythm Parkour/Rebuild Forward City Rush Visual Demo")]
        public static void BuildForwardCityRushDemo()
        {
            EnsureFolders();
            ConfigureKitModelImporters();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ConfigureEnvironment();

            var materials = VisualMaterials.Create(MaterialFolder);
            var rig = CreateVrRig("Forward Rush Camera Rig");
            var speedVolume = CreateSpeedVolume("ForwardCityRushVisualDemo");
            CreateVolumetricCloudVolume("ForwardCityRushVisualDemo");
            CreateVisualInputSystem(rig);
            CreateCityRushStage(materials, rig, speedVolume);

            EditorSceneManager.SaveScene(scene, CityRushScenePath);
            Debug.Log($"[RhythmParkour] Rebuilt {CityRushScenePath}.");
        }

        static void EnsureFolders()
        {
            Directory.CreateDirectory(SceneFolder);
            Directory.CreateDirectory(MaterialFolder);
            Directory.CreateDirectory(VolumeFolder);
        }

        static void ValidateScene(
            string scenePath,
            bool requirePendulum,
            bool requireCityRush,
            int minRushBuildings,
            int minColossalRotators)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            var missingScripts = 0;
            var hasVrRig = false;
            var hasInputSystem = false;
            var hasPendulum = false;
            var hasCityRush = false;
            var rushBuildings = 0;
            var colossalRotators = 0;

            foreach (var root in roots)
            {
                missingScripts += CountMissingScripts(root);
                hasVrRig |= root.GetComponentInChildren<OVRCameraRig>(true) != null;
                hasInputSystem |= root.GetComponentInChildren<VRInputReader>(true) != null;
                hasPendulum |= root.GetComponentInChildren<GrapplePendulumDemoDriver>(true) != null;
                hasCityRush |= root.GetComponentInChildren<ForwardCityRushDemoDriver>(true) != null;
                rushBuildings += CountNamedChildren(root, "Rush Building");
                colossalRotators += root.GetComponentsInChildren<SlowColossalRotationDriver>(true).Length;
            }

            if (missingScripts > 0)
                throw new System.InvalidOperationException($"{scenePath} has {missingScripts} missing script references.");
            if (!hasVrRig)
                throw new System.InvalidOperationException($"{scenePath} is missing OVRCameraRig.");
            if (!hasInputSystem)
                throw new System.InvalidOperationException($"{scenePath} is missing VRInputReader.");
            if (requirePendulum && !hasPendulum)
                throw new System.InvalidOperationException($"{scenePath} is missing GrapplePendulumDemoDriver.");
            if (requireCityRush && !hasCityRush)
                throw new System.InvalidOperationException($"{scenePath} is missing ForwardCityRushDemoDriver.");
            if (rushBuildings < minRushBuildings)
                throw new System.InvalidOperationException($"{scenePath} has only {rushBuildings} rush buildings; expected at least {minRushBuildings}.");
            if (colossalRotators < minColossalRotators)
                throw new System.InvalidOperationException($"{scenePath} has only {colossalRotators} colossal rotators; expected at least {minColossalRotators}.");

            Debug.Log($"[RhythmParkour] Validated {scenePath}: missingScripts={missingScripts}, rushBuildings={rushBuildings}, colossalRotators={colossalRotators}.");
        }

        static int CountMissingScripts(GameObject root)
        {
            var count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root);
            foreach (Transform child in root.transform)
                count += CountMissingScripts(child.gameObject);
            return count;
        }

        static int CountNamedChildren(GameObject root, string token)
        {
            var count = root.name.Contains(token) ? 1 : 0;
            foreach (Transform child in root.transform)
                count += CountNamedChildren(child.gameObject, token);
            return count;
        }

        static void ConfigureEnvironment()
        {
            RenderSettings.skybox = CreateSkyboxMaterial();
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.22f, 0.27f, 0.38f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.035f, 0.055f, 0.12f);
            RenderSettings.fogDensity = 0.0045f;

            var lightObject = new GameObject("Cold Directional Key Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.45f;
            light.color = new Color(0.72f, 0.88f, 1f);
            light.transform.rotation = Quaternion.Euler(46f, -28f, 0f);
        }

        static void ConfigureImportTestEnvironment()
        {
            var skybox = AssetDatabase.LoadAssetAtPath<Material>(ImportTestSkyboxPath);
            if (skybox == null)
                skybox = CreateSkyboxMaterial();

            RenderSettings.skybox = skybox;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.72f, 0.76f, 0.84f);
            RenderSettings.fog = false;

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
        }

        static Material CreateSkyboxMaterial()
        {
            var shader = Shader.Find("Skybox/Procedural");
            if (shader == null)
                shader = Shader.Find("RhythmParkour/OpenSource/Keijiro Horizontal Skybox URP");

            var material = AssetDatabase.LoadAssetAtPath<Material>(SkyboxMaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, SkyboxMaterialPath);
            }

            material.shader = shader;
            if (material.HasProperty("_SunSize"))
                material.SetFloat("_SunSize", 0.01f);
            if (material.HasProperty("_SunSizeConvergence"))
                material.SetFloat("_SunSizeConvergence", 5f);
            if (material.HasProperty("_AtmosphereThickness"))
                material.SetFloat("_AtmosphereThickness", 0.72f);
            if (material.HasProperty("_SkyTint"))
                material.SetColor("_SkyTint", new Color(0.0f, 0.42f, 0.84f, 1f));
            if (material.HasProperty("_GroundColor"))
                material.SetColor("_GroundColor", new Color(0.32f, 0.045f, 0.32f, 1f));
            if (material.HasProperty("_Exposure"))
                material.SetFloat("_Exposure", 1.35f);

            if (material.HasProperty("_Color1"))
            {
                material.SetColor("_Color1", new Color(0.0f, 0.66f, 1.0f, 1f));
                material.SetColor("_Color2", new Color(0.035f, 0.12f, 0.42f, 1f));
                material.SetColor("_Color3", new Color(0.42f, 0.055f, 0.44f, 1f));
                material.SetFloat("_Exponent1", 0.88f);
                material.SetFloat("_Exponent2", 0.95f);
                material.SetFloat("_Intensity", 1.55f);
                material.SetFloat("_PulseStrength", 0f);
                material.SetColor("_CloudColor", new Color(0.42f, 0.78f, 1f, 1f));
                material.SetFloat("_CloudStrength", 0.38f);
                material.SetFloat("_CloudCoverage", 0.63f);
                material.SetFloat("_CloudHeight", 0.58f);
                material.SetFloat("_CloudThickness", 0.3f);
                material.SetFloat("_CloudScale", 2.35f);
                material.SetFloat("_CloudDrift", 0.018f);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        static PlayerRig CreateVrRig(string name)
        {
            var root = new GameObject("[BuildingBlock] Camera Rig");
            root.transform.position = new Vector3(0f, 1.35f, -1.5f);

            var trackingSpace = new GameObject("TrackingSpace");
            trackingSpace.transform.SetParent(root.transform, false);

            CreateEyeAnchor("LeftEyeAnchor", trackingSpace.transform, false);
            var centerEye = CreateEyeAnchor("CenterEyeAnchor", trackingSpace.transform, true);
            CreateEyeAnchor("RightEyeAnchor", trackingSpace.transform, false);
            CreateAnchor("TrackerAnchor", trackingSpace.transform);
            var leftController = CreateAnchor("LeftControllerAnchor", trackingSpace.transform);
            var rightController = CreateAnchor("RightControllerAnchor", trackingSpace.transform);
            CreateAnchor("LeftHandAnchor", trackingSpace.transform);
            CreateAnchor("RightHandAnchor", trackingSpace.transform);

            root.AddComponent<OVRCameraRig>();
            var manager = root.AddComponent<OVRManager>();
            manager.trackingOriginType = OVRManager.TrackingOrigin.FloorLevel;

            var camera = centerEye.GetComponent<Camera>();
            camera.fieldOfView = 76f;
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = 900f;
            camera.clearFlags = CameraClearFlags.Skybox;

            root.name = "[BuildingBlock] Camera Rig - " + name;
            return new PlayerRig(root, trackingSpace, centerEye, leftController, rightController);
        }

        static void DisablePostProcessing(PlayerRig rig)
        {
            var cameraData = rig.Camera.GetComponent<UniversalAdditionalCameraData>();
            if (cameraData != null)
                cameraData.renderPostProcessing = false;
        }

        static GameObject CreateEyeAnchor(string name, Transform parent, bool mainCamera)
        {
            var anchor = CreateAnchor(name, parent);
            if (mainCamera)
                anchor.tag = "MainCamera";

            var camera = anchor.AddComponent<Camera>();
            camera.enabled = mainCamera;
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.nearClipPlane = 0.03f;
            if (mainCamera)
            {
                anchor.AddComponent<AudioListener>();
                var cameraData = anchor.AddComponent<UniversalAdditionalCameraData>();
                cameraData.renderPostProcessing = true;
            }

            return anchor;
        }

        static GameObject CreateAnchor(string name, Transform parent)
        {
            var anchor = new GameObject(name);
            anchor.transform.SetParent(parent, false);
            return anchor;
        }

        static Volume CreateSpeedVolume(string id)
        {
            var profile = SaveSpeedVolumeProfile(id);
            var volumeObject = new GameObject("Global Speed Feel Volume");
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.weight = 1f;
            volume.sharedProfile = profile;
            return volume;
        }

        static void CreateVolumetricCloudVolume(string id)
        {
            var profilePath = $"{VolumeFolder}/{id}_VolumetricCloudsProfile.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.name = id + " Volumetric Clouds";
                AssetDatabase.CreateAsset(profile, profilePath);
            }

            profile.components.RemoveAll(component => component == null);
            if (!profile.TryGet(out VolumetricClouds clouds))
            {
                clouds = profile.Add<VolumetricClouds>(true);
                clouds.name = "VolumetricClouds";
                AssetDatabase.AddObjectToAsset(clouds, profile);
            }

            clouds.active = true;
            clouds.state.overrideState = true;
            clouds.state.value = true;
            clouds.localClouds.overrideState = true;
            clouds.localClouds.value = true;
            clouds.cloudPreset = VolumetricClouds.CloudPresets.Cloudy;
            clouds.densityMultiplier.overrideState = true;
            clouds.densityMultiplier.value = 0.46f;
            clouds.shapeFactor.overrideState = true;
            clouds.shapeFactor.value = 0.42f;
            clouds.shapeScale.overrideState = true;
            clouds.shapeScale.value = 7f;
            clouds.erosionFactor.overrideState = true;
            clouds.erosionFactor.value = 0.48f;
            clouds.erosionScale.overrideState = true;
            clouds.erosionScale.value = 80f;
            clouds.bottomAltitude.overrideState = true;
            clouds.bottomAltitude.value = 18f;
            clouds.altitudeRange.overrideState = true;
            clouds.altitudeRange.value = 100f;
            clouds.earthCurvature.overrideState = true;
            clouds.earthCurvature.value = 0f;
            clouds.globalSpeed.overrideState = true;
            clouds.globalSpeed.value = 4f;
            clouds.globalOrientation.overrideState = true;
            clouds.globalOrientation.value = 35f;
            EditorUtility.SetDirty(clouds);

            var volumeObject = new GameObject("Global Volumetric Clouds");
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 20f;
            volume.weight = 1f;
            volume.sharedProfile = profile;
            EditorUtility.SetDirty(profile);
        }

        static void AttachSpeedFeel(PlayerRig rig, Volume speedVolume, bool strong)
        {
            var speedFeel = rig.Root.AddComponent<VRSpeedFeelDriver>();
            var serialized = new SerializedObject(speedFeel);
            serialized.FindProperty("speedVolume").objectReferenceValue = speedVolume;
            serialized.FindProperty("targetCamera").objectReferenceValue = rig.Camera.GetComponent<Camera>();
            serialized.FindProperty("baseLensDistortion").floatValue = strong ? -0.075f : -0.025f;
            serialized.FindProperty("actionLensDistortion").floatValue = strong ? -0.18f : -0.095f;
            serialized.FindProperty("baseChromaticAberration").floatValue = strong ? 0.095f : 0.035f;
            serialized.FindProperty("actionChromaticAberration").floatValue = strong ? 0.28f : 0.18f;
            serialized.FindProperty("baseVignette").floatValue = strong ? 0.22f : 0.18f;
            serialized.FindProperty("actionVignette").floatValue = strong ? 0.38f : 0.32f;
            serialized.FindProperty("desktopFovBoost").floatValue = strong ? 12f : 7f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static VolumeProfile SaveSpeedVolumeProfile(string id)
        {
            var path = $"{VolumeFolder}/{id}_SpeedFeelProfile.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }

            var lens = GetOrAddVolumeComponent<LensDistortion>(profile);
            lens.intensity.overrideState = true;
            lens.scale.overrideState = true;
            lens.intensity.value = -0.055f;
            lens.scale.value = 1.025f;

            var chromatic = GetOrAddVolumeComponent<ChromaticAberration>(profile);
            chromatic.intensity.overrideState = true;
            chromatic.intensity.value = 0.11f;

            var vignette = GetOrAddVolumeComponent<Vignette>(profile);
            vignette.intensity.overrideState = true;
            vignette.smoothness.overrideState = true;
            vignette.color.overrideState = true;
            vignette.intensity.value = 0.22f;
            vignette.smoothness.value = 0.6f;
            vignette.color.value = new Color(0.015f, 0.01f, 0.05f);

            EditorUtility.SetDirty(profile);
            return profile;
        }

        static T GetOrAddVolumeComponent<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (!profile.TryGet<T>(out var component))
                component = profile.Add<T>(true);
            component.active = true;
            EditorUtility.SetDirty(component);
            return component;
        }

        static void CreateVisualInputSystem(PlayerRig rig)
        {
            var inputObject = new GameObject("VR Input System");
            inputObject.transform.position = rig.Root.transform.position;

            var reader = inputObject.AddComponent<VRInputReader>();
            var serializedReader = new SerializedObject(reader);
            serializedReader.FindProperty("directionReference").objectReferenceValue = rig.Camera.transform;
            serializedReader.FindProperty("playerTurnReference").objectReferenceValue = rig.Camera.transform;
            serializedReader.ApplyModifiedPropertiesWithoutUndo();

            var poseBinder = inputObject.AddComponent<VRControllerPoseBinder>();
            poseBinder.enabled = false;
            var serializedPoseBinder = new SerializedObject(poseBinder);
            serializedPoseBinder.FindProperty("trackingSpace").objectReferenceValue = rig.TrackingSpace.transform;
            serializedPoseBinder.FindProperty("leftController").objectReferenceValue = rig.LeftController.transform;
            serializedPoseBinder.FindProperty("rightController").objectReferenceValue = rig.RightController.transform;
            serializedPoseBinder.ApplyModifiedPropertiesWithoutUndo();

            var debugVisuals = inputObject.AddComponent<VRControllerDebugVisuals>();
            var serializedVisuals = new SerializedObject(debugVisuals);
            serializedVisuals.FindProperty("trackingSpace").objectReferenceValue = rig.TrackingSpace.transform;
            serializedVisuals.ApplyModifiedPropertiesWithoutUndo();
        }

        static void CreatePendulumStage(VisualMaterials materials, PlayerRig rig, RhythmActionEvent grappleEvent, float secondsPerBeat)
        {
            var hand = grappleEvent != null ? grappleEvent.Hand : RhythmHand.Right;
            var durationBeats = grappleEvent != null ? Mathf.Max(0.25f, grappleEvent.DurationBeats) : 8f;
            var durationSeconds = Mathf.Max(0.1f, durationBeats * Mathf.Max(0.01f, secondsPerBeat));
            ConfigureGrappleHandAnchors(rig, hand);

            CreateCube("Entry Light Bridge", new Vector3(0f, 0f, -1.2f), new Vector3(3.2f, 0.12f, 12.4f), materials.Bridge);
            CreateCube("Exit Light Bridge", new Vector3(0f, 0.05f, 29.2f), new Vector3(3.2f, 0.12f, 18.5f), materials.Bridge);
            CreateCube("Gap Darkness", new Vector3(0f, -0.25f, 14.6f), new Vector3(4.2f, 0.08f, 18.5f), materials.Danger);

            var hook = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hook.name = "Fixed Grapple Claw Position";
            hook.transform.position = new Vector3(0f, 7.2f, 10.5f);
            hook.transform.localScale = new Vector3(0.55f, 0.24f, 0.55f);
            hook.GetComponent<Renderer>().sharedMaterial = materials.Hook;
            Object.DestroyImmediate(hook.GetComponent<Collider>());

            var clawLeft = CreateCube("Claw Left Jaw", hook.transform.position + new Vector3(-0.42f, -0.08f, 0.1f), new Vector3(0.12f, 0.38f, 0.44f), materials.Hook);
            var clawRight = CreateCube("Claw Right Jaw", hook.transform.position + new Vector3(0.42f, -0.08f, 0.1f), new Vector3(0.12f, 0.38f, 0.44f), materials.Hook);
            clawLeft.transform.rotation = Quaternion.Euler(0f, 0f, -18f);
            clawRight.transform.rotation = Quaternion.Euler(0f, 0f, 18f);

            CreateGrappleJudgementMarkers(materials, durationBeats);

            var driver = rig.Root.AddComponent<GrapplePendulumDemoDriver>();
            var serialized = new SerializedObject(driver);
            serialized.FindProperty("rigRoot").objectReferenceValue = rig.Root.transform;
            serialized.FindProperty("motionRoot").objectReferenceValue = rig.TrackingSpace.transform;
            serialized.FindProperty("hookPoint").objectReferenceValue = hook.transform;
            serialized.FindProperty("leftRopeEndPoint").objectReferenceValue = rig.LeftController.transform;
            serialized.FindProperty("rightRopeEndPoint").objectReferenceValue = rig.RightController.transform;
            serialized.FindProperty("ropeMaterial").objectReferenceValue = materials.Rope;
            serialized.FindProperty("clawVisual").objectReferenceValue = hook.transform;
            serialized.FindProperty("grappleHand").enumValueIndex = (int)hand;
            serialized.FindProperty("configureMotionFromGrapple").boolValue = true;
            serialized.FindProperty("grappleDurationSeconds").floatValue = durationSeconds;
            serialized.FindProperty("approachSeconds").floatValue = 1.65f;
            serialized.FindProperty("swingSeconds").floatValue = durationSeconds;
            serialized.FindProperty("exitSeconds").floatValue = 2.6f;
            serialized.FindProperty("approachDistance").floatValue = 12.5f;
            serialized.FindProperty("baseSwingRadius").floatValue = 8.2f;
            serialized.FindProperty("swingRadiusPerSecond").floatValue = 1.35f;
            serialized.FindProperty("swingRadius").floatValue = 8.2f + durationSeconds * 1.35f;
            serialized.FindProperty("swingAngleDegrees").floatValue = 64f;
            serialized.FindProperty("swingMidSpeedBoost").floatValue = 0f;
            serialized.FindProperty("baseExitDistance").floatValue = 10f;
            serialized.FindProperty("exitDistancePerSecond").floatValue = 3f;
            serialized.FindProperty("exitDistance").floatValue = 10f + durationSeconds * 3f;
            serialized.FindProperty("cameraRollDegrees").floatValue = 10f;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            CreateLabel("Grapple Pendulum Motion Demo\nfixed claw, body swings like a pendulum", new Vector3(-2.9f, 2.2f, 3.5f));
        }

        static void ConfigureGrappleHandAnchors(PlayerRig rig, RhythmHand hand)
        {
            var activeIsLeft = hand == RhythmHand.Left;
            rig.LeftController.transform.localPosition = activeIsLeft
                ? new Vector3(-0.46f, 0.18f, 0.72f)
                : new Vector3(-0.42f, -0.32f, 0.28f);
            rig.RightController.transform.localPosition = activeIsLeft
                ? new Vector3(0.42f, -0.32f, 0.28f)
                : new Vector3(0.46f, 0.18f, 0.72f);
        }

        static void CreateGrappleJudgementMarkers(VisualMaterials materials, float durationBeats)
        {
            const float startZ = 4.2f;
            const float unitsPerBeat = 1.45f;
            var markerCount = Mathf.Max(2, Mathf.CeilToInt(durationBeats));
            var segmentLength = durationBeats * unitsPerBeat;
            var spacing = segmentLength / Mathf.Max(1, markerCount - 1);

            CreateCube(
                "Grapple Swing Hold Segment",
                new Vector3(0f, 0.06f, startZ + segmentLength * 0.5f),
                new Vector3(0.12f, 0.04f, segmentLength),
                materials.Rope);

            for (var i = 0; i < markerCount; i++)
            {
                var z = startZ + i * spacing;
                CreateCube(
                    "Grapple Swing Judgement Marker " + i,
                    new Vector3(0f, 0.1f, z),
                    new Vector3(2.35f - Mathf.Min(i, 10) * 0.08f, 0.04f, 0.08f),
                    i % 2 == 0 ? materials.Beat : materials.Downbeat);
            }
        }

        static void CreateCityRushStage(VisualMaterials materials, PlayerRig rig, Volume speedVolume, bool enableSpeedFeel = true, string label = "Forward City Rush Visual Demo\n90 seconds, no chart judging")
        {
            const float runDistance = 690f;
            const float runwayCenter = 345f;
            const float runwayLength = 720f;
            CreateCube("Transparent Forward Runway", new Vector3(0f, -0.08f, runwayCenter), new Vector3(3.2f, 0.08f, runwayLength), materials.Bridge);
            CreateCube("Left Runway Edge", new Vector3(-1.75f, 0.02f, runwayCenter), new Vector3(0.08f, 0.12f, runwayLength), materials.Edge);
            CreateCube("Right Runway Edge", new Vector3(1.75f, 0.02f, runwayCenter), new Vector3(0.08f, 0.12f, runwayLength), materials.Edge);

            for (var i = 0; i < 88; i++)
            {
                var z = 4f + i * 7.8f;
                if (z > runDistance)
                    break;
                var width = i % 4 == 0 ? 4.2f : 2.6f;
                CreateCube("Soft Beat Depth Marker " + i, new Vector3(0f, 0.03f, z), new Vector3(width, 0.04f, 0.08f), i % 4 == 0 ? materials.Downbeat : materials.Beat);
            }

            var rushRoot = new GameObject("Opposing Direction City Rush Field");
            var cityMaterial = materials.CitySilhouette;
            var windowMaterials = new[] { materials.WindowCyan, materials.WindowMagenta, materials.WindowRed };
            for (var i = 0; i < 22; i++)
            {
                var side = i % 2 == 0 ? -1f : 1f;
                var lane = i % 4;
                var x = side * (20f + lane * 9f);
                var y = -23f - (i % 5) * 7f;
                var z = 18f + i * 11.6f;
                var scale = 0.12f + (i % 3) * 0.038f;
                var yaw = side < 0f ? 18f + i * 3f : -18f - i * 3f;
                var building = InstantiateBuilding(i, new Vector3(x, y, z), scale, yaw, cityMaterial, rushRoot.transform);
                if (building != null)
                {
                    building.transform.rotation *= GetRushRotationOffset(i, side, 0);
                    AddWindowStreaks(building.transform, windowMaterials[i % windowMaterials.Length], side);
                }
            }

            for (var i = 0; i < 10; i++)
            {
                var side = i % 2 == 0 ? -1f : 1f;
                var x = side * (15f + (i % 3) * 5.2f);
                var y = 20f + (i % 4) * 4.5f;
                var z = 32f + i * 19f;
                var scale = 0.09f + (i % 3) * 0.024f;
                var yaw = side < 0f ? 96f : -96f;
                var overhead = InstantiateBuilding(i + 5, new Vector3(x, y, z), scale, yaw, cityMaterial, rushRoot.transform);
                if (overhead != null)
                {
                    overhead.name = "Overhead Rush Building " + i;
                    overhead.transform.rotation *= GetRushRotationOffset(i, side, 1);
                    AddWindowStreaks(overhead.transform, windowMaterials[(i + 1) % windowMaterials.Length], side);
                }
            }

            for (var i = 0; i < 6; i++)
            {
                var side = i % 2 == 0 ? -1f : 1f;
                var x = side * (16f + (i % 2) * 4.2f);
                var y = -4.5f + (i % 3) * 2.8f;
                var z = 26f + i * 32f;
                var scale = 0.16f + (i % 2) * 0.045f;
                var yaw = side < 0f ? 72f : -72f;
                var near = InstantiateBuilding(i + 11, new Vector3(x, y, z), scale, yaw, cityMaterial, rushRoot.transform);
                if (near != null)
                {
                    near.name = "Near Miss Rush Building " + i;
                    near.transform.rotation *= GetRushRotationOffset(i, side, 2);
                    AddWindowStreaks(near.transform, windowMaterials[(i + 2) % windowMaterials.Length], side);
                }
            }

            CreateColossalFarRotators(materials);

            var driver = rig.Root.AddComponent<ForwardCityRushDemoDriver>();
            var serialized = new SerializedObject(driver);
            serialized.FindProperty("rigRoot").objectReferenceValue = rig.Root.transform;
            serialized.FindProperty("rushRoot").objectReferenceValue = rushRoot.transform;
            serialized.FindProperty("durationSeconds").floatValue = 90f;
            serialized.FindProperty("forwardSpeed").floatValue = 7.5f;
            serialized.FindProperty("objectRushSpeed").floatValue = 24f;
            serialized.FindProperty("recycleLength").floatValue = 330f;
            serialized.FindProperty("frontDistance").floatValue = 285f;
            serialized.FindProperty("backDistance").floatValue = -45f;
            serialized.FindProperty("yawWobbleDegrees").floatValue = 3.6f;
            serialized.FindProperty("rollWobbleDegrees").floatValue = 10f;
            serialized.FindProperty("pitchWobbleDegrees").floatValue = 5.8f;
            serialized.FindProperty("scalePulse").floatValue = 0.045f;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            if (enableSpeedFeel)
                AttachSpeedFeel(rig, speedVolume, false);

            CreateLabel(label, new Vector3(-3.1f, 2.5f, 4f));
        }

        static void CreateColossalFarRotators(VisualMaterials materials)
        {
            var root = new GameObject("Distant Colossal Rotating City Forms");
            var placements = new[]
            {
                new ColossalPlacement(1, new Vector3(-105f, 32f, 235f), 1.9f, new Vector3(0f, 22f, -8f), new Vector3(0.35f, 2.1f, 0.22f), 0.4f, false),
                new ColossalPlacement(4, new Vector3(128f, 42f, 330f), 2.35f, new Vector3(0f, -32f, 13f), new Vector3(-0.25f, -1.35f, 0.31f), 1.9f, false),
                new ColossalPlacement(7, new Vector3(46f, 76f, 455f), 3.05f, new Vector3(18f, 8f, 18f), new Vector3(0.18f, 0.72f, -0.16f), 3.1f, true)
            };

            foreach (var placement in placements)
            {
                var colossal = InstantiateBuilding(
                    placement.AssetIndex,
                    placement.Position,
                    placement.Scale,
                    placement.Euler.y,
                    materials.Colossal,
                    root.transform);

                if (colossal == null)
                    continue;

                colossal.name = "Distant Colossal Rotator";
                colossal.transform.rotation = Quaternion.Euler(placement.Euler);
                if (placement.UseGlitchMaterial)
                    ApplyMaterialToRenderers(colossal, materials.Glitch);
                var driver = colossal.AddComponent<SlowColossalRotationDriver>();
                var serialized = new SerializedObject(driver);
                serialized.FindProperty("angularVelocityDegrees").vector3Value = placement.AngularVelocity;
                serialized.FindProperty("hoverAmplitude").floatValue = 1.2f;
                serialized.FindProperty("hoverFrequency").floatValue = 0.045f;
                serialized.FindProperty("phase").floatValue = placement.Phase;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static Quaternion GetRushRotationOffset(int index, float side, int band)
        {
            var pitchMagnitude = band == 1 ? 16f : band == 2 ? 12f : 8f;
            var rollMagnitude = band == 1 ? 22f : band == 2 ? 18f : 13f;
            var yawMagnitude = band == 1 ? 9f : band == 2 ? 7f : 5f;
            var pitch = ((index % 5) - 2) * pitchMagnitude * 0.42f + (band == 1 ? 10f : -4f);
            var yaw = side * (((index % 4) - 1.5f) * yawMagnitude);
            var roll = -side * (rollMagnitude + (index % 4) * 4.2f);
            return Quaternion.Euler(pitch, yaw, roll);
        }

        static GameObject InstantiateBuilding(int index, Vector3 position, float scale, float yaw, Material fallbackMaterial, Transform parent)
        {
            GameObject instance = null;
            var assetPath = BuildingPaths[index % BuildingPaths.Length];
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (asset != null)
                instance = PrefabUtility.InstantiatePrefab(asset) as GameObject;

            var usingImportedModel = instance != null;
            if (instance == null)
            {
                instance = GameObject.CreatePrimitive(PrimitiveType.Cube);
                instance.transform.localScale = new Vector3(5f, 18f, 5f);
            }

            instance.name = "Rush Building " + index;
            instance.transform.SetParent(parent, false);
            instance.transform.position = position;
            instance.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            instance.transform.localScale = Vector3.one * scale;

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
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                renderer.allowOcclusionWhenDynamic = false;
                if (!usingImportedModel && fallbackMaterial != null)
                    FillRendererMaterials(renderer, fallbackMaterial);
            }

            if (usingImportedModel)
                ApplyUnlitTextureCopies(instance);

            return instance;
        }

        static void AddWindowStreaks(Transform building, Material material, float side)
        {
            for (var i = 0; i < 4; i++)
            {
                var streak = GameObject.CreatePrimitive(PrimitiveType.Cube);
                streak.name = "Rush Window Streak";
                streak.transform.SetParent(building, false);
                streak.transform.localPosition = new Vector3(side * 2.15f, 5f + i * 2.8f, -1.8f - i * 0.3f);
                streak.transform.localRotation = Quaternion.Euler(0f, side * 90f, 0f);
                streak.transform.localScale = new Vector3(0.08f, 1.1f, 0.018f);
                streak.GetComponent<Renderer>().sharedMaterial = material;
                Object.DestroyImmediate(streak.GetComponent<Collider>());
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

                if (changed)
                    importer.SaveAndReimport();
            }
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
            Directory.CreateDirectory(RushUnlitMaterialFolder);
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                var sourceMaterials = renderer.sharedMaterials;
                if (sourceMaterials == null || sourceMaterials.Length == 0)
                    continue;

                var unlitMaterials = new Material[sourceMaterials.Length];
                for (var i = 0; i < sourceMaterials.Length; i++)
                {
                    var source = sourceMaterials[i];
                    if (source == null)
                        continue;

                    var texture = source.HasProperty("_BaseMap") ? source.GetTexture("_BaseMap") : source.HasProperty("_MainTex") ? source.GetTexture("_MainTex") : null;
                    var color = source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") : source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white;
                    var safeName = source.name.Replace("/", "_").Replace("\\", "_");
                    var materialPath = $"{RushUnlitMaterialFolder}/{safeName}_RushUnlit.mat";
                    unlitMaterials[i] = CreateUnlitTextureMaterial(materialPath, color, texture);
                }

                renderer.sharedMaterials = unlitMaterials;
            }
        }

        static Material CreateUnlitTextureMaterial(string path, Color baseColor, Texture texture)
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
            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 0f);
            if (material.HasProperty("_Blend"))
                material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_ZWrite"))
                material.SetFloat("_ZWrite", 1f);
            if (material.HasProperty("_Cull"))
                material.SetFloat("_Cull", 0f);

            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Geometry;
            EditorUtility.SetDirty(material);
            return material;
        }

        static void ApplyMaterialToRenderers(GameObject root, Material material)
        {
            if (root == null || material == null)
                return;

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                FillRendererMaterials(renderer, material);
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

        static void CreateLabel(string text, Vector3 position)
        {
            var textObject = new GameObject("Demo Floating Label");
            textObject.transform.position = position;
            textObject.transform.localScale = Vector3.one * 0.08f;
            var label = textObject.AddComponent<TextMesh>();
            label.text = text;
            label.fontSize = 64;
            label.characterSize = 0.22f;
            label.anchor = TextAnchor.MiddleLeft;
            label.alignment = TextAlignment.Left;
            label.color = new Color(0.82f, 0.96f, 1f, 1f);
        }

        static Material SaveMaterial(string path, Color color, bool transparent, float emission = 1f)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");

            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color * emission);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color * emission);
            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", transparent ? 1f : 0f);
            if (material.HasProperty("_Blend"))
                material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_ZWrite"))
                material.SetFloat("_ZWrite", transparent ? 0f : 1f);
            if (material.HasProperty("_Cull"))
                material.SetFloat("_Cull", transparent ? 0f : 2f);

            if (transparent)
            {
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = (int)RenderQueue.Transparent;
            }
            else
            {
                material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = (int)RenderQueue.Geometry;
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        static Material SaveGlitchMaterial(string path)
        {
            var shader = Shader.Find("RhythmParkour/URP Glitch Silhouette");
            if (shader == null)
                return SaveMaterial(path, new Color(0.08f, 0.16f, 0.36f, 1f), false, 1.1f);

            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.SetColor("_BaseColor", new Color(0.055f, 0.12f, 0.32f, 1f));
            material.SetColor("_GlitchColor", new Color(0.12f, 0.95f, 1f, 1f));
            material.SetColor("_AccentColor", new Color(1f, 0.14f, 0.82f, 1f));
            material.SetFloat("_BandDensity", 30f);
            material.SetFloat("_GlitchStrength", 0.38f);
            material.SetFloat("_PulseSpeed", 0.55f);
            material.renderQueue = (int)RenderQueue.Geometry;
            EditorUtility.SetDirty(material);
            return material;
        }

        readonly struct PlayerRig
        {
            public PlayerRig(GameObject root, GameObject trackingSpace, GameObject camera, GameObject leftController, GameObject rightController)
            {
                Root = root;
                TrackingSpace = trackingSpace;
                Camera = camera;
                LeftController = leftController;
                RightController = rightController;
            }

            public GameObject Root { get; }
            public GameObject TrackingSpace { get; }
            public GameObject Camera { get; }
            public GameObject LeftController { get; }
            public GameObject RightController { get; }
        }

        readonly struct ColossalPlacement
        {
            public ColossalPlacement(int assetIndex, Vector3 position, float scale, Vector3 euler, Vector3 angularVelocity, float phase, bool useGlitchMaterial)
            {
                AssetIndex = assetIndex;
                Position = position;
                Scale = scale;
                Euler = euler;
                AngularVelocity = angularVelocity;
                Phase = phase;
                UseGlitchMaterial = useGlitchMaterial;
            }

            public int AssetIndex { get; }
            public Vector3 Position { get; }
            public float Scale { get; }
            public Vector3 Euler { get; }
            public Vector3 AngularVelocity { get; }
            public float Phase { get; }
            public bool UseGlitchMaterial { get; }
        }

        sealed class VisualMaterials
        {
            public Material Bridge { get; private set; }
            public Material Edge { get; private set; }
            public Material Beat { get; private set; }
            public Material Downbeat { get; private set; }
            public Material Danger { get; private set; }
            public Material Hook { get; private set; }
            public Material Rope { get; private set; }
            public Material CitySilhouette { get; private set; }
            public Material WindowCyan { get; private set; }
            public Material WindowMagenta { get; private set; }
            public Material WindowRed { get; private set; }
            public Material Cloud { get; private set; }
            public Material Colossal { get; private set; }
            public Material Glitch { get; private set; }

            public static VisualMaterials Create(string folder)
            {
                return new VisualMaterials
                {
                    Bridge = SaveMaterial(folder + "/M_VisualDemo_LightBridge.mat", new Color(0.05f, 0.86f, 1f, 0.28f), true, 1.2f),
                    Edge = SaveMaterial(folder + "/M_VisualDemo_Edge.mat", new Color(0.5f, 1f, 1f, 0.76f), true, 1.4f),
                    Beat = SaveMaterial(folder + "/M_VisualDemo_Beat.mat", new Color(0.18f, 0.62f, 1f, 0.46f), true, 1.15f),
                    Downbeat = SaveMaterial(folder + "/M_VisualDemo_Downbeat.mat", new Color(1f, 0.22f, 0.88f, 0.62f), true, 1.25f),
                    Danger = SaveMaterial(folder + "/M_VisualDemo_DepthDanger.mat", new Color(1f, 0.08f, 0.16f, 0.18f), true, 1f),
                    Hook = SaveMaterial(folder + "/M_VisualDemo_GrappleHook.mat", new Color(0.74f, 0.98f, 1f, 0.95f), true, 1.4f),
                    Rope = SaveMaterial(folder + "/M_VisualDemo_GrappleCable.mat", new Color(1f, 0.04f, 0.08f, 0.86f), true, 1.55f),
                    CitySilhouette = SaveMaterial(folder + "/M_VisualDemo_CitySilhouette.mat", new Color(0.11f, 0.2f, 0.36f, 1f), false, 1.18f),
                    WindowCyan = SaveMaterial(folder + "/M_VisualDemo_WindowCyan.mat", new Color(0.08f, 0.95f, 1f, 0.82f), true, 1.8f),
                    WindowMagenta = SaveMaterial(folder + "/M_VisualDemo_WindowMagenta.mat", new Color(1f, 0.18f, 0.95f, 0.82f), true, 1.65f),
                    WindowRed = SaveMaterial(folder + "/M_VisualDemo_WindowRed.mat", new Color(1f, 0.08f, 0.12f, 0.78f), true, 1.45f),
                    Cloud = SaveMaterial(folder + "/M_VisualDemo_CloudSheet.mat", new Color(0.44f, 0.76f, 1f, 0.34f), true, 1.35f),
                    Colossal = SaveMaterial(folder + "/M_VisualDemo_ColossalSilhouette.mat", new Color(0.075f, 0.12f, 0.27f, 1f), false, 1.08f),
                    Glitch = SaveGlitchMaterial(folder + "/M_VisualDemo_GlitchColossal.mat")
                };
            }
        }
    }
}
