using System.IO;
using System.Collections.Generic;
using BoringRun.VRInput;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace RhythmParkour.Editor
{
    public static class FormalDemoEditBaseSceneUpdater
    {
        const string ScenePath = "Assets/Scenes/FormalDemos/TellingWorldFormalDemo_EditBase.unity";
        const string RequestPath = "Assets/Editor/RhythmParkour/UpdateTellingWorldFormalEditBaseVisuals.request";
        const string SkyboxPath = "Assets/Materials/Stage/M_FormalDemoExpandedRedSkybox.mat";
        const string VolumeProfilePath = "Assets/Settings/Volumes/FormalDemos/TellingWorldFormalDemo_EditBase_VolumetricCloudsProfile.asset";
        const string GrappleRopeMaterialPath = "Assets/Materials/VisualDemos/M_VisualDemo_GrappleCable.mat";
        const string GrappleClawMaterialPath = "Assets/Materials/VisualDemos/M_VisualDemo_GrappleHook.mat";
        const string VisualMaterialFolder = "Assets/Materials/VisualDemos";
        const string UiMaterialFolder = "Assets/Materials/UI";
        const string ResultPanelMaterialPath = UiMaterialFolder + "/M_Result_Panel_Back.mat";
        const string RushUnlitMaterialFolder = VisualMaterialFolder + "/NeoCityRushUnlit";
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
            EditorApplication.update -= UpdateIfRequested;
            EditorApplication.update += UpdateIfRequested;
        }

        static void UpdateIfRequested()
        {
            if (!File.Exists(RequestPath))
                return;

            EditorApplication.update -= UpdateIfRequested;
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    EditorApplication.isPlaying = false;
                    EditorApplication.update += UpdateIfRequested;
                    return;
                }

                UpdateScene();
                AssetDatabase.DeleteAsset(RequestPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorApplication.update += UpdateIfRequested;
            };
        }

        [MenuItem("Rhythm Parkour/Update Telling World Formal Edit Base Visuals")]
        public static void UpdateScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ConfigureRenderSettings();
            DisableVolumetricCloudVolume();
            ConfigureGrappleFeedback();
            ConfigurePortalSuccessTrigger();
            ConfigureGrappleAnchorSides();
            ConfigureResultPanelVisuals();
            ReplaceSlideVisuals();
            RemoveLegacyMeshCloudSheets();
            ReplaceSimpleSetDressingWithCityRush();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[RhythmParkour] Updated formal edit-base visuals in {ScenePath}.");
        }

        static void ConfigureRenderSettings()
        {
            var skybox = AssetDatabase.LoadAssetAtPath<Material>(SkyboxPath);
            if (skybox != null)
            {
                RenderSettings.skybox = skybox;
                if (skybox.HasProperty("_CloudStrength"))
                    skybox.SetFloat("_CloudStrength", 0.28f);
                if (skybox.HasProperty("_CloudCoverage"))
                    skybox.SetFloat("_CloudCoverage", 0.58f);
                if (skybox.HasProperty("_CloudColor"))
                    skybox.SetColor("_CloudColor", new Color(0.46f, 0.78f, 1f, 1f));
                EditorUtility.SetDirty(skybox);
            }

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.2f, 0.24f, 0.36f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.018f, 0.035f, 0.085f);
            RenderSettings.fogDensity = 0.0045f;
        }

        static void DisableVolumetricCloudVolume()
        {
            EnsureFolder("Assets/Settings", "Volumes");
            EnsureFolder("Assets/Settings/Volumes", "FormalDemos");

            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.name = "TellingWorldFormalDemo EditBase Volumetric Clouds";
                AssetDatabase.CreateAsset(profile, VolumeProfilePath);
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
            clouds.state.value = false;
            clouds.localClouds.overrideState = true;
            clouds.localClouds.value = true;
            clouds.cloudPreset = VolumetricClouds.CloudPresets.Cloudy;
            clouds.densityMultiplier.overrideState = true;
            clouds.densityMultiplier.value = 0.42f;
            clouds.shapeFactor.overrideState = true;
            clouds.shapeFactor.value = 0.44f;
            clouds.shapeScale.overrideState = true;
            clouds.shapeScale.value = 7.6f;
            clouds.erosionFactor.overrideState = true;
            clouds.erosionFactor.value = 0.52f;
            clouds.erosionScale.overrideState = true;
            clouds.erosionScale.value = 84f;
            clouds.bottomAltitude.overrideState = true;
            clouds.bottomAltitude.value = 18f;
            clouds.altitudeRange.overrideState = true;
            clouds.altitudeRange.value = 110f;
            clouds.earthCurvature.overrideState = true;
            clouds.earthCurvature.value = 0f;
            clouds.globalSpeed.overrideState = true;
            clouds.globalSpeed.value = 4f;
            clouds.globalOrientation.overrideState = true;
            clouds.globalOrientation.value = 35f;
            EditorUtility.SetDirty(clouds);
            EditorUtility.SetDirty(profile);

            var volumeObject = GameObject.Find("Global Volumetric Clouds");
            if (volumeObject != null)
                Object.DestroyImmediate(volumeObject);
        }

        static void ConfigureGrappleFeedback()
        {
            var poseBinder = Object.FindObjectOfType<VRControllerPoseBinder>(true);
            if (poseBinder != null)
            {
                poseBinder.enabled = true;
                EditorUtility.SetDirty(poseBinder);
            }

            var feedback = Object.FindObjectOfType<FirstPersonActionFeedbackDriver>(true);
            if (feedback == null)
                return;

            var prototype = Object.FindObjectOfType<VRRhythmActionPrototype>(true);
            if (prototype != null)
            {
                var prototypeSerialized = new SerializedObject(prototype);
                SetIfExists(prototypeSerialized, "successSoundVolume", 0.46f);
                SetIfExists(prototypeSerialized, "failureSoundVolume", 0.56f);
                SetIfExists(prototypeSerialized, "tailSilenceSeconds", 10f);
                prototypeSerialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(prototype);
            }

            var speedFeel = Object.FindObjectOfType<VRSpeedFeelDriver>(true);
            if (speedFeel != null)
            {
                var speedSerialized = new SerializedObject(speedFeel);
                SetIfExists(speedSerialized, "baseVignetteColor", new Color(0.02f, 0.01f, 0.05f, 1f));
                SetIfExists(speedSerialized, "failureVignetteColor", new Color(1f, 0.025f, 0.015f, 1f));
                SetIfExists(speedSerialized, "failureVignette", 0.68f);
                SetIfExists(speedSerialized, "failurePulseDecaySpeed", 3.8f);
                speedSerialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(speedFeel);
            }

            var serialized = new SerializedObject(feedback);
            SetIfExists(serialized, "baseActionDurationSeconds", 0.42f);
            SetIfExists(serialized, "stepBob", 0.22f);
            SetIfExists(serialized, "stepForward", 0.34f);
            SetIfExists(serialized, "stepSideSway", 0.045f);
            SetIfExists(serialized, "stepLandingDip", 0.08f);
            SetIfExists(serialized, "sideGrabLift", 0.34f);
            SetIfExists(serialized, "sideGrabSway", 0.22f);
            SetIfExists(serialized, "sideGrabForward", 0.34f);
            SetIfExists(serialized, "sideGrabLandingDip", 0.08f);
            SetIfExists(serialized, "sideGrabYawDegrees", 5f);
            SetIfExists(serialized, "slideLowering", 0.42f);
            SetIfExists(serialized, "slideForward", 0.5f);
            SetIfExists(serialized, "slideDropStage", 0.2f);
            SetIfExists(serialized, "slideHoldStage", 0.74f);
            SetIfExists(serialized, "longJumpLift", 0.9f);
            SetIfExists(serialized, "longJumpForward", 1.35f);
            SetIfExists(serialized, "longJumpLandingDip", 0.16f);
            SetIfExists(serialized, "gravityFallPower", 1.55f);
            SetIfExists(serialized, "stepRollDegrees", 3.8f);
            SetIfExists(serialized, "stepYawDegrees", 1.6f);
            SetIfExists(serialized, "longJumpTakeoffLookUpDegrees", 10f);
            SetIfExists(serialized, "longJumpAirLookDownDegrees", 6.5f);
            SetIfExists(serialized, "longJumpLandingRollDegrees", 3.2f);
            SetIfExists(serialized, "longJumpTakeoffStage", 0.2f);
            SetIfExists(serialized, "longJumpFloatStage", 0.85f);
            SetIfExists(serialized, "usePendulumGrappleMotion", true);
            SetIfExists(serialized, "grapplePendulumBaseRadius", 4.2f);
            SetIfExists(serialized, "grapplePendulumRadiusPerBeat", 0.38f);
            SetIfExists(serialized, "grapplePendulumAngleDegrees", 56f);
            SetIfExists(serialized, "grapplePendulumRollDegrees", 10f);
            SetIfExists(serialized, "grappleHookForward", 6.6f);
            SetIfExists(serialized, "grappleHookHeight", 6.2f);
            SetIfExists(serialized, "grappleHookSideOffset", 0.72f);
            SetIfExists(serialized, "grappleHookAnchorVerticalOffset", 1.25f);
            SetIfExists(serialized, "grappleBodyVerticalOffset", 1.05f);
            SetIfExists(serialized, "grappleRopeRadius", 0.075f);
            SetIfExists(serialized, "grappleClawScale", new Vector3(0.64f, 0.32f, 0.64f));
            SetIfExists(serialized, "grappleForward", 0.12f);
            SetIfExists(serialized, "grappleLift", 0.02f);
            SetObjectIfExists(serialized, "leftRopeEndPoint", poseBinder != null && poseBinder.LeftController != null ? poseBinder.LeftController : FindTransform("LeftControllerAnchor"));
            SetObjectIfExists(serialized, "rightRopeEndPoint", poseBinder != null && poseBinder.RightController != null ? poseBinder.RightController : FindTransform("RightControllerAnchor"));
            SetObjectIfExists(serialized, "grappleRopeMaterial", AssetDatabase.LoadAssetAtPath<Material>(GrappleRopeMaterialPath));
            SetObjectIfExists(serialized, "grappleClawMaterial", AssetDatabase.LoadAssetAtPath<Material>(GrappleClawMaterialPath));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(feedback);
        }

        static void ConfigurePortalSuccessTrigger()
        {
            var trigger = Object.FindObjectOfType<RhythmPortalSuccessTrigger>(true);
            if (trigger == null)
                return;

            var rigRoot = FindTransform("[BuildingBlock] Camera Rig");
            var prototype = Object.FindObjectOfType<VRRhythmActionPrototype>(true);
            var serialized = new SerializedObject(trigger);
            SetObjectIfExists(serialized, "playerRoot", rigRoot);
            SetObjectIfExists(serialized, "actionSource", prototype);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(trigger);
        }

        static void ConfigureGrappleAnchorSides()
        {
            var prototype = Object.FindObjectOfType<VRRhythmActionPrototype>(true);
            if (prototype == null)
                return;

            var serialized = new SerializedObject(prototype);
            var track = serialized.FindProperty("track").objectReferenceValue as RhythmTrackConfig;
            if (track == null || track.Chart == null || track.Chart.Events == null)
                return;

            foreach (var evt in track.Chart.Events)
            {
                if (evt == null || evt.ActionType != RhythmActionType.Grapple)
                    continue;

                var anchor = FindTransform($"Grapple Hook Anchor Beat {evt.Beat:0.##}");
                if (anchor == null)
                    anchor = FindTransform($"Grapple Hook Anchor Beat {Mathf.RoundToInt(evt.Beat)}");
                if (anchor == null)
                    continue;

                var position = anchor.position;
                position.x = evt.Hand == RhythmHand.Left ? -Mathf.Abs(position.x) : Mathf.Abs(position.x);
                anchor.position = position;
                EditorUtility.SetDirty(anchor);
            }
        }

        static void ConfigureResultPanelVisuals()
        {
            var panelMaterial = AssetDatabase.LoadAssetAtPath<Material>(ResultPanelMaterialPath);
            if (panelMaterial != null)
            {
                var color = new Color(0.018f, 0.014f, 0.045f, 1f);
                if (panelMaterial.HasProperty("_BaseColor"))
                    panelMaterial.SetColor("_BaseColor", color);
                else
                    panelMaterial.color = color;
                if (panelMaterial.HasProperty("_EmissionColor"))
                    panelMaterial.SetColor("_EmissionColor", color * 1.4f);
                panelMaterial.SetOverrideTag("RenderType", "Opaque");
                panelMaterial.renderQueue = (int)RenderQueue.Geometry;
                if (panelMaterial.HasProperty("_Surface"))
                    panelMaterial.SetFloat("_Surface", 0f);
                if (panelMaterial.HasProperty("_Blend"))
                    panelMaterial.SetFloat("_Blend", 0f);
                if (panelMaterial.HasProperty("_AlphaClip"))
                    panelMaterial.SetFloat("_AlphaClip", 0f);
                EditorUtility.SetDirty(panelMaterial);
            }

            SetLocalPoseIfExists("Result Back Plate", new Vector3(0f, 0f, 0.1f), new Vector3(4.3f, 3.05f, 0.1f));
            ApplyRendererMaterialIfExists("Result Back Plate", panelMaterial);
            SetTextIfExists("Result Title", new Vector3(0f, 1.03f, -0.08f), Vector3.one * 0.13f, Color.white);
            SetTextIfExists("Result Stats", new Vector3(0f, 0.44f, -0.08f), Vector3.one * 0.043f, new Color(0.88f, 0.98f, 1f, 1f));
            SetTextIfExists("Result Hint", new Vector3(0f, -1.28f, -0.08f), Vector3.one * 0.034f, new Color(0.62f, 0.9f, 1f, 0.95f));
            SetLocalPoseIfExists("Result Button Retry", new Vector3(-1f, -0.94f, -0.08f), null);
            SetLocalPoseIfExists("Result Button Main Menu", new Vector3(1f, -0.94f, -0.08f), null);
            SetTextGroupIfExists("Label", null, Vector3.one * 0.064f, Color.white);
            SetTextGroupIfExists("Detail", null, Vector3.one * 0.03f, new Color(0.78f, 0.92f, 1f, 0.94f));
        }

        static void ReplaceSlideVisuals()
        {
            var slideGates = FindTransformsWithPrefix("Slide Gate Beat ");
            DestroyTransformsWithPrefix("Slide Clearance Shadow Beat ");
            DestroyTransformsWithPrefix("Slide Floor Trail Beat ");
            DestroyTransformsWithPrefix("Slide Floor Arrow Beat ");

            if (slideGates.Count == 0)
                return;

            var theme = RhythmParkourLevelTheme.CreateDefault();
            DestroyNamedRoot("Formal Demo Slide Visuals");
            DestroyTransformsWithPrefix("Slide Cue Beat ");
            DestroyTransformsWithPrefix("Slide Low Barrier Beat ");

            var root = new GameObject("Formal Demo Slide Visuals");
            foreach (var gate in slideGates)
            {
                if (gate == null)
                    continue;

                var beatLabel = gate.name.Substring("Slide Gate Beat ".Length);
                var z = gate.position.z;
                var obstacleZ = z + 1.55f;
                CreateCube($"Slide Cue Beat {beatLabel}", root.transform, new Vector3(0f, 0.13f, z), new Vector3(1.55f, 0.1f, 0.42f), theme.Action);
                CreateCube($"Slide Low Barrier Beat {beatLabel}", root.transform, new Vector3(0f, 1.18f, obstacleZ), new Vector3(2.9f, 0.34f, 0.34f), theme.Danger);
            }

            for (var i = slideGates.Count - 1; i >= 0; i--)
            {
                if (slideGates[i] != null)
                    Object.DestroyImmediate(slideGates[i].gameObject);
            }
        }

        static void RemoveLegacyMeshCloudSheets()
        {
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var root in roots)
                RemoveLegacyMeshCloudSheets(root.transform);
        }

        static void ReplaceSimpleSetDressingWithCityRush()
        {
            DestroyNamedRoot("Formal Demo Visual Set Dressing");
            DestroyNamedRoot("Opposing Direction City Rush Field");
            DestroyNamedRoot("Formal Demo Neo City Rush Field");
            DestroyNamedRoot("Distant Colossal Rotating City Forms");

            ConfigureKitModelImporters();

            var rigRoot = FindTransform("[BuildingBlock] Camera Rig");
            if (rigRoot == null)
                return;

            foreach (var existing in rigRoot.GetComponents<ForwardCityRushDemoDriver>())
                Object.DestroyImmediate(existing);

            var materials = CityRushMaterials.Load();
            var rushRoot = new GameObject("Formal Demo Neo City Rush Field");
            var windowMaterials = new[] { materials.WindowCyan, materials.WindowMagenta, materials.WindowRed };

            for (var i = 0; i < 24; i++)
            {
                var side = i % 2 == 0 ? -1f : 1f;
                var lane = i % 4;
                var x = side * (24f + lane * 12f);
                var y = -24f - (i % 5) * 7.5f;
                var z = 18f + i * 12.8f;
                var scale = 0.12f + (i % 3) * 0.036f;
                var yaw = side < 0f ? 18f + i * 3f : -18f - i * 3f;
                var building = InstantiateBuilding(i, new Vector3(x, y, z), scale, yaw, materials.CitySilhouette, rushRoot.transform);
                if (building != null)
                {
                    building.transform.rotation *= GetRushRotationOffset(i, side, 0);
                    AddWindowStreaks(building.transform, windowMaterials[i % windowMaterials.Length], side);
                }
            }

            for (var i = 0; i < 10; i++)
            {
                var side = i % 2 == 0 ? -1f : 1f;
                var x = side * (18f + (i % 3) * 6.4f);
                var y = 22f + (i % 4) * 5.2f;
                var z = 36f + i * 21f;
                var scale = 0.09f + (i % 3) * 0.024f;
                var yaw = side < 0f ? 96f : -96f;
                var overhead = InstantiateBuilding(i + 5, new Vector3(x, y, z), scale, yaw, materials.CitySilhouette, rushRoot.transform);
                if (overhead == null)
                    continue;

                overhead.name = "Overhead Rush Building " + i;
                overhead.transform.rotation *= GetRushRotationOffset(i, side, 1);
                AddWindowStreaks(overhead.transform, windowMaterials[(i + 1) % windowMaterials.Length], side);
            }

            for (var i = 0; i < 6; i++)
            {
                var side = i % 2 == 0 ? -1f : 1f;
                var x = side * (18f + (i % 2) * 5.2f);
                var y = -5.5f + (i % 3) * 2.8f;
                var z = 30f + i * 34f;
                var scale = 0.16f + (i % 2) * 0.045f;
                var yaw = side < 0f ? 72f : -72f;
                var near = InstantiateBuilding(i + 11, new Vector3(x, y, z), scale, yaw, materials.CitySilhouette, rushRoot.transform);
                if (near == null)
                    continue;

                near.name = "Near Miss Rush Building " + i;
                near.transform.rotation *= GetRushRotationOffset(i, side, 2);
                AddWindowStreaks(near.transform, windowMaterials[(i + 2) % windowMaterials.Length], side);
            }

            CreateColossalFarRotators(materials);
            AttachCityRushDriver(rigRoot, rushRoot.transform);
        }

        static void AttachCityRushDriver(Transform rigRoot, Transform rushRoot)
        {
            var driver = rigRoot.gameObject.AddComponent<ForwardCityRushDemoDriver>();
            var serialized = new SerializedObject(driver);
            serialized.FindProperty("rigRoot").objectReferenceValue = rigRoot;
            serialized.FindProperty("rushRoot").objectReferenceValue = rushRoot;
            serialized.FindProperty("actionSource").objectReferenceValue = Object.FindObjectOfType<VRRhythmActionPrototype>(true);
            serialized.FindProperty("durationSeconds").floatValue = 150f;
            serialized.FindProperty("driveRigRoot").boolValue = false;
            serialized.FindProperty("forwardSpeed").floatValue = 0f;
            serialized.FindProperty("objectRushSpeed").floatValue = 24f;
            serialized.FindProperty("recycleLength").floatValue = 360f;
            serialized.FindProperty("frontDistance").floatValue = 300f;
            serialized.FindProperty("backDistance").floatValue = -55f;
            serialized.FindProperty("lateralBreath").floatValue = 0f;
            serialized.FindProperty("yawWobbleDegrees").floatValue = 3.4f;
            serialized.FindProperty("rollWobbleDegrees").floatValue = 9.5f;
            serialized.FindProperty("pitchWobbleDegrees").floatValue = 5.4f;
            serialized.FindProperty("scalePulse").floatValue = 0.035f;
            serialized.FindProperty("glitchRushOverTime").boolValue = true;
            serialized.FindProperty("glitchMaterial").objectReferenceValue = CityRushMaterials.Load().Glitch;
            serialized.FindProperty("glitchStartSeconds").floatValue = 28f;
            serialized.FindProperty("glitchFullSeconds").floatValue = 105f;
            serialized.FindProperty("glitchByBeat").boolValue = true;
            serialized.FindProperty("glitchStartBeat").floatValue = 72f;
            serialized.FindProperty("glitchFullBeat").floatValue = 178f;
            serialized.FindProperty("waitForPositiveBeat").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(driver);
        }

        static void CreateColossalFarRotators(CityRushMaterials materials)
        {
            var root = new GameObject("Distant Colossal Rotating City Forms");
            var placements = new[]
            {
                new ColossalPlacement(1, new Vector3(-168f, 44f, 250f), 1.85f, new Vector3(0f, 32f, -8f), new Vector3(0.35f, 2.1f, 0.22f), 0.4f, false),
                new ColossalPlacement(4, new Vector3(186f, 54f, 365f), 2.25f, new Vector3(0f, -42f, 13f), new Vector3(-0.25f, -1.35f, 0.31f), 1.9f, false),
                new ColossalPlacement(7, new Vector3(-118f, 86f, 505f), 2.85f, new Vector3(18f, 24f, 18f), new Vector3(0.18f, 0.72f, -0.16f), 3.1f, true)
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
                EditorUtility.SetDirty(driver);
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
            if (material == null)
                return;

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

        static void ApplyMaterialToRenderers(GameObject root, Material material)
        {
            if (root == null || material == null)
                return;

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                FillRendererMaterials(renderer, material);
        }

        static void DestroyNamedRoot(string rootName)
        {
            var target = GameObject.Find(rootName);
            if (target != null)
                Object.DestroyImmediate(target);
        }

        static GameObject CreateCube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, true);
            cube.transform.position = position;
            cube.transform.localScale = scale;
            var renderer = cube.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = material;
            return cube;
        }

        static List<Transform> FindTransformsWithPrefix(string prefix)
        {
            var results = new List<Transform>();
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
                FindTransformsWithPrefix(roots[i].transform, prefix, results);
            return results;
        }

        static void FindTransformsWithPrefix(Transform root, string prefix, List<Transform> results)
        {
            if (root.name.StartsWith(prefix, System.StringComparison.Ordinal))
                results.Add(root);

            foreach (Transform child in root)
                FindTransformsWithPrefix(child, prefix, results);
        }

        static void DestroyTransformsWithPrefix(string prefix)
        {
            var targets = FindTransformsWithPrefix(prefix);
            for (var i = targets.Count - 1; i >= 0; i--)
            {
                if (targets[i] != null)
                    Object.DestroyImmediate(targets[i].gameObject);
            }
        }

        static void RemoveLegacyMeshCloudSheets(Transform root)
        {
            for (var i = root.childCount - 1; i >= 0; i--)
                RemoveLegacyMeshCloudSheets(root.GetChild(i));

            if (root.name.StartsWith("Upper Cloud Sheet", System.StringComparison.Ordinal))
                Object.DestroyImmediate(root.gameObject);
        }

        static Transform FindTransform(string transformName)
        {
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var result = FindTransform(roots[i].transform, transformName);
                if (result != null)
                    return result;
            }

            return null;
        }

        static void SetLocalPoseIfExists(string transformName, Vector3 localPosition, Vector3? localScale)
        {
            var transform = FindTransform(transformName);
            if (transform == null)
                return;

            transform.localPosition = localPosition;
            if (localScale.HasValue)
                transform.localScale = localScale.Value;
            EditorUtility.SetDirty(transform);
        }

        static void SetTextIfExists(string transformName, Vector3? localPosition, Vector3? localScale, Color color)
        {
            var transform = FindTransform(transformName);
            if (transform == null)
                return;

            ApplyTextVisual(transform, localPosition, localScale, color);
        }

        static void SetTextGroupIfExists(string transformName, Vector3? localPosition, Vector3? localScale, Color color)
        {
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
                SetTextGroupIfExists(roots[i].transform, transformName, localPosition, localScale, color);
        }

        static void SetTextGroupIfExists(Transform root, string transformName, Vector3? localPosition, Vector3? localScale, Color color)
        {
            if (root.name == transformName)
                ApplyTextVisual(root, localPosition, localScale, color);

            foreach (Transform child in root)
                SetTextGroupIfExists(child, transformName, localPosition, localScale, color);
        }

        static void ApplyTextVisual(Transform transform, Vector3? localPosition, Vector3? localScale, Color color)
        {
            if (localPosition.HasValue)
                transform.localPosition = localPosition.Value;
            if (localScale.HasValue)
                transform.localScale = localScale.Value;

            var text = transform.GetComponent<TextMesh>();
            if (text != null)
            {
                text.color = color;
                text.fontSize = 112;
                text.characterSize = 0.22f;
                EditorUtility.SetDirty(text);
            }

            EditorUtility.SetDirty(transform);
        }

        static void ApplyRendererMaterialIfExists(string transformName, Material material)
        {
            if (material == null)
                return;

            var transform = FindTransform(transformName);
            if (transform == null)
                return;

            var renderer = transform.GetComponent<Renderer>();
            if (renderer == null)
                return;

            renderer.sharedMaterial = material;
            EditorUtility.SetDirty(renderer);
        }

        static Transform FindTransform(Transform root, string transformName)
        {
            if (root.name == transformName)
                return root;

            foreach (Transform child in root)
            {
                var result = FindTransform(child, transformName);
                if (result != null)
                    return result;
            }

            return null;
        }

        static void SetIfExists(SerializedObject serialized, string propertyName, bool value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property != null)
                property.boolValue = value;
        }

        static void SetIfExists(SerializedObject serialized, string propertyName, float value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property != null)
                property.floatValue = value;
        }

        static void SetIfExists(SerializedObject serialized, string propertyName, Vector3 value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property != null)
                property.vector3Value = value;
        }

        static void SetIfExists(SerializedObject serialized, string propertyName, Color value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property != null)
                property.colorValue = value;
        }

        static void SetObjectIfExists(SerializedObject serialized, string propertyName, Object value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property != null)
                property.objectReferenceValue = value;
        }

        static void EnsureFolder(string parent, string child)
        {
            var path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
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

        sealed class CityRushMaterials
        {
            public Material CitySilhouette { get; private set; }
            public Material WindowCyan { get; private set; }
            public Material WindowMagenta { get; private set; }
            public Material WindowRed { get; private set; }
            public Material Colossal { get; private set; }
            public Material Glitch { get; private set; }

            public static CityRushMaterials Load()
            {
                return new CityRushMaterials
                {
                    CitySilhouette = AssetDatabase.LoadAssetAtPath<Material>(VisualMaterialFolder + "/M_VisualDemo_CitySilhouette.mat"),
                    WindowCyan = AssetDatabase.LoadAssetAtPath<Material>(VisualMaterialFolder + "/M_VisualDemo_WindowCyan.mat"),
                    WindowMagenta = AssetDatabase.LoadAssetAtPath<Material>(VisualMaterialFolder + "/M_VisualDemo_WindowMagenta.mat"),
                    WindowRed = AssetDatabase.LoadAssetAtPath<Material>(VisualMaterialFolder + "/M_VisualDemo_WindowRed.mat"),
                    Colossal = AssetDatabase.LoadAssetAtPath<Material>(VisualMaterialFolder + "/M_VisualDemo_ColossalSilhouette.mat"),
                    Glitch = AssetDatabase.LoadAssetAtPath<Material>(VisualMaterialFolder + "/M_VisualDemo_GlitchColossal.mat")
                };
            }
        }
    }
}
