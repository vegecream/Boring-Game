using BoringRun.VRInput;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RhythmParkour.Editor
{
    public static class Quest3OnboardingSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/SampleScene.unity";
        const string RootName = "Quest3 Onboarding Demo";
        const string SequencePath = "Assets/GuideLevels/Quest3Onboarding/Quest3_Onboarding_Guide_Sequence.asset";
        const string RequestPath = "Assets/Editor/RhythmParkour/RebuildQuest3OnboardingDemo.request";

        static readonly GuideLocation[] Locations =
        {
            new("Guide_Step_Left", "Assets/GuideLevels/Quest3Onboarding/Guide_Step_Left.asset", new Vector3(-0.8f, 0f, 3f), "LEFT STEP"),
            new("Guide_Step_Right", "Assets/GuideLevels/Quest3Onboarding/Guide_Step_Right.asset", new Vector3(0.8f, 0f, 6f), "RIGHT STEP"),
            new("Guide_SideGrab_Left", "Assets/GuideLevels/Quest3Onboarding/Guide_SideGrab_Left.asset", new Vector3(-2.8f, 0f, 10f), "LEFT GRAB"),
            new("Guide_SideGrab_Right", "Assets/GuideLevels/Quest3Onboarding/Guide_SideGrab_Right.asset", new Vector3(2.8f, 0f, 14f), "RIGHT GRAB"),
            new("Guide_Slide", "Assets/GuideLevels/Quest3Onboarding/Guide_Slide.asset", new Vector3(0f, 0f, 19f), "SLIDE"),
            new("Guide_LongJump", "Assets/GuideLevels/Quest3Onboarding/Guide_LongJump.asset", new Vector3(0f, 0f, 26f), "LONG JUMP"),
            new("Guide_Turn_Left", "Assets/GuideLevels/Quest3Onboarding/Guide_Turn_Left.asset", new Vector3(-2.4f, 0f, 32f), "TURN LEFT"),
            new("Guide_Turn_Right", "Assets/GuideLevels/Quest3Onboarding/Guide_Turn_Right.asset", new Vector3(2.4f, 0f, 36f), "TURN RIGHT"),
            new("Guide_Grapple_Hold", "Assets/GuideLevels/Quest3Onboarding/Guide_Grapple_Hold.asset", new Vector3(0f, 0f, 43f), "GRAPPLE")
        };

        [InitializeOnLoadMethod]
        static void RebuildIfRequested()
        {
            if (!File.Exists(RequestPath))
                return;

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

        [MenuItem("Rhythm Parkour/Rebuild Quest3 Onboarding Demo In SampleScene")]
        public static void BuildScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var existingRoot = GameObject.Find(RootName);
            if (existingRoot != null)
                Object.DestroyImmediate(existingRoot);

            var root = new GameObject(RootName);
            CreateEnvironment(root.transform);

            var controller = CreateGuideSystem(root.transform);
            for (var i = 0; i < Locations.Length; i++)
                CreateGuideLocation(root.transform, controller, Locations[i], i);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[RhythmParkour] Rebuilt {RootName} in {ScenePath}.");
        }

        static VRGuideLevelController CreateGuideSystem(Transform parent)
        {
            var guideSystem = new GameObject("Guide System");
            guideSystem.transform.SetParent(parent, false);
            guideSystem.transform.position = Vector3.zero;

            var controller = guideSystem.AddComponent<VRGuideLevelController>();
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("sequence").objectReferenceValue = AssetDatabase.LoadAssetAtPath<VRGuideSequence>(SequencePath);
            serialized.FindProperty("inputEvents").objectReferenceValue = Object.FindObjectOfType<VRParkourInputEvents>(true);
            serialized.FindProperty("autoStart").boolValue = true;
            serialized.FindProperty("advanceAutomatically").boolValue = true;
            serialized.FindProperty("autoAdvanceDelaySeconds").floatValue = 1.35f;
            serialized.FindProperty("showDesktopOverlay").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return controller;
        }

        static void CreateEnvironment(Transform parent)
        {
            var floor = CreateMaterial("Onboarding_Floor", new Color(0.12f, 0.14f, 0.13f));
            var lane = CreateMaterial("Onboarding_Lane", new Color(0.05f, 0.32f, 0.33f));
            var start = CreateMaterial("Onboarding_Start", new Color(0.15f, 0.85f, 0.42f));
            var goal = CreateMaterial("Onboarding_Goal", new Color(0.24f, 0.48f, 1f));

            CreateCube(parent, "Guide Floor", new Vector3(0f, -0.08f, 23f), new Vector3(8f, 0.12f, 50f), floor);
            CreateCube(parent, "Main Tutorial Lane", new Vector3(0f, 0.01f, 23f), new Vector3(2.2f, 0.08f, 48f), lane);
            CreateCube(parent, "Start Pad", new Vector3(0f, 0.06f, 0f), new Vector3(3.6f, 0.12f, 1.6f), start);
            CreateCube(parent, "Finish Pad", new Vector3(0f, 0.06f, 47f), new Vector3(4.2f, 0.12f, 1.8f), goal);
        }

        static void CreateGuideLocation(Transform parent, VRGuideLevelController controller, GuideLocation location, int index)
        {
            var level = AssetDatabase.LoadAssetAtPath<VRGuideLevelDefinition>(location.LevelPath);
            var markerRoot = new GameObject(location.Name);
            markerRoot.transform.SetParent(parent, false);
            markerRoot.transform.position = location.Position;

            var inactiveMat = CreateMaterial($"{location.Name}_Inactive", new Color(0.26f, 0.35f, 0.42f));
            var activeMat = CreateMaterial($"{location.Name}_Active", ColorForIndex(index));
            var markerRenderers = CreateMarkerGeometry(markerRoot.transform, location, inactiveMat, activeMat);
            var sign = CreateFloatingSign(markerRoot.transform, location.Label);

            var marker = markerRoot.AddComponent<VRGuideLevelMarker>();
            var serialized = new SerializedObject(marker);
            serialized.FindProperty("controller").objectReferenceValue = controller;
            serialized.FindProperty("level").objectReferenceValue = level;
            SetObjectArray(serialized.FindProperty("activeOnlyObjects"), new Object[] { sign.Root.gameObject });
            SetObjectArray(serialized.FindProperty("markerRenderers"), markerRenderers);
            serialized.FindProperty("activeColor").colorValue = ColorForIndex(index);
            serialized.FindProperty("inactiveColor").colorValue = new Color(0.25f, 0.35f, 0.42f, 1f);
            serialized.FindProperty("completedColor").colorValue = new Color(0.65f, 0.9f, 1f, 1f);
            serialized.FindProperty("signRoot").objectReferenceValue = sign.Root;
            serialized.FindProperty("signText").objectReferenceValue = sign.Text;
            serialized.FindProperty("hideSignWhenInactive").boolValue = true;
            serialized.FindProperty("faceMainCamera").boolValue = true;
            serialized.FindProperty("activePulseScale").floatValue = 0.05f;
            serialized.FindProperty("pulseRate").floatValue = 1.15f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static Renderer[] CreateMarkerGeometry(Transform parent, GuideLocation location, Material inactiveMat, Material activeMat)
        {
            switch (location.Name)
            {
                case "Guide_SideGrab_Left":
                    return new[]
                    {
                        CreateCube(parent, "Left Side Wall", new Vector3(0f, 1.25f, 0f), new Vector3(0.25f, 2.1f, 1.4f), inactiveMat).GetComponent<Renderer>(),
                        CreateCube(parent, "Grab Target", new Vector3(0.25f, 1.35f, 0f), new Vector3(0.18f, 0.55f, 0.55f), activeMat).GetComponent<Renderer>()
                    };
                case "Guide_SideGrab_Right":
                    return new[]
                    {
                        CreateCube(parent, "Right Side Wall", new Vector3(0f, 1.25f, 0f), new Vector3(0.25f, 2.1f, 1.4f), inactiveMat).GetComponent<Renderer>(),
                        CreateCube(parent, "Grab Target", new Vector3(-0.25f, 1.35f, 0f), new Vector3(0.18f, 0.55f, 0.55f), activeMat).GetComponent<Renderer>()
                    };
                case "Guide_Slide":
                    return new[]
                    {
                        CreateCube(parent, "Slide Left Post", new Vector3(-1.25f, 0.85f, 0f), new Vector3(0.25f, 1.7f, 0.28f), inactiveMat).GetComponent<Renderer>(),
                        CreateCube(parent, "Slide Right Post", new Vector3(1.25f, 0.85f, 0f), new Vector3(0.25f, 1.7f, 0.28f), inactiveMat).GetComponent<Renderer>(),
                        CreateCube(parent, "Low Slide Bar", new Vector3(0f, 1.35f, 0f), new Vector3(2.8f, 0.25f, 0.28f), activeMat).GetComponent<Renderer>()
                    };
                case "Guide_LongJump":
                    return new[]
                    {
                        CreateCube(parent, "Jump Takeoff", new Vector3(0f, 0.1f, -0.75f), new Vector3(2.5f, 0.18f, 0.7f), activeMat).GetComponent<Renderer>(),
                        CreateCube(parent, "Jump Landing", new Vector3(0f, 0.1f, 1f), new Vector3(2.5f, 0.18f, 0.9f), inactiveMat).GetComponent<Renderer>()
                    };
                case "Guide_Turn_Left":
                    return CreateArrow(parent, "Left Turn Arrow", -1f, activeMat);
                case "Guide_Turn_Right":
                    return CreateArrow(parent, "Right Turn Arrow", 1f, activeMat);
                case "Guide_Grapple_Hold":
                    return new[]
                    {
                        CreateCube(parent, "Grapple Pillar", new Vector3(0f, 1.25f, 0f), new Vector3(0.25f, 2.5f, 0.25f), inactiveMat).GetComponent<Renderer>(),
                        CreateCube(parent, "Grapple Target", new Vector3(0f, 2.65f, 0f), new Vector3(0.85f, 0.28f, 0.85f), activeMat).GetComponent<Renderer>()
                    };
                default:
                    return new[]
                    {
                        CreateCube(parent, "Step Pad", new Vector3(0f, 0.08f, 0f), new Vector3(1.4f, 0.16f, 1.2f), activeMat).GetComponent<Renderer>()
                    };
            }
        }

        static Renderer[] CreateArrow(Transform parent, string name, float direction, Material material)
        {
            var shaft = CreateCube(parent, $"{name} Shaft", new Vector3(direction * 0.35f, 1.05f, 0f), new Vector3(0.8f, 0.18f, 0.18f), material);
            var headA = CreateCube(parent, $"{name} Head A", new Vector3(direction * 0.85f, 1.17f, 0f), new Vector3(0.45f, 0.15f, 0.15f), material);
            var headB = CreateCube(parent, $"{name} Head B", new Vector3(direction * 0.85f, 0.93f, 0f), new Vector3(0.45f, 0.15f, 0.15f), material);
            headA.transform.localRotation = Quaternion.Euler(0f, 0f, direction > 0f ? -35f : 35f);
            headB.transform.localRotation = Quaternion.Euler(0f, 0f, direction > 0f ? 35f : -35f);

            return new[]
            {
                shaft.GetComponent<Renderer>(),
                headA.GetComponent<Renderer>(),
                headB.GetComponent<Renderer>()
            };
        }

        static Sign CreateFloatingSign(Transform parent, string label)
        {
            var signRoot = new GameObject("Floating Sign").transform;
            signRoot.SetParent(parent, false);
            signRoot.localPosition = new Vector3(0f, 2.25f, -0.35f);
            signRoot.localScale = Vector3.one;

            var back = CreateCube(signRoot, "Sign Back Plate", Vector3.zero, new Vector3(2.9f, 0.75f, 0.08f), CreateMaterial("Sign_Back", new Color(0.03f, 0.05f, 0.07f)));
            back.transform.localPosition = new Vector3(0f, 0f, 0.05f);

            var textObject = new GameObject("Sign Text");
            textObject.transform.SetParent(signRoot, false);
            textObject.transform.localPosition = new Vector3(-1.32f, -0.22f, -0.02f);
            textObject.transform.localRotation = Quaternion.identity;
            textObject.transform.localScale = Vector3.one * 0.08f;

            var text = textObject.AddComponent<TextMesh>();
            text.text = label;
            text.anchor = TextAnchor.MiddleLeft;
            text.alignment = TextAlignment.Left;
            text.characterSize = 0.28f;
            text.fontSize = 64;
            text.color = Color.white;

            return new Sign(signRoot, text);
        }

        static GameObject CreateCube(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPosition;
            cube.transform.localScale = localScale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            return cube;
        }

        static Material CreateMaterial(string name, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Standard");

            var material = new Material(shader)
            {
                name = name
            };

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            else
                material.color = color;

            return material;
        }

        static Color ColorForIndex(int index)
        {
            var colors = new[]
            {
                new Color(0.2f, 0.95f, 0.55f, 1f),
                new Color(0.35f, 0.75f, 1f, 1f),
                new Color(1f, 0.72f, 0.2f, 1f),
                new Color(1f, 0.4f, 0.32f, 1f),
                new Color(0.65f, 0.52f, 1f, 1f),
                new Color(0.95f, 0.9f, 0.35f, 1f)
            };

            return colors[index % colors.Length];
        }

        static void SetObjectArray(SerializedProperty property, Object[] values)
        {
            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        readonly struct GuideLocation
        {
            public readonly string Name;
            public readonly string LevelPath;
            public readonly Vector3 Position;
            public readonly string Label;

            public GuideLocation(string name, string levelPath, Vector3 position, string label)
            {
                Name = name;
                LevelPath = levelPath;
                Position = position;
                Label = label;
            }
        }

        readonly struct Sign
        {
            public readonly Transform Root;
            public readonly TextMesh Text;

            public Sign(Transform root, TextMesh text)
            {
                Root = root;
                Text = text;
            }
        }
    }
}
