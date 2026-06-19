using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BoringRun.VRInput;
using RhythmParkour;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RhythmParkour.Editor
{
    public static class MusicVrDemoSceneBuilder
    {
        const string SourceTrackPath = "Assets/Rhythm/Tutorial_120BPM_Track.asset";
        const string RhythmFolder = "Assets/Rhythm/MusicVrDemos";
        const string SceneFolder = "Assets/Scenes/MusicVrDemos";
        const string VolumeProfileFolder = "Assets/Settings/Volumes/MusicVrDemos";
        const string RecordingRhythmFolder = "Assets/Rhythm/RecordingDemos";
        const string RecordingSceneFolder = "Assets/Scenes/RecordingDemos";
        const string RecordingVolumeProfileFolder = "Assets/Settings/Volumes/RecordingDemos";
        const string FormalRhythmFolder = "Assets/Rhythm/FormalDemos";
        const string FormalSceneFolder = "Assets/Scenes/FormalDemos";
        const string FormalVolumeProfileFolder = "Assets/Settings/Volumes/FormalDemos";
        const string TellingWorldAudioPath = "Assets/Audio/TellingWorld.mp3";
        const string TellingWorldFormalChartTablePath = "Assets/Rhythm/FormalDemos/TellingWorldFormalDemo_Chart.tsv";
        const string SkyboxPath = "Assets/Materials/Stage/M_SoftNightPanoramicSkybox.mat";
        const string VisualSkyboxPath = "Assets/Materials/VisualDemos/M_VisualDemo_Skybox.mat";
        const string GameFlowMainMenuSceneName = "VRMainMenu";
        const string MenuMaterialFolder = "Assets/Materials/UI";
        const string RequestPath = "Assets/Editor/RhythmParkour/RebuildMusicVrDemoScenes.request";
        const string RecordingRequestPath = "Assets/Editor/RhythmParkour/RebuildRecordingDemoScene.request";
        const string TellingWorldRequestPath = "Assets/Editor/RhythmParkour/RebuildTellingWorldCalibrationDemo.request";
        const string TellingWorldFormalRequestPath = "Assets/Editor/RhythmParkour/RebuildTellingWorldFormalDemo.request";
        const string TellingWorldFormalEditBaseRequestPath = "Assets/Editor/RhythmParkour/RebuildTellingWorldFormalEditBase.request";
        const float UnitsPerBeat = 2.5f;
        const float TellingWorldInitialBpm = 150f;
        const float TellingWorldInitialFirstBeatOffsetSeconds = 4.57f;

        static readonly DemoDefinition[] Demos =
        {
            new("Demo_00_RhythmRun", "Rhythm Run", "Move with the music. No scored input.", CreateRhythmRunEvents()),
            new("Demo_01_Step", "Step", "Alternate left and right controller button presses on the beat.", CreateStepEvents()),
            new("Demo_02_SideGrab", "Side Grab", "Press the authored hand while pointing toward the side grab.", CreateSideGrabEvents()),
            new("Demo_03_Slide", "Slide", "Press both hands together while pointing down.", CreateSlideEvents()),
            new("Demo_04_LongJump", "Long Jump", "Press both hands together while pointing up.", CreateLongJumpEvents()),
            new("Demo_05_Grapple", "Grapple", "Hold the authored hand upward through the full grapple segment.", CreateGrappleEvents())
        };

        [InitializeOnLoadMethod]
        static void RebuildIfRequested()
        {
            if (!File.Exists(RequestPath)
                && !File.Exists(RecordingRequestPath)
                && !File.Exists(TellingWorldRequestPath)
                && !File.Exists(TellingWorldFormalRequestPath)
                && !File.Exists(TellingWorldFormalEditBaseRequestPath))
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
                    BuildAllDemos();
                    AssetDatabase.DeleteAsset(RequestPath);
                }

                if (File.Exists(RecordingRequestPath))
                {
                    BuildRecordingDemo();
                    AssetDatabase.DeleteAsset(RecordingRequestPath);
                }

                if (File.Exists(TellingWorldRequestPath))
                {
                    BuildTellingWorldCalibrationDemo();
                    AssetDatabase.DeleteAsset(TellingWorldRequestPath);
                }

                if (File.Exists(TellingWorldFormalRequestPath))
                {
                    BuildTellingWorldFormalDemo();
                    AssetDatabase.DeleteAsset(TellingWorldFormalRequestPath);
                }

                if (File.Exists(TellingWorldFormalEditBaseRequestPath))
                {
                    BuildTellingWorldFormalEditBaseDemo();
                    AssetDatabase.DeleteAsset(TellingWorldFormalEditBaseRequestPath);
                }

                AssetDatabase.Refresh();
            };
        }

        [MenuItem("Rhythm Parkour/Rebuild Music VR Demo Scenes")]
        public static void BuildAllDemos()
        {
            EnsureFolder("Assets/Rhythm", "MusicVrDemos");
            EnsureFolder("Assets/Scenes", "MusicVrDemos");
            EnsureFolder("Assets/Settings", "Volumes");
            EnsureFolder("Assets/Settings/Volumes", "MusicVrDemos");

            var sourceTrack = AssetDatabase.LoadAssetAtPath<RhythmTrackConfig>(SourceTrackPath);
            if (sourceTrack == null)
            {
                Debug.LogError($"[RhythmParkour] Missing source track at {SourceTrackPath}.");
                return;
            }

            foreach (var demo in Demos)
            {
                var chart = SaveChart(demo);
                var track = SaveTrack(demo, sourceTrack, chart);
                BuildScene(demo, track);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[RhythmParkour] Rebuilt {Demos.Length} music VR demo scenes.");
        }

        [MenuItem("Rhythm Parkour/Rebuild Recording Demo Scene")]
        public static void BuildRecordingDemo()
        {
            EnsureFolder("Assets/Rhythm", "RecordingDemos");
            EnsureFolder("Assets/Scenes", "RecordingDemos");
            EnsureFolder("Assets/Settings", "Volumes");
            EnsureFolder("Assets/Settings/Volumes", "RecordingDemos");

            var sourceTrack = AssetDatabase.LoadAssetAtPath<RhythmTrackConfig>(SourceTrackPath);
            if (sourceTrack == null)
            {
                Debug.LogError($"[RhythmParkour] Missing source track at {SourceTrackPath}.");
                return;
            }

            var demo = CreateRecordingDemoDefinition();
            var chart = SaveChart(demo, RecordingRhythmFolder);
            var track = SaveTrack(demo, sourceTrack, chart, RecordingRhythmFolder);
            BuildScene(demo, track, RecordingSceneFolder, RecordingVolumeProfileFolder);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[RhythmParkour] Rebuilt recording demo scene.");
        }

        [MenuItem("Rhythm Parkour/Rebuild Telling World Calibration Demo")]
        public static void BuildTellingWorldCalibrationDemo()
        {
            EnsureFolder("Assets/Rhythm", "FormalDemos");
            EnsureFolder("Assets/Scenes", "FormalDemos");
            EnsureFolder("Assets/Settings", "Volumes");
            EnsureFolder("Assets/Settings/Volumes", "FormalDemos");

            var audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(TellingWorldAudioPath);
            if (audioClip == null)
            {
                Debug.LogError($"[RhythmParkour] Missing audio clip at {TellingWorldAudioPath}.");
                return;
            }

            var demo = CreateTellingWorldCalibrationDemoDefinition();
            var chart = SaveChart(demo, FormalRhythmFolder);
            var track = SaveTrack(
                demo,
                audioClip,
                TellingWorldInitialBpm,
                TellingWorldInitialFirstBeatOffsetSeconds,
                chart,
                FormalRhythmFolder);

            BuildScene(demo, track, FormalSceneFolder, FormalVolumeProfileFolder);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[RhythmParkour] Rebuilt Telling World calibration demo scene.");
        }

        [MenuItem("Rhythm Parkour/Rebuild Telling World Formal Demo")]
        public static void BuildTellingWorldFormalDemo()
        {
            EnsureFolder("Assets/Rhythm", "FormalDemos");
            EnsureFolder("Assets/Scenes", "FormalDemos");
            EnsureFolder("Assets/Settings", "Volumes");
            EnsureFolder("Assets/Settings/Volumes", "FormalDemos");

            var audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(TellingWorldAudioPath);
            if (audioClip == null)
            {
                Debug.LogError($"[RhythmParkour] Missing audio clip at {TellingWorldAudioPath}.");
                return;
            }

            var demo = CreateTellingWorldFormalDemoDefinition();
            var chart = SaveChart(demo, FormalRhythmFolder);
            var track = SaveTrack(
                demo,
                audioClip,
                TellingWorldInitialBpm,
                TellingWorldInitialFirstBeatOffsetSeconds,
                chart,
                FormalRhythmFolder);

            BuildScene(demo, track, FormalSceneFolder, FormalVolumeProfileFolder);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[RhythmParkour] Rebuilt Telling World formal demo scene.");
        }

        [MenuItem("Rhythm Parkour/Rebuild Telling World Formal Edit Base Demo")]
        public static void BuildTellingWorldFormalEditBaseDemo()
        {
            EnsureFolder("Assets/Rhythm", "FormalDemos");
            EnsureFolder("Assets/Scenes", "FormalDemos");
            EnsureFolder("Assets/Settings", "Volumes");
            EnsureFolder("Assets/Settings/Volumes", "FormalDemos");

            var audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(TellingWorldAudioPath);
            if (audioClip == null)
            {
                Debug.LogError($"[RhythmParkour] Missing audio clip at {TellingWorldAudioPath}.");
                return;
            }

            var demo = CreateTellingWorldFormalDemoDefinition();
            var chart = SaveChart(demo, FormalRhythmFolder);
            var track = SaveTrack(
                demo,
                audioClip,
                TellingWorldInitialBpm,
                TellingWorldInitialFirstBeatOffsetSeconds,
                chart,
                FormalRhythmFolder);

            BuildScene(
                demo,
                track,
                FormalSceneFolder,
                FormalVolumeProfileFolder,
                $"{FormalSceneFolder}/TellingWorldFormalDemo_EditBase.unity");
            FormalDemoEditBaseSceneUpdater.UpdateScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[RhythmParkour] Rebuilt Telling World formal edit-base demo scene.");
        }

        static RhythmActionChart SaveChart(DemoDefinition demo, string folder = RhythmFolder)
        {
            var path = $"{folder}/{demo.Id}_Chart.asset";
            var chart = AssetDatabase.LoadAssetAtPath<RhythmActionChart>(path);
            if (chart == null)
            {
                chart = ScriptableObject.CreateInstance<RhythmActionChart>();
                AssetDatabase.CreateAsset(chart, path);
            }

            chart.SetEvents(demo.Events);
            EditorUtility.SetDirty(chart);
            return chart;
        }

        static RhythmTrackConfig SaveTrack(
            DemoDefinition demo,
            RhythmTrackConfig sourceTrack,
            RhythmActionChart chart,
            string folder = RhythmFolder)
        {
            return SaveTrack(
                demo,
                sourceTrack.AudioClip,
                sourceTrack.BaseBpm,
                sourceTrack.FirstBeatOffsetSeconds,
                chart,
                folder);
        }

        static RhythmTrackConfig SaveTrack(
            DemoDefinition demo,
            AudioClip audioClip,
            float baseBpm,
            float firstBeatOffsetSeconds,
            RhythmActionChart chart,
            string folder = RhythmFolder)
        {
            var path = $"{folder}/{demo.Id}_Track.asset";
            var track = AssetDatabase.LoadAssetAtPath<RhythmTrackConfig>(path);
            if (track == null)
            {
                track = ScriptableObject.CreateInstance<RhythmTrackConfig>();
                AssetDatabase.CreateAsset(track, path);
            }

            track.Configure(audioClip, baseBpm, firstBeatOffsetSeconds, chart);
            EditorUtility.SetDirty(track);
            return track;
        }

        static void BuildScene(
            DemoDefinition demo,
            RhythmTrackConfig track,
            string sceneFolder = SceneFolder,
            string volumeProfileFolder = VolumeProfileFolder,
            string scenePathOverride = null)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ConfigureRenderSettings(demo.UseVisualSetDressing);

            var materials = RhythmParkourLevelTheme.CreateDefault();
            var playerRoot = CreatePlayerRig(demo, track, materials);
            var speedVolume = CreateSpeedVolume(demo, volumeProfileFolder);

            var levelOptions = new RhythmParkourLevelBuildOptions
            {
                UnitsPerBeat = UnitsPerBeat,
                AlignBeatStripesToEvents = demo.AlignBeatStripesToEvents,
                UseTrackPositionMap = true,
                GrappleExtraUnitsPerBeat = RhythmTrackPositionMapper.DefaultGrappleExtraUnitsPerBeat
            };
            var chartIssues = RhythmParkourLevelBuilder.ValidateChart(demo.Events);
            foreach (var issue in chartIssues)
                Debug.LogWarning($"[RhythmParkour] {demo.Id}: {issue}");

            var scenePath = string.IsNullOrEmpty(scenePathOverride) ? $"{sceneFolder}/{demo.Id}.unity" : scenePathOverride;
            var retrySceneName = Path.GetFileNameWithoutExtension(scenePath);

            var endBeat = RhythmParkourLevelBuilder.BuildStraightRunway(demo.Events, materials, levelOptions);
            RhythmParkourLevelBuilder.CreateActionCues(demo.Events, materials, UnitsPerBeat, levelOptions);
            var goalZ = RhythmTrackPositionMapper.GetZ(endBeat, demo.Events, UnitsPerBeat, levelOptions.GrappleExtraUnitsPerBeat) + 6f;
            var portalTrigger = RhythmParkourLevelBuilder.CreateGoalPortal(materials, goalZ);
            ConfigurePortalSuccessTrigger(portalTrigger, playerRoot);
            if (demo.UseVisualSetDressing)
                CreateFormalVisualSetDressing(materials, endBeat);

            CreateVrInputSystem(playerRoot, demo.Events.Count > 0, speedVolume);
            CreateRunResultPanel(playerRoot, portalTrigger, retrySceneName);

            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[RhythmParkour] Rebuilt {scenePath}.");
        }

        static void ConfigurePortalSuccessTrigger(RhythmPortalSuccessTrigger portalTrigger, PlayerRig rig)
        {
            if (portalTrigger == null || rig.Root == null)
                return;

            var serialized = new SerializedObject(portalTrigger);
            serialized.FindProperty("playerRoot").objectReferenceValue = rig.Root.transform;
            serialized.FindProperty("actionSource").objectReferenceValue = rig.Prototype;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void ConfigureRenderSettings(bool useVisualSetDressing)
        {
            RenderSettings.ambientLight = useVisualSetDressing
                ? new Color(0.18f, 0.22f, 0.34f)
                : new Color(0.1f, 0.09f, 0.16f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = useVisualSetDressing ? FogMode.ExponentialSquared : FogMode.Exponential;
            RenderSettings.fogColor = useVisualSetDressing
                ? new Color(0.018f, 0.035f, 0.085f)
                : new Color(0.035f, 0.015f, 0.08f);
            RenderSettings.fogDensity = useVisualSetDressing ? 0.0045f : 0.01f;

            var skybox = AssetDatabase.LoadAssetAtPath<Material>(useVisualSetDressing ? VisualSkyboxPath : SkyboxPath);
            if (skybox != null)
                RenderSettings.skybox = skybox;
        }

        static void CreateFormalVisualSetDressing(RhythmParkourLevelTheme materials, int endBeat)
        {
            var root = new GameObject("Formal Demo Visual Set Dressing");
            var endZ = endBeat * UnitsPerBeat + 18f;

            CreateCube("Distant Horizon Glow", root.transform, new Vector3(0f, 5.2f, endZ * 0.56f), new Vector3(26f, 0.08f, endZ), materials.GoalEnergy);
            CreateCube("Left Far Neon Rail", root.transform, new Vector3(-4.2f, 0.72f, endZ * 0.5f), new Vector3(0.08f, 0.16f, endZ), materials.Edge);
            CreateCube("Right Far Neon Rail", root.transform, new Vector3(4.2f, 0.72f, endZ * 0.5f), new Vector3(0.08f, 0.16f, endZ), materials.Edge);

            var cityBlockCount = Mathf.CeilToInt(endZ / 12f);
            for (var i = 0; i < cityBlockCount; i++)
            {
                var z = 10f + i * 12f;
                if (z > endZ)
                    break;

                var side = i % 2 == 0 ? -1f : 1f;
                var height = 5.5f + (i % 5) * 1.4f;
                var width = 1.6f + (i % 3) * 0.55f;
                var x = side * (8.5f + (i % 4) * 1.8f);
                var building = CreateCube(
                    $"Visual City Block {i:00}",
                    root.transform,
                    new Vector3(x, height * 0.5f - 0.4f, z),
                    new Vector3(width, height, 2.1f + (i % 4) * 0.45f),
                    materials.CitySilhouette);
                building.transform.rotation = Quaternion.Euler(0f, side * (6f + i % 4 * 3f), 0f);

                var windowMaterial = i % 3 == 0 ? materials.WindowCyan : i % 3 == 1 ? materials.WindowMagenta : materials.WindowRed;
                var innerFaceX = x - side * (width * 0.52f + 0.015f);
                for (var row = 0; row < 3; row++)
                {
                    CreateCube(
                        $"Visual Window Strip {i:00}-{row}",
                        root.transform,
                        new Vector3(innerFaceX, 1.4f + row * 1.45f, z - 0.72f + row * 0.18f),
                        new Vector3(0.035f, 0.12f, 1.1f + (row % 2) * 0.5f),
                        windowMaterial);
                }
            }

            var cloudSheetCount = Mathf.CeilToInt(endZ / 22f);
            for (var i = 0; i < cloudSheetCount; i++)
            {
                var cloud = CreateCube(
                    $"Upper Cloud Sheet {i:00}",
                    root.transform,
                    new Vector3(i % 2 == 0 ? -5.5f : 5.5f, 7.2f + (i % 3) * 0.9f, 18f + i * 22f),
                    new Vector3(8.5f, 0.04f, 5.2f),
                    materials.Cloud);
                cloud.transform.rotation = Quaternion.Euler(0f, i * 19f, 0f);
            }
        }

        static GameObject CreateCube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, true);
            cube.transform.position = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            return cube;
        }

        static PlayerRig CreatePlayerRig(DemoDefinition demo, RhythmTrackConfig track, RhythmParkourLevelTheme materials)
        {
            var root = new GameObject("[BuildingBlock] Camera Rig");
            root.transform.position = new Vector3(0f, 1f, -1.5f);

            var trackingSpace = new GameObject("TrackingSpace");
            trackingSpace.transform.SetParent(root.transform, false);

            CreateEyeAnchor("LeftEyeAnchor", trackingSpace.transform, false);
            var centerEye = CreateEyeAnchor("CenterEyeAnchor", trackingSpace.transform, true);
            CreateEyeAnchor("RightEyeAnchor", trackingSpace.transform, false);
            CreateAnchor("TrackerAnchor", trackingSpace.transform);
            CreateAnchor("LeftHandAnchor", trackingSpace.transform);
            CreateAnchor("RightHandAnchor", trackingSpace.transform);
            var leftController = CreateAnchor("LeftControllerAnchor", trackingSpace.transform);
            var rightController = CreateAnchor("RightControllerAnchor", trackingSpace.transform);

            root.AddComponent<OVRCameraRig>();
            var manager = root.AddComponent<OVRManager>();
            manager.trackingOriginType = OVRManager.TrackingOrigin.FloorLevel;

            var camera = centerEye.GetComponent<Camera>();
            camera.fieldOfView = 74f;
            camera.nearClipPlane = 0.03f;
            camera.clearFlags = CameraClearFlags.Skybox;

            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.transform.rotation = Quaternion.Euler(46f, -28f, 0f);

            var cue = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cue.name = "Hidden Beat Position";
            cue.transform.localScale = Vector3.one * 0.2f;
            cue.GetComponent<Renderer>().sharedMaterial = materials.Cue;
            cue.hideFlags = HideFlags.NotEditable;

            var prototypeObject = new GameObject("Music VR Rhythm Action Prototype");
            var audioSource = prototypeObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.dopplerLevel = 0f;

            var distortionFilter = prototypeObject.AddComponent<AudioDistortionFilter>();
            distortionFilter.distortionLevel = 0f;

            var prototype = prototypeObject.AddComponent<VRRhythmActionPrototype>();
            var serialized = new SerializedObject(prototype);
            serialized.FindProperty("track").objectReferenceValue = track;
            serialized.FindProperty("useKeyboardFallback").boolValue = demo.Events.Count > 0;
            serialized.FindProperty("hitWindowBeats").floatValue = demo.HitWindowBeats;
            serialized.FindProperty("keyboardHitKey").intValue = (int)KeyCode.Space;
            serialized.FindProperty("keyboardStartKey").intValue = (int)KeyCode.Space;
            serialized.FindProperty("cueObject").objectReferenceValue = cue.transform;
            serialized.FindProperty("unitsPerBeat").floatValue = UnitsPerBeat;
            serialized.FindProperty("useTrackPositionMap").boolValue = true;
            serialized.FindProperty("grappleExtraUnitsPerBeat").floatValue = RhythmTrackPositionMapper.DefaultGrappleExtraUnitsPerBeat;
            serialized.FindProperty("cameraToFollow").objectReferenceValue = root.transform;
            serialized.FindProperty("cameraLeadUnits").floatValue = 0f;
            serialized.FindProperty("minimumCameraZ").floatValue = -1.5f;
            serialized.FindProperty("musicDistortionFilter").objectReferenceValue = distortionFilter;
            serialized.FindProperty("distortionNoiseVolume").floatValue = 0.045f;
            serialized.FindProperty("maxMusicDistortionLevel").floatValue = 0.14f;
            serialized.FindProperty("dropoutPulseRate").floatValue = 7f;
            serialized.FindProperty("dropoutVolumeFloor").floatValue = 0.78f;
            serialized.FindProperty("distortionSmoothingSeconds").floatValue = 0.45f;
            serialized.FindProperty("enableMissTempoPenalty").boolValue = demo.EnableMissTempoPenalty;
            serialized.FindProperty("successSoundVolume").floatValue = 0.46f;
            serialized.FindProperty("failureSoundVolume").floatValue = 0.56f;
            serialized.FindProperty("tailSilenceSeconds").floatValue = demo.UseVisualSetDressing ? 10f : 2f;
            serialized.FindProperty("enableRuntimeOffsetAdjustment").boolValue = demo.EnableRuntimeOffsetAdjustment;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            CreateHudText(demo);
            return new PlayerRig(root, trackingSpace, trackingSpace, centerEye, leftController, rightController, prototype);
        }

        static GameObject CreateEyeAnchor(string name, Transform parent, bool mainCamera)
        {
            var anchor = CreateAnchor(name, parent);
            if (mainCamera)
                anchor.tag = "MainCamera";

            var camera = anchor.AddComponent<Camera>();
            camera.enabled = mainCamera;
            camera.nearClipPlane = 0.03f;
            camera.clearFlags = CameraClearFlags.Skybox;
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

        static Volume CreateSpeedVolume(DemoDefinition demo, string volumeProfileFolder)
        {
            var profile = SaveSpeedVolumeProfile(demo, volumeProfileFolder);
            var volumeObject = new GameObject("Global Speed Feel Volume");
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.weight = 1f;
            volume.sharedProfile = profile;
            return volume;
        }

        static VolumeProfile SaveSpeedVolumeProfile(DemoDefinition demo, string volumeProfileFolder)
        {
            var path = $"{volumeProfileFolder}/{demo.Id}_SpeedFeelProfile.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.name = $"{demo.Id} Speed Feel";
                AssetDatabase.CreateAsset(profile, path);
            }

            var lens = GetOrAddVolumeComponent<LensDistortion>(profile);
            lens.intensity.overrideState = true;
            lens.scale.overrideState = true;
            lens.xMultiplier.overrideState = true;
            lens.yMultiplier.overrideState = true;
            lens.intensity.value = -0.025f;
            lens.scale.value = 1.02f;
            lens.xMultiplier.value = 1f;
            lens.yMultiplier.value = 1f;

            var chromatic = GetOrAddVolumeComponent<ChromaticAberration>(profile);
            chromatic.intensity.overrideState = true;
            chromatic.intensity.value = 0.035f;

            var vignette = GetOrAddVolumeComponent<Vignette>(profile);
            vignette.intensity.overrideState = true;
            vignette.smoothness.overrideState = true;
            vignette.color.overrideState = true;
            vignette.intensity.value = 0.18f;
            vignette.smoothness.value = 0.58f;
            vignette.color.value = new Color(0.02f, 0.01f, 0.05f);

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

        static void CreateVrInputSystem(PlayerRig rig, bool assignToRhythmPrototype, Volume speedVolume)
        {
            var inputObject = new GameObject("VR Input System");
            inputObject.transform.position = rig.Root.transform.position;

            var reader = inputObject.AddComponent<VRInputReader>();
            var serializedReader = new SerializedObject(reader);
            serializedReader.FindProperty("directionReference").objectReferenceValue = rig.Camera.transform;
            serializedReader.FindProperty("playerTurnReference").objectReferenceValue = rig.Camera.transform;
            serializedReader.ApplyModifiedPropertiesWithoutUndo();

            var inputEvents = inputObject.AddComponent<VRParkourInputEvents>();
            var serializedEvents = new SerializedObject(inputEvents);
            serializedEvents.FindProperty("inputReader").objectReferenceValue = reader;
            serializedEvents.ApplyModifiedPropertiesWithoutUndo();

            var overlay = inputObject.AddComponent<VRInputDebugOverlay>();
            var serializedOverlay = new SerializedObject(overlay);
            serializedOverlay.FindProperty("inputReader").objectReferenceValue = reader;
            serializedOverlay.ApplyModifiedPropertiesWithoutUndo();

            var poseBinder = inputObject.AddComponent<VRControllerPoseBinder>();
            var serializedPoseBinder = new SerializedObject(poseBinder);
            poseBinder.enabled = true;
            serializedPoseBinder.FindProperty("trackingSpace").objectReferenceValue = rig.TrackingSpace.transform;
            serializedPoseBinder.FindProperty("leftController").objectReferenceValue = rig.LeftController.transform;
            serializedPoseBinder.FindProperty("rightController").objectReferenceValue = rig.RightController.transform;
            serializedPoseBinder.ApplyModifiedPropertiesWithoutUndo();

            var debugVisuals = inputObject.AddComponent<VRControllerDebugVisuals>();
            var serializedVisuals = new SerializedObject(debugVisuals);
            serializedVisuals.FindProperty("trackingSpace").objectReferenceValue = rig.TrackingSpace.transform;
            serializedVisuals.ApplyModifiedPropertiesWithoutUndo();

            var feedback = inputObject.AddComponent<FirstPersonActionFeedbackDriver>();
            var feedbackSerialized = new SerializedObject(feedback);
            feedbackSerialized.FindProperty("actionSource").objectReferenceValue = rig.Prototype;
            feedbackSerialized.FindProperty("debugInputSource").objectReferenceValue = inputEvents;
            feedbackSerialized.FindProperty("motionRoot").objectReferenceValue = rig.CameraOffset.transform;
            feedbackSerialized.FindProperty("locomotionRoot").objectReferenceValue = rig.Root.transform;
            feedbackSerialized.FindProperty("playRecognizedInputEvents").boolValue = false;
            feedbackSerialized.FindProperty("baseActionDurationSeconds").floatValue = 0.42f;
            feedbackSerialized.FindProperty("grappleSecondsPerBeat").floatValue = 0.25f;
            feedbackSerialized.FindProperty("stepBob").floatValue = 0.22f;
            feedbackSerialized.FindProperty("stepForward").floatValue = 0.34f;
            feedbackSerialized.FindProperty("stepSideSway").floatValue = 0.045f;
            feedbackSerialized.FindProperty("stepLandingDip").floatValue = 0.08f;
            feedbackSerialized.FindProperty("sideGrabLift").floatValue = 0.34f;
            feedbackSerialized.FindProperty("sideGrabSway").floatValue = 0.22f;
            feedbackSerialized.FindProperty("sideGrabForward").floatValue = 0.34f;
            feedbackSerialized.FindProperty("sideGrabLandingDip").floatValue = 0.08f;
            feedbackSerialized.FindProperty("sideGrabYawDegrees").floatValue = 5f;
            feedbackSerialized.FindProperty("slideLowering").floatValue = 0.42f;
            feedbackSerialized.FindProperty("slideForward").floatValue = 0.5f;
            feedbackSerialized.FindProperty("longJumpLift").floatValue = 0.9f;
            feedbackSerialized.FindProperty("longJumpForward").floatValue = 1.35f;
            feedbackSerialized.FindProperty("longJumpLandingDip").floatValue = 0.16f;
            feedbackSerialized.FindProperty("gravityFallPower").floatValue = 1.55f;
            feedbackSerialized.FindProperty("grappleForward").floatValue = 0.48f;
            feedbackSerialized.FindProperty("grappleLift").floatValue = 0.085f;
            feedbackSerialized.FindProperty("grapplePendulumBaseRadius").floatValue = 4.2f;
            feedbackSerialized.FindProperty("grapplePendulumRadiusPerBeat").floatValue = 0.38f;
            feedbackSerialized.FindProperty("grapplePendulumAngleDegrees").floatValue = 56f;
            feedbackSerialized.FindProperty("grapplePendulumRollDegrees").floatValue = 10f;
            feedbackSerialized.FindProperty("grappleHookForward").floatValue = 6.6f;
            feedbackSerialized.FindProperty("grappleHookHeight").floatValue = 6.2f;
            feedbackSerialized.FindProperty("grappleHookSideOffset").floatValue = 0.72f;
            feedbackSerialized.FindProperty("grappleHookAnchorVerticalOffset").floatValue = 1.25f;
            feedbackSerialized.FindProperty("grappleBodyVerticalOffset").floatValue = 1.05f;
            feedbackSerialized.FindProperty("grappleRopeRadius").floatValue = 0.075f;
            feedbackSerialized.FindProperty("grappleClawScale").vector3Value = new Vector3(0.64f, 0.32f, 0.64f);
            feedbackSerialized.FindProperty("stepRollDegrees").floatValue = 3.8f;
            feedbackSerialized.FindProperty("stepYawDegrees").floatValue = 1.6f;
            feedbackSerialized.FindProperty("slideLookUpDegrees").floatValue = 10f;
            feedbackSerialized.FindProperty("slideRollDegrees").floatValue = 2.2f;
            feedbackSerialized.FindProperty("longJumpTakeoffLookUpDegrees").floatValue = 10f;
            feedbackSerialized.FindProperty("longJumpAirLookDownDegrees").floatValue = 6.5f;
            feedbackSerialized.FindProperty("longJumpLandingRollDegrees").floatValue = 3.2f;
            feedbackSerialized.FindProperty("grappleLookUpDegrees").floatValue = 6f;
            feedbackSerialized.FindProperty("moveRootOnSuccessfulActions").boolValue = false;
            feedbackSerialized.FindProperty("moveRootOnMisses").boolValue = false;
            feedbackSerialized.FindProperty("stepTravelDistance").floatValue = 0.85f;
            feedbackSerialized.FindProperty("stepAccelerationExponent").floatValue = 1.75f;
            feedbackSerialized.FindProperty("slideTravelDistance").floatValue = 1.25f;
            feedbackSerialized.FindProperty("slideDropStage").floatValue = 0.2f;
            feedbackSerialized.FindProperty("slideHoldStage").floatValue = 0.74f;
            feedbackSerialized.FindProperty("longJumpTravelDistance").floatValue = 1.65f;
            feedbackSerialized.FindProperty("longJumpTakeoffStage").floatValue = 0.2f;
            feedbackSerialized.FindProperty("longJumpFloatStage").floatValue = 0.85f;
            feedbackSerialized.FindProperty("grappleTravelDistance").floatValue = 1.9f;
            feedbackSerialized.FindProperty("missShakeAmount").floatValue = 0.045f;
            feedbackSerialized.FindProperty("missShakeFrequency").floatValue = 42f;
            feedbackSerialized.FindProperty("enableKeyboardDebug").boolValue = false;
            feedbackSerialized.FindProperty("debugStepKey").intValue = (int)KeyCode.Space;
            feedbackSerialized.FindProperty("debugSideGrabKey").intValue = (int)KeyCode.Space;
            feedbackSerialized.FindProperty("debugSlideKey").intValue = (int)KeyCode.Space;
            feedbackSerialized.FindProperty("debugLongJumpKey").intValue = (int)KeyCode.Space;
            feedbackSerialized.FindProperty("debugGrappleKey").intValue = (int)KeyCode.Space;
            feedbackSerialized.ApplyModifiedPropertiesWithoutUndo();

            var speedFeel = inputObject.AddComponent<VRSpeedFeelDriver>();
            var speedSerialized = new SerializedObject(speedFeel);
            speedSerialized.FindProperty("actionSource").objectReferenceValue = rig.Prototype;
            speedSerialized.FindProperty("speedVolume").objectReferenceValue = speedVolume;
            speedSerialized.FindProperty("targetCamera").objectReferenceValue = rig.Camera.GetComponent<Camera>();
            speedSerialized.FindProperty("failureVignette").floatValue = 0.68f;
            speedSerialized.FindProperty("failurePulseDecaySpeed").floatValue = 3.8f;
            speedSerialized.ApplyModifiedPropertiesWithoutUndo();

            var serializedPrototype = new SerializedObject(rig.Prototype);
            serializedPrototype.FindProperty("inputReader").objectReferenceValue = assignToRhythmPrototype ? reader : null;
            serializedPrototype.ApplyModifiedPropertiesWithoutUndo();
        }

        static void CreateHudText(DemoDefinition demo)
        {
            var textObject = new GameObject("Demo Floating Label");
            textObject.transform.position = new Vector3(-2.2f, 2.35f, 4f);
            textObject.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            textObject.transform.localScale = Vector3.one * 0.09f;

            var text = textObject.AddComponent<TextMesh>();
            text.text = $"{demo.DisplayName}\n{demo.Instruction}";
            text.anchor = TextAnchor.MiddleLeft;
            text.alignment = TextAlignment.Left;
            text.fontSize = 72;
            text.characterSize = 0.22f;
            text.color = new Color(0.82f, 0.96f, 1f, 1f);
        }

        static void CreateRunResultPanel(PlayerRig rig, RhythmPortalSuccessTrigger portalTrigger, string retrySceneName)
        {
            EnsureFolder("Assets/Materials", "UI");

            var normal = CreateUiMaterial("M_Menu_Button_Normal", new Color(0.08f, 0.08f, 0.18f, 0.86f), true);
            var hover = CreateUiMaterial("M_Menu_Button_Hover", new Color(0.1f, 0.55f, 1f, 0.84f), true);
            var selected = CreateUiMaterial("M_Menu_Button_Selected", new Color(1f, 0.08f, 0.42f, 0.9f), true);
            var panelMaterial = CreateUiMaterial("M_Result_Panel_Back", new Color(0.018f, 0.014f, 0.045f, 1f), false);
            var rayMaterial = CreateUiMaterial("M_Menu_Ray", new Color(1f, 1f, 1f, 0.85f), true);

            var system = new GameObject("VR Run Result System");
            var panelRoot = new GameObject("VR Run Result Panel");
            panelRoot.transform.SetParent(rig.Camera.transform, false);
            panelRoot.transform.localPosition = new Vector3(0f, -0.05f, 3.05f);
            panelRoot.transform.localRotation = Quaternion.identity;

            var back = GameObject.CreatePrimitive(PrimitiveType.Cube);
            back.name = "Result Back Plate";
            back.transform.SetParent(panelRoot.transform, false);
            back.transform.localPosition = new Vector3(0f, 0f, 0.1f);
            back.transform.localScale = new Vector3(4.3f, 3.05f, 0.1f);
            back.GetComponent<Renderer>().sharedMaterial = panelMaterial;

            var title = CreatePanelText(panelRoot.transform, "Result Title", "RUN COMPLETE", new Vector3(0f, 1.03f, -0.08f), 0.13f, Color.white);
            var stats = CreatePanelText(panelRoot.transform, "Result Stats", "Waiting for run data.", new Vector3(0f, 0.44f, -0.08f), 0.043f, new Color(0.88f, 0.98f, 1f, 1f));
            var hint = CreatePanelText(panelRoot.transform, "Result Hint", "", new Vector3(0f, -1.28f, -0.08f), 0.034f, new Color(0.62f, 0.9f, 1f, 0.95f));

            var retry = CreatePanelButton(panelRoot.transform, "Retry", "Restart this demo.", retrySceneName, new Vector3(-1f, -0.94f, -0.08f), normal, hover, selected);
            var mainMenu = CreatePanelButton(panelRoot.transform, "Main Menu", "Return to the main menu.", GameFlowMainMenuSceneName, new Vector3(1f, -0.94f, -0.08f), normal, hover, selected);

            var leftRay = CreateResultRay("Result Left Menu Ray", panelRoot.transform, rayMaterial, new Color(0.25f, 0.85f, 1f, 0.9f));
            var rightRay = CreateResultRay("Result Right Menu Ray", panelRoot.transform, rayMaterial, new Color(1f, 0.18f, 0.68f, 0.9f));

            var menuController = system.AddComponent<VRMenuController>();
            var menuSerialized = new SerializedObject(menuController);
            menuSerialized.FindProperty("inputReader").objectReferenceValue = UnityEngine.Object.FindObjectOfType<VRInputReader>(true);
            menuSerialized.FindProperty("leftRayOrigin").objectReferenceValue = rig.LeftController.transform;
            menuSerialized.FindProperty("rightRayOrigin").objectReferenceValue = rig.RightController.transform;
            menuSerialized.FindProperty("menuCamera").objectReferenceValue = rig.Camera.GetComponent<Camera>();
            menuSerialized.FindProperty("leftRay").objectReferenceValue = leftRay;
            menuSerialized.FindProperty("rightRay").objectReferenceValue = rightRay;
            menuSerialized.FindProperty("rayDistance").floatValue = 8f;
            menuSerialized.FindProperty("backSceneName").stringValue = GameFlowMainMenuSceneName;
            SetObjectArray(menuSerialized.FindProperty("buttons"), new UnityEngine.Object[] { retry, mainMenu });
            menuSerialized.ApplyModifiedPropertiesWithoutUndo();
            menuController.enabled = false;

            var resultPanel = system.AddComponent<VRRunResultPanel>();
            var resultSerialized = new SerializedObject(resultPanel);
            resultSerialized.FindProperty("actionSource").objectReferenceValue = rig.Prototype;
            resultSerialized.FindProperty("successTrigger").objectReferenceValue = portalTrigger;
            resultSerialized.FindProperty("panelRoot").objectReferenceValue = panelRoot;
            resultSerialized.FindProperty("menuController").objectReferenceValue = menuController;
            resultSerialized.FindProperty("titleText").objectReferenceValue = title;
            resultSerialized.FindProperty("statsText").objectReferenceValue = stats;
            resultSerialized.FindProperty("hintText").objectReferenceValue = hint;
            resultSerialized.ApplyModifiedPropertiesWithoutUndo();

            panelRoot.SetActive(false);
        }

        static VRMenuButton CreatePanelButton(Transform parent, string labelText, string detailText, string sceneName, Vector3 localPosition, Material normal, Material hover, Material selected)
        {
            var root = new GameObject($"Result Button {labelText}");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPosition;

            var plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.name = "Plate";
            plate.transform.SetParent(root.transform, false);
            plate.transform.localPosition = Vector3.zero;
            plate.transform.localScale = new Vector3(1.45f, 0.42f, 0.08f);
            var renderer = plate.GetComponent<Renderer>();
            renderer.sharedMaterial = normal;

            var label = CreatePanelText(root.transform, "Label", labelText, new Vector3(0f, 0.07f, -0.075f), 0.064f, Color.white);
            var detail = CreatePanelText(root.transform, "Detail", detailText, new Vector3(0f, -0.11f, -0.075f), 0.03f, new Color(0.78f, 0.92f, 1f, 0.94f));

            var button = root.AddComponent<VRMenuButton>();
            button.Configure(VRMenuButtonAction.LoadScene, sceneName, label, detail, new[] { renderer }, normal, hover, selected);
            return button;
        }

        static TextMesh CreatePanelText(Transform parent, string name, string textValue, Vector3 localPosition, float scale, Color color)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            textObject.transform.localPosition = localPosition;
            textObject.transform.localScale = Vector3.one * scale;

            var text = textObject.AddComponent<TextMesh>();
            text.text = textValue;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 96;
            text.characterSize = 0.2f;
            text.color = color;
            return text;
        }

        static LineRenderer CreateResultRay(string name, Transform parent, Material material, Color color)
        {
            var rayObject = new GameObject(name);
            rayObject.transform.SetParent(parent, false);
            var ray = rayObject.AddComponent<LineRenderer>();
            ray.sharedMaterial = material;
            ray.useWorldSpace = true;
            ray.positionCount = 2;
            ray.startWidth = 0.018f;
            ray.endWidth = 0.004f;
            ray.startColor = color;
            ray.endColor = new Color(color.r, color.g, color.b, 0.12f);
            return ray;
        }

        static Material CreateUiMaterial(string name, Color color, bool transparent)
        {
            var path = $"{MenuMaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null)
                    shader = Shader.Find("Standard");

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            else
                material.color = color;

            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", color * 1.6f);

            if (transparent)
            {
                material.SetOverrideTag("RenderType", "Transparent");
                material.renderQueue = (int)RenderQueue.Transparent;
                if (material.HasProperty("_Surface"))
                    material.SetFloat("_Surface", 1f);
                if (material.HasProperty("_Blend"))
                    material.SetFloat("_Blend", 0f);
                if (material.HasProperty("_AlphaClip"))
                    material.SetFloat("_AlphaClip", 0f);
            }
            else
            {
                material.SetOverrideTag("RenderType", "Opaque");
                material.renderQueue = (int)RenderQueue.Geometry;
                if (material.HasProperty("_Surface"))
                    material.SetFloat("_Surface", 0f);
                if (material.HasProperty("_Blend"))
                    material.SetFloat("_Blend", 0f);
                if (material.HasProperty("_AlphaClip"))
                    material.SetFloat("_AlphaClip", 0f);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        static void SetObjectArray(SerializedProperty property, UnityEngine.Object[] values)
        {
            property.arraySize = values != null ? values.Length : 0;
            for (var i = 0; i < property.arraySize; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        static void EnsureFolder(string parent, string child)
        {
            var path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }

        static IReadOnlyList<RhythmActionEvent> CreateRhythmRunEvents()
        {
            return new List<RhythmActionEvent>();
        }

        static IReadOnlyList<RhythmActionEvent> CreateStepEvents()
        {
            return new List<RhythmActionEvent>
            {
                new(2f, 1f, RhythmActionType.Step, RhythmHand.Left, RhythmDirection.None),
                new(3f, 1f, RhythmActionType.Step, RhythmHand.Right, RhythmDirection.None),
                new(4f, 1f, RhythmActionType.Step, RhythmHand.Left, RhythmDirection.None),
                new(5f, 1f, RhythmActionType.Step, RhythmHand.Right, RhythmDirection.None),
                new(6f, 1f, RhythmActionType.Step, RhythmHand.Left, RhythmDirection.None),
                new(7f, 1f, RhythmActionType.Step, RhythmHand.Right, RhythmDirection.None)
            };
        }

        static IReadOnlyList<RhythmActionEvent> CreateSideGrabEvents()
        {
            return new List<RhythmActionEvent>
            {
                new(2f, 1f, RhythmActionType.SideGrab, RhythmHand.Left, RhythmDirection.Left),
                new(4f, 1f, RhythmActionType.SideGrab, RhythmHand.Right, RhythmDirection.Right),
                new(6f, 1f, RhythmActionType.SideGrab, RhythmHand.Left, RhythmDirection.Left),
                new(8f, 1f, RhythmActionType.SideGrab, RhythmHand.Right, RhythmDirection.Right)
            };
        }

        static IReadOnlyList<RhythmActionEvent> CreateSlideEvents()
        {
            return new List<RhythmActionEvent>
            {
                new(2f, 1f, RhythmActionType.Slide, RhythmHand.Both, RhythmDirection.Down),
                new(4f, 1f, RhythmActionType.Slide, RhythmHand.Both, RhythmDirection.Down),
                new(6f, 1f, RhythmActionType.Slide, RhythmHand.Both, RhythmDirection.Down),
                new(8f, 1f, RhythmActionType.Slide, RhythmHand.Both, RhythmDirection.Down)
            };
        }

        static IReadOnlyList<RhythmActionEvent> CreateLongJumpEvents()
        {
            return new List<RhythmActionEvent>
            {
                new(2f, 1f, RhythmActionType.LongJump, RhythmHand.Both, RhythmDirection.Up),
                new(4f, 1f, RhythmActionType.LongJump, RhythmHand.Both, RhythmDirection.Up),
                new(6f, 1f, RhythmActionType.LongJump, RhythmHand.Both, RhythmDirection.Up),
                new(8f, 1f, RhythmActionType.LongJump, RhythmHand.Both, RhythmDirection.Up)
            };
        }

        static IReadOnlyList<RhythmActionEvent> CreateGrappleEvents()
        {
            return new List<RhythmActionEvent>
            {
                new(2f, 4f, RhythmActionType.Grapple, RhythmHand.Right, RhythmDirection.Up),
                new(8f, 4f, RhythmActionType.Grapple, RhythmHand.Left, RhythmDirection.Up)
            };
        }

        static DemoDefinition CreateRecordingDemoDefinition()
        {
            return new DemoDefinition(
                "RhythmParkourRecordingDemo",
                "Rhythm Parkour Recording Demo",
                "Mixed rhythm parkour flow for recording. Press Space or a controller button to start.",
                CreateRecordingDemoEvents(),
                0.5f);
        }

        static IReadOnlyList<RhythmActionEvent> CreateRecordingDemoEvents()
        {
            return new List<RhythmActionEvent>
            {
                new(2f, 1f, RhythmActionType.Step, RhythmHand.Left, RhythmDirection.None),
                new(3f, 1f, RhythmActionType.Step, RhythmHand.Right, RhythmDirection.None),
                new(4f, 1f, RhythmActionType.SideGrab, RhythmHand.Left, RhythmDirection.Left),
                new(6f, 1f, RhythmActionType.SideGrab, RhythmHand.Right, RhythmDirection.Right),
                new(8f, 1f, RhythmActionType.Slide, RhythmHand.Both, RhythmDirection.Down),
                new(10f, 1f, RhythmActionType.LongJump, RhythmHand.Both, RhythmDirection.Up),
                new(12f, 1f, RhythmActionType.Step, RhythmHand.Left, RhythmDirection.None),
                new(13f, 1f, RhythmActionType.Step, RhythmHand.Right, RhythmDirection.None),
                new(16f, 4f, RhythmActionType.Grapple, RhythmHand.Right, RhythmDirection.Up),
                new(22f, 1f, RhythmActionType.Slide, RhythmHand.Both, RhythmDirection.Down),
                new(24f, 1f, RhythmActionType.LongJump, RhythmHand.Both, RhythmDirection.Up),
                new(28f, 4f, RhythmActionType.Grapple, RhythmHand.Left, RhythmDirection.Up)
            };
        }

        static DemoDefinition CreateTellingWorldCalibrationDemoDefinition()
        {
            return new DemoDefinition(
                "TellingWorldCalibrationDemo",
                "Telling World Calibration Demo",
                "Step-only calibration chart for TellingWorld.mp3. Press Space or a controller button to start.",
                CreateTellingWorldCalibrationEvents(),
                0.5f,
                enableMissTempoPenalty: false,
                enableRuntimeOffsetAdjustment: true,
                alignBeatStripesToEvents: true);
        }

        static DemoDefinition CreateTellingWorldFormalDemoDefinition()
        {
            return new DemoDefinition(
                "TellingWorldFormalDemo",
                "Telling World Formal Demo",
                "Mixed rhythm parkour level generated from reusable components. Press Space or a controller button to start.",
                LoadTellingWorldFormalEvents(),
                0.5f,
                enableMissTempoPenalty: false,
                enableRuntimeOffsetAdjustment: true,
                alignBeatStripesToEvents: true,
                useVisualSetDressing: true);
        }

        static IReadOnlyList<RhythmActionEvent> LoadTellingWorldFormalEvents()
        {
            if (!File.Exists(TellingWorldFormalChartTablePath))
                return CreateTellingWorldFormalEvents();

            var events = new List<RhythmActionEvent>();
            var lines = File.ReadAllLines(TellingWorldFormalChartTablePath);
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#", StringComparison.Ordinal))
                    continue;

                var cells = line.Split('\t');
                if (cells.Length > 0 && cells[0].Trim().Equals("beat", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (TryParseTellingWorldFormalEvent(cells, i + 1, out var evt))
                    events.Add(evt);
            }

            if (events.Count == 0)
            {
                Debug.LogWarning($"[RhythmParkour] {TellingWorldFormalChartTablePath} did not contain valid chart rows. Falling back to built-in formal events.");
                return CreateTellingWorldFormalEvents();
            }

            events.Sort((left, right) => left.Beat.CompareTo(right.Beat));
            return events;
        }

        static bool TryParseTellingWorldFormalEvent(string[] cells, int lineNumber, out RhythmActionEvent evt)
        {
            evt = null;
            if (cells.Length < 3)
            {
                Debug.LogWarning($"[RhythmParkour] {TellingWorldFormalChartTablePath}:{lineNumber} needs at least beat, durationBeats, actionType.");
                return false;
            }

            if (!float.TryParse(cells[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var beat))
            {
                Debug.LogWarning($"[RhythmParkour] {TellingWorldFormalChartTablePath}:{lineNumber} has invalid beat '{cells[0]}'.");
                return false;
            }

            if (!float.TryParse(cells[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var durationBeats))
            {
                Debug.LogWarning($"[RhythmParkour] {TellingWorldFormalChartTablePath}:{lineNumber} has invalid duration '{cells[1]}'.");
                return false;
            }

            if (!Enum.TryParse(cells[2].Trim(), true, out RhythmActionType actionType))
            {
                Debug.LogWarning($"[RhythmParkour] {TellingWorldFormalChartTablePath}:{lineNumber} has invalid actionType '{cells[2]}'.");
                return false;
            }

            var hand = GetDefaultHand(actionType);
            if (cells.Length > 3 && !string.IsNullOrWhiteSpace(cells[3]))
            {
                if (!Enum.TryParse(cells[3].Trim(), true, out hand))
                {
                    Debug.LogWarning($"[RhythmParkour] {TellingWorldFormalChartTablePath}:{lineNumber} has invalid hand '{cells[3]}'.");
                    return false;
                }
            }

            var direction = GetDefaultDirection(actionType, hand);
            if (cells.Length > 4 && !string.IsNullOrWhiteSpace(cells[4]))
            {
                if (!Enum.TryParse(cells[4].Trim(), true, out direction))
                {
                    Debug.LogWarning($"[RhythmParkour] {TellingWorldFormalChartTablePath}:{lineNumber} has invalid direction '{cells[4]}'.");
                    return false;
                }
            }

            evt = new RhythmActionEvent(beat, durationBeats, actionType, hand, direction);
            return true;
        }

        static RhythmHand GetDefaultHand(RhythmActionType actionType)
        {
            switch (actionType)
            {
                case RhythmActionType.Slide:
                case RhythmActionType.LongJump:
                    return RhythmHand.Both;
                case RhythmActionType.Step:
                case RhythmActionType.SideGrab:
                case RhythmActionType.Grapple:
                default:
                    return RhythmHand.Right;
            }
        }

        static RhythmDirection GetDefaultDirection(RhythmActionType actionType, RhythmHand hand)
        {
            switch (actionType)
            {
                case RhythmActionType.SideGrab:
                    return hand == RhythmHand.Left ? RhythmDirection.Left : RhythmDirection.Right;
                case RhythmActionType.Slide:
                    return RhythmDirection.Down;
                case RhythmActionType.LongJump:
                case RhythmActionType.Grapple:
                    return RhythmDirection.Up;
                case RhythmActionType.Step:
                default:
                    return RhythmDirection.None;
            }
        }

        static IReadOnlyList<RhythmActionEvent> CreateTellingWorldCalibrationEvents()
        {
            return new List<RhythmActionEvent>
            {
                new(8f, 1f, RhythmActionType.Step, RhythmHand.Left, RhythmDirection.None),
                new(9f, 1f, RhythmActionType.Step, RhythmHand.Right, RhythmDirection.None),
                new(10f, 1f, RhythmActionType.Step, RhythmHand.Left, RhythmDirection.None),
                new(11f, 1f, RhythmActionType.Step, RhythmHand.Right, RhythmDirection.None),
                new(12f, 1f, RhythmActionType.Step, RhythmHand.Left, RhythmDirection.None),
                new(13f, 1f, RhythmActionType.Step, RhythmHand.Right, RhythmDirection.None),
                new(14f, 1f, RhythmActionType.Step, RhythmHand.Left, RhythmDirection.None),
                new(15f, 1f, RhythmActionType.Step, RhythmHand.Right, RhythmDirection.None),
                new(16f, 1f, RhythmActionType.Step, RhythmHand.Left, RhythmDirection.None),
                new(17f, 1f, RhythmActionType.Step, RhythmHand.Right, RhythmDirection.None),
                new(18f, 1f, RhythmActionType.Step, RhythmHand.Left, RhythmDirection.None),
                new(19f, 1f, RhythmActionType.Step, RhythmHand.Right, RhythmDirection.None),
                new(20f, 1f, RhythmActionType.Step, RhythmHand.Left, RhythmDirection.None),
                new(21f, 1f, RhythmActionType.Step, RhythmHand.Right, RhythmDirection.None),
                new(22f, 1f, RhythmActionType.Step, RhythmHand.Left, RhythmDirection.None),
                new(23f, 1f, RhythmActionType.Step, RhythmHand.Right, RhythmDirection.None),
                new(24f, 1f, RhythmActionType.Step, RhythmHand.Left, RhythmDirection.None),
                new(25f, 1f, RhythmActionType.Step, RhythmHand.Right, RhythmDirection.None),
                new(26f, 1f, RhythmActionType.Step, RhythmHand.Left, RhythmDirection.None),
                new(27f, 1f, RhythmActionType.Step, RhythmHand.Right, RhythmDirection.None),
                new(28f, 1f, RhythmActionType.Step, RhythmHand.Left, RhythmDirection.None),
                new(29f, 1f, RhythmActionType.Step, RhythmHand.Right, RhythmDirection.None),
                new(30f, 1f, RhythmActionType.Step, RhythmHand.Left, RhythmDirection.None),
                new(31f, 1f, RhythmActionType.Step, RhythmHand.Right, RhythmDirection.None)
            };
        }

        static IReadOnlyList<RhythmActionEvent> CreateTellingWorldFormalEvents()
        {
            return new List<RhythmActionEvent>
            {
                new(8f, 1f, RhythmActionType.Step, RhythmHand.Left, RhythmDirection.None),
                new(9f, 1f, RhythmActionType.Step, RhythmHand.Right, RhythmDirection.None),
                new(10f, 1f, RhythmActionType.Step, RhythmHand.Left, RhythmDirection.None),
                new(11f, 1f, RhythmActionType.Step, RhythmHand.Right, RhythmDirection.None),
                new(12f, 1f, RhythmActionType.SideGrab, RhythmHand.Left, RhythmDirection.Left),
                new(14f, 1f, RhythmActionType.SideGrab, RhythmHand.Right, RhythmDirection.Right),
                new(16f, 1f, RhythmActionType.Step, RhythmHand.Left, RhythmDirection.None),
                new(17f, 1f, RhythmActionType.Step, RhythmHand.Right, RhythmDirection.None),
                new(20f, 1f, RhythmActionType.Slide, RhythmHand.Both, RhythmDirection.Down),
                new(24f, 1f, RhythmActionType.LongJump, RhythmHand.Both, RhythmDirection.Up),
                new(28f, 1f, RhythmActionType.Step, RhythmHand.Left, RhythmDirection.None),
                new(29f, 1f, RhythmActionType.Step, RhythmHand.Right, RhythmDirection.None),
                new(32f, 1f, RhythmActionType.SideGrab, RhythmHand.Left, RhythmDirection.Left),
                new(34f, 1f, RhythmActionType.SideGrab, RhythmHand.Right, RhythmDirection.Right),
                new(36f, 1f, RhythmActionType.Slide, RhythmHand.Both, RhythmDirection.Down),
                new(40f, 1f, RhythmActionType.LongJump, RhythmHand.Both, RhythmDirection.Up),
                new(44f, 1f, RhythmActionType.Step, RhythmHand.Left, RhythmDirection.None),
                new(45f, 1f, RhythmActionType.Step, RhythmHand.Right, RhythmDirection.None),
                new(48f, 4f, RhythmActionType.Grapple, RhythmHand.Right, RhythmDirection.Up),
                new(56f, 1f, RhythmActionType.Step, RhythmHand.Left, RhythmDirection.None),
                new(57f, 1f, RhythmActionType.Step, RhythmHand.Right, RhythmDirection.None),
                new(60f, 1f, RhythmActionType.Slide, RhythmHand.Both, RhythmDirection.Down),
                new(64f, 1f, RhythmActionType.LongJump, RhythmHand.Both, RhythmDirection.Up),
                new(68f, 1f, RhythmActionType.SideGrab, RhythmHand.Left, RhythmDirection.Left),
                new(70f, 1f, RhythmActionType.SideGrab, RhythmHand.Right, RhythmDirection.Right),
                new(72f, 4f, RhythmActionType.Grapple, RhythmHand.Left, RhythmDirection.Up),
                new(80f, 1f, RhythmActionType.Step, RhythmHand.Left, RhythmDirection.None),
                new(81f, 1f, RhythmActionType.Step, RhythmHand.Right, RhythmDirection.None),
                new(84f, 1f, RhythmActionType.Slide, RhythmHand.Both, RhythmDirection.Down),
                new(88f, 1f, RhythmActionType.LongJump, RhythmHand.Both, RhythmDirection.Up),
                new(92f, 4f, RhythmActionType.Grapple, RhythmHand.Right, RhythmDirection.Up)
            };
        }

        readonly struct DemoDefinition
        {
            public DemoDefinition(
                string id,
                string displayName,
                string instruction,
                IReadOnlyList<RhythmActionEvent> events,
                float hitWindowBeats = 0.45f,
                bool enableMissTempoPenalty = true,
                bool enableRuntimeOffsetAdjustment = false,
                bool alignBeatStripesToEvents = false,
                bool useVisualSetDressing = false)
            {
                Id = id;
                DisplayName = displayName;
                Instruction = instruction;
                Events = events;
                HitWindowBeats = hitWindowBeats;
                EnableMissTempoPenalty = enableMissTempoPenalty;
                EnableRuntimeOffsetAdjustment = enableRuntimeOffsetAdjustment;
                AlignBeatStripesToEvents = alignBeatStripesToEvents;
                UseVisualSetDressing = useVisualSetDressing;
            }

            public string Id { get; }
            public string DisplayName { get; }
            public string Instruction { get; }
            public IReadOnlyList<RhythmActionEvent> Events { get; }
            public float HitWindowBeats { get; }
            public bool EnableMissTempoPenalty { get; }
            public bool EnableRuntimeOffsetAdjustment { get; }
            public bool AlignBeatStripesToEvents { get; }
            public bool UseVisualSetDressing { get; }
        }

        readonly struct PlayerRig
        {
            public PlayerRig(
                GameObject root,
                GameObject cameraOffset,
                GameObject trackingSpace,
                GameObject camera,
                GameObject leftController,
                GameObject rightController,
                VRRhythmActionPrototype prototype)
            {
                Root = root;
                CameraOffset = cameraOffset;
                TrackingSpace = trackingSpace;
                Camera = camera;
                LeftController = leftController;
                RightController = rightController;
                Prototype = prototype;
            }

            public GameObject Root { get; }
            public GameObject CameraOffset { get; }
            public GameObject TrackingSpace { get; }
            public GameObject Camera { get; }
            public GameObject LeftController { get; }
            public GameObject RightController { get; }
            public VRRhythmActionPrototype Prototype { get; }
        }

    }
}
