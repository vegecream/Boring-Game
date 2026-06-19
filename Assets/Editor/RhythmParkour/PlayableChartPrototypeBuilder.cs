using System.IO;
using RhythmParkour;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RhythmParkour.Editor
{

    public static class PlayableChartPrototypeBuilder
    {
        private const string RequestPath = "Assets/Editor/RhythmParkour/RebuildPlayableChartPrototype.request";
        private const string OpenRequestPath = "Assets/Editor/RhythmParkour/OpenPlayableChartPrototype.request";
        private const string ScenePath = "Assets/Scenes/PlayableChartPrototype.unity";
        private const string TrackPath = "Assets/Rhythm/Tutorial_120BPM_Track.asset";
        private const string NoisePath = "Assets/Audio/Distortion_Noise_30s.wav";

        [InitializeOnLoadMethod]
        private static void RebuildIfRequested()
        {
            if (File.Exists(OpenRequestPath))
            {
                EditorApplication.delayCall += () =>
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        EditorApplication.isPlaying = false;
                        EditorApplication.delayCall += RebuildIfRequested;
                        return;
                    }

                    EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                    Debug.Log($"[RhythmParkour] Opened {ScenePath}.");
                    AssetDatabase.DeleteAsset(OpenRequestPath);
                    AssetDatabase.Refresh();
                };
            }

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

        [MenuItem("Rhythm Parkour/Rebuild Playable Chart Prototype")]
        public static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.ambientLight = new Color(0.12f, 0.12f, 0.16f);
            RenderSettings.fog = false;

            var floorMat = CreateMaterial("TopDown_DarkFloor", new Color(0.03f, 0.035f, 0.055f));
            var laneMat = CreateMaterial("TopDown_Lane", new Color(0.04f, 0.25f, 0.32f));
            var wallMat = CreateMaterial("TopDown_WallCue", new Color(0.9f, 0.08f, 1f));
            var ballMat = CreateMaterial("TopDown_PlayerBall", new Color(1f, 0.86f, 0.08f));
            var startMat = CreateMaterial("TopDown_Start", new Color(1f, 0.1f, 0.15f));
            var goalMat = CreateMaterial("TopDown_Goal", new Color(0.05f, 0.2f, 1f));

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 16f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.005f, 0.005f, 0.018f);
            camera.transform.position = new Vector3(0f, 24f, 12f);
            camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            cameraObject.AddComponent<AudioListener>();

            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            CreateCube("Top Down Floor", new Vector3(0f, -0.08f, 20f), new Vector3(12f, 0.12f, 56f), floorMat);
            CreateCube("Beat Lane", new Vector3(0f, 0.02f, 20f), new Vector3(2.2f, 0.08f, 52f), laneMat);
            CreateCube("Start Red Band", new Vector3(0f, 0.12f, -1.5f), new Vector3(8f, 0.15f, 0.35f), startMat);
            CreateCube("Goal Blue Band", new Vector3(0f, 0.12f, 45f), new Vector3(8f, 0.15f, 0.35f), goalMat);

            var track = AssetDatabase.LoadAssetAtPath<RhythmTrackConfig>(TrackPath);
            const float unitsPerBeat = 2.5f;
            if (track != null && track.Chart != null)
            {
                foreach (var evt in track.Chart.Events)
                {
                    CreateCube(
                        $"Wall Beat {evt.Beat:0.##} {evt.ActionType}",
                        new Vector3(0f, 0.32f, evt.Beat * unitsPerBeat),
                        new Vector3(7f, 0.45f, 0.18f),
                        wallMat);
                }
            }

            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "Beat Ball";
            ball.transform.position = new Vector3(0f, 0.6f, 0f);
            ball.transform.localScale = Vector3.one * 1.25f;
            ball.GetComponent<Renderer>().sharedMaterial = ballMat;

            var prototypeObject = new GameObject("Single Button Chart Prototype");
            var audioSource = prototypeObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;

            var distortionFilter = prototypeObject.AddComponent<AudioDistortionFilter>();
            distortionFilter.distortionLevel = 0f;

            var noiseClip = AssetDatabase.LoadAssetAtPath<AudioClip>(NoisePath);
            var noiseSource = prototypeObject.AddComponent<AudioSource>();
            noiseSource.clip = noiseClip;
            noiseSource.playOnAwake = false;
            noiseSource.loop = true;
            noiseSource.volume = 0f;
            noiseSource.spatialBlend = 0f;

            var prototype = prototypeObject.AddComponent<SingleButtonChartPrototype>();
            var serialized = new SerializedObject(prototype);
            serialized.FindProperty("track").objectReferenceValue = track;
            serialized.FindProperty("hitKey").intValue = (int)KeyCode.Space;
            serialized.FindProperty("hitWindowBeats").floatValue = 0.45f;
            serialized.FindProperty("cueObject").objectReferenceValue = ball.transform;
            serialized.FindProperty("unitsPerBeat").floatValue = unitsPerBeat;
            serialized.FindProperty("startDelaySeconds").floatValue = 0f;
            serialized.FindProperty("cameraToFollow").objectReferenceValue = camera.transform;
            serialized.FindProperty("cameraLeadUnits").floatValue = 10f;
            serialized.FindProperty("minimumCameraZ").floatValue = 12f;
            serialized.FindProperty("noiseSource").objectReferenceValue = noiseSource;
            serialized.FindProperty("distortionNoiseVolume").floatValue = 0.12f;
            serialized.FindProperty("musicDistortionFilter").objectReferenceValue = distortionFilter;
            serialized.FindProperty("maxMusicDistortionLevel").floatValue = 0.38f;
            serialized.FindProperty("dropoutPulseRate").floatValue = 9f;
            serialized.FindProperty("dropoutVolumeFloor").floatValue = 0.45f;
            serialized.FindProperty("distortionSmoothingSeconds").floatValue = 0.35f;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[RhythmParkour] Rebuilt {ScenePath} from {TrackPath}.");
        }

        private static Material CreateMaterial(string name, Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"))
            {
                name = name
            };
            material.SetColor("_BaseColor", color * 1.1f);
            return material;
        }

        private static GameObject CreateCube(string name, Vector3 position, Vector3 scale, Material material)
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
