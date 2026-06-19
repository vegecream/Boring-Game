using System.IO;
using BoringRun.VRInput;
using RhythmParkour;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RhythmParkour.Editor
{
    public static class VRActionPrototypeBuilder
    {
        const string RequestPath = "Assets/Editor/RhythmParkour/RebuildVRActionPrototype.request";
        const string ScenePath = "Assets/Scenes/VRActionPrototype.unity";
        const string TrackPath = "Assets/Rhythm/Tutorial_120BPM_Track.asset";
        const float UnitsPerBeat = 2.5f;

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

        [MenuItem("Rhythm Parkour/Rebuild VR Action Prototype")]
        public static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.ambientLight = new Color(0.09f, 0.08f, 0.14f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.02f, 0.01f, 0.05f);
            RenderSettings.fogDensity = 0.012f;

            var floorMat = CreateMaterial("ActionDemo_DarkFloor", new Color(0.025f, 0.025f, 0.05f));
            var laneMat = CreateMaterial("ActionDemo_Lane", new Color(0.02f, 0.4f, 0.48f));
            var timingMat = CreateMaterial("ActionDemo_TimingLine", new Color(1f, 0.95f, 0.25f));
            var ballMat = CreateMaterial("ActionDemo_CueBall", new Color(1f, 0.82f, 0.05f));
            var goalMat = CreateMaterial("ActionDemo_Goal", new Color(0.06f, 0.22f, 1f));

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 17f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.005f, 0.004f, 0.025f);
            camera.transform.position = new Vector3(0f, 26f, 12f);
            camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            cameraObject.AddComponent<AudioListener>();

            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            CreateCube("Open Space Floor", new Vector3(0f, -0.08f, 36f), new Vector3(12f, 0.12f, 88f), floorMat);
            CreateCube("Neon Run Lane", new Vector3(0f, 0.02f, 36f), new Vector3(2.1f, 0.08f, 84f), laneMat);
            CreateCube("Timing Line", new Vector3(0f, 0.18f, 0f), new Vector3(9f, 0.18f, 0.16f), timingMat);
            CreateCube("Goal Portal Marker", new Vector3(0f, 0.4f, 68f), new Vector3(7f, 0.8f, 0.28f), goalMat);

            var track = AssetDatabase.LoadAssetAtPath<RhythmTrackConfig>(TrackPath);
            if (track != null && track.Chart != null)
            {
                foreach (var evt in track.Chart.Events)
                    CreateActionCue(evt);
            }

            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "Action Cue Ball";
            ball.transform.position = new Vector3(0f, 0.6f, 0f);
            ball.transform.localScale = Vector3.one * 1.15f;
            ball.GetComponent<Renderer>().sharedMaterial = ballMat;

            var prototypeObject = new GameObject("VR Rhythm Action Prototype");
            var audioSource = prototypeObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            var distortionFilter = prototypeObject.AddComponent<AudioDistortionFilter>();
            distortionFilter.distortionLevel = 0f;

            var prototype = prototypeObject.AddComponent<VRRhythmActionPrototype>();
            var serialized = new SerializedObject(prototype);
            serialized.FindProperty("track").objectReferenceValue = track;
            serialized.FindProperty("useKeyboardFallback").boolValue = true;
            serialized.FindProperty("hitWindowBeats").floatValue = 0.45f;
            serialized.FindProperty("cueObject").objectReferenceValue = ball.transform;
            serialized.FindProperty("unitsPerBeat").floatValue = UnitsPerBeat;
            serialized.FindProperty("cameraToFollow").objectReferenceValue = camera.transform;
            serialized.FindProperty("cameraLeadUnits").floatValue = 10f;
            serialized.FindProperty("minimumCameraZ").floatValue = 12f;
            serialized.FindProperty("musicDistortionFilter").objectReferenceValue = distortionFilter;
            serialized.FindProperty("distortionNoiseVolume").floatValue = 0.08f;
            serialized.FindProperty("maxMusicDistortionLevel").floatValue = 0.22f;
            serialized.FindProperty("dropoutPulseRate").floatValue = 9f;
            serialized.FindProperty("dropoutVolumeFloor").floatValue = 0.7f;
            serialized.FindProperty("distortionSmoothingSeconds").floatValue = 0.35f;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[RhythmParkour] Rebuilt {ScenePath} from {TrackPath}.");
        }

        static void CreateActionCue(RhythmActionEvent evt)
        {
            var z = evt.Beat * UnitsPerBeat;
            var material = CreateMaterial($"ActionDemo_{evt.ActionType}_{evt.Beat:0.##}", ColorForAction(evt));

            switch (evt.ActionType)
            {
                case RhythmActionType.Step:
                    CreateCube($"Step Beat {evt.Beat:0.##} {evt.Hand}", new Vector3(0f, 0.32f, z), new Vector3(2.5f, 0.45f, 0.18f), material);
                    break;
                case RhythmActionType.SideGrab:
                    var sideX = evt.Direction == RhythmDirection.Left ? -3.6f : 3.6f;
                    CreateCube($"Side Grab Beat {evt.Beat:0.##} {evt.Hand} {evt.Direction}", new Vector3(sideX, 0.7f, z), new Vector3(0.9f, 0.9f, 0.22f), material);
                    break;
                case RhythmActionType.Slide:
                    CreateCube($"Slide Beat {evt.Beat:0.##}", new Vector3(0f, 0.22f, z), new Vector3(5.8f, 0.28f, 0.2f), material);
                    break;
                case RhythmActionType.LongJump:
                    CreateCube($"Long Jump Beat {evt.Beat:0.##}", new Vector3(0f, 1.0f, z), new Vector3(5.8f, 0.35f, 0.2f), material);
                    break;
                case RhythmActionType.Grapple:
                    var holdLength = Mathf.Max(0.4f, evt.DurationBeats * UnitsPerBeat);
                    CreateCube(
                        $"Grapple Beat {evt.Beat:0.##} {evt.Hand}",
                        new Vector3(0f, 1.45f, z + holdLength * 0.5f),
                        new Vector3(1.2f, 1.2f, holdLength),
                        material);
                    break;
            }
        }

        static Color ColorForAction(RhythmActionEvent evt)
        {
            switch (evt.ActionType)
            {
                case RhythmActionType.Step:
                    return new Color(0.95f, 0.08f, 1f);
                case RhythmActionType.SideGrab:
                    return new Color(0.05f, 0.95f, 1f);
                case RhythmActionType.Slide:
                    return new Color(1f, 0.18f, 0.1f);
                case RhythmActionType.LongJump:
                    return new Color(0.16f, 1f, 0.28f);
                case RhythmActionType.Grapple:
                    return new Color(1f, 0.58f, 0.05f);
                default:
                    return Color.white;
            }
        }

        static Material CreateMaterial(string name, Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"))
            {
                name = name
            };
            material.SetColor("_BaseColor", color * 1.35f);
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
