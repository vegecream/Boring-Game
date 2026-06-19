using BoringRun.VRInput;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RhythmParkour.Editor
{
    public static class VRGameFlowSceneBuilder
    {
        const string SceneFolder = "Assets/Scenes/GameFlow";
        const string MaterialFolder = "Assets/Materials/UI";
        const string MainMenuScene = SceneFolder + "/VRMainMenu.unity";
        const string TutorialScene = SceneFolder + "/VRTutorialPlaceholder.unity";
        const string LevelSelectScene = SceneFolder + "/VRLevelSelect.unity";
        const string DemoScene = "Assets/Scenes/FormalDemos/TellingWorldFormalDemo_EditBase.unity";
        const string SkyboxPath = "Assets/Materials/Stage/M_FormalDemoExpandedRedSkybox.mat";
        const string RequestPath = "Assets/Editor/RhythmParkour/RebuildVRGameFlow.request";

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

                BuildAll();
                AssetDatabase.DeleteAsset(RequestPath);
                AssetDatabase.Refresh();
            };
        }

        [MenuItem("Rhythm Parkour/Rebuild VR Game Flow Scenes")]
        public static void BuildAll()
        {
            EnsureFolder("Assets/Scenes", "GameFlow");
            EnsureFolder("Assets/Materials", "UI");

            var materials = CreateOrLoadMaterials();
            BuildMenuScene(new MenuSceneSpec(
                MainMenuScene,
                "TELLING WORLD",
                "VR Rhythm Parkour Prototype",
                "",
                new[]
                {
                    new ButtonSpec("Tutorial", "Open the tutorial placeholder.", VRMenuButtonAction.LoadScene, "VRTutorialPlaceholder"),
                    new ButtonSpec("Level Select", "Choose a playable demo level.", VRMenuButtonAction.LoadScene, "VRLevelSelect"),
                    new ButtonSpec("Quit", "Exit the application.", VRMenuButtonAction.Quit, "")
                }),
                materials);

            BuildMenuScene(new MenuSceneSpec(
                TutorialScene,
                "TUTORIAL",
                "Tutorial flow placeholder",
                "The tutorial content will be wired after the action demos settle.",
                new[]
                {
                    new ButtonSpec("Back", "Return to the main menu.", VRMenuButtonAction.LoadScene, "VRMainMenu")
                }),
                materials);

            BuildMenuScene(new MenuSceneSpec(
                LevelSelectScene,
                "LEVEL SELECT",
                "Available demo levels",
                "Select the current formal rhythm parkour demo.",
                new[]
                {
                    new ButtonSpec("Demo Level", "Telling World formal demo.", VRMenuButtonAction.LoadScene, "TellingWorldFormalDemo_EditBase"),
                    new ButtonSpec("Back", "Return to the main menu.", VRMenuButtonAction.LoadScene, "VRMainMenu")
                }),
                materials);

            UpdateBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[RhythmParkour] Rebuilt VR game flow scenes.");
        }

        static void BuildMenuScene(MenuSceneSpec spec, MenuMaterials materials)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>(SkyboxPath);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.24f, 0.28f, 0.42f, 1f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.03f, 0.04f, 0.1f, 1f);
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.018f;

            var rig = CreateMenuRig(materials);
            CreateLighting();
            CreateSetDressing(materials);
            var buttons = CreateMenuPanel(spec, materials);
            CreateMenuController(rig, buttons, spec.BackSceneName, materials);

            EditorSceneManager.SaveScene(scene, spec.ScenePath);
        }

        static MenuRig CreateMenuRig(MenuMaterials materials)
        {
            var root = new GameObject("[BuildingBlock] Camera Rig");
            root.transform.position = new Vector3(0f, 1.55f, -3.4f);

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

            leftController.transform.localPosition = new Vector3(-0.38f, -0.28f, 0.62f);
            rightController.transform.localPosition = new Vector3(0.38f, -0.28f, 0.62f);

            root.AddComponent<OVRCameraRig>();
            var manager = root.AddComponent<OVRManager>();
            manager.trackingOriginType = OVRManager.TrackingOrigin.FloorLevel;

            var inputObject = new GameObject("VR Menu Input System");
            var reader = inputObject.AddComponent<VRInputReader>();
            var readerSerialized = new SerializedObject(reader);
            readerSerialized.FindProperty("directionReference").objectReferenceValue = centerEye.transform;
            readerSerialized.FindProperty("playerTurnReference").objectReferenceValue = centerEye.transform;
            readerSerialized.ApplyModifiedPropertiesWithoutUndo();

            var poseBinder = inputObject.AddComponent<VRControllerPoseBinder>();
            var poseSerialized = new SerializedObject(poseBinder);
            poseSerialized.FindProperty("trackingSpace").objectReferenceValue = trackingSpace.transform;
            poseSerialized.FindProperty("leftController").objectReferenceValue = leftController.transform;
            poseSerialized.FindProperty("rightController").objectReferenceValue = rightController.transform;
            poseSerialized.ApplyModifiedPropertiesWithoutUndo();

            var leftRay = CreateRay("Left Menu Ray", materials.Ray, new Color(0.25f, 0.85f, 1f, 0.9f));
            var rightRay = CreateRay("Right Menu Ray", materials.Ray, new Color(1f, 0.18f, 0.68f, 0.9f));

            return new MenuRig(root.transform, trackingSpace.transform, centerEye.transform, leftController.transform, rightController.transform, reader, leftRay, rightRay);
        }

        static GameObject CreateEyeAnchor(string name, Transform parent, bool mainCamera)
        {
            var anchor = CreateAnchor(name, parent);
            if (mainCamera)
                anchor.tag = "MainCamera";

            var camera = anchor.AddComponent<Camera>();
            camera.enabled = mainCamera;
            camera.fieldOfView = 72f;
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = 250f;
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

        static LineRenderer CreateRay(string name, Material material, Color color)
        {
            var rayObject = new GameObject(name);
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

        static void CreateLighting()
        {
            var lightObject = new GameObject("Menu Key Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.05f;
            light.color = new Color(0.82f, 0.9f, 1f, 1f);
            light.transform.rotation = Quaternion.Euler(38f, -28f, 0f);
        }

        static void CreateSetDressing(MenuMaterials materials)
        {
            CreateCube("Menu Light Bridge", new Vector3(0f, -0.08f, 1.2f), new Vector3(4.7f, 0.055f, 9f), materials.Platform);
            CreateCube("Menu Left Edge", new Vector3(-2.45f, 0.02f, 1.2f), new Vector3(0.05f, 0.08f, 8.8f), materials.Edge);
            CreateCube("Menu Right Edge", new Vector3(2.45f, 0.02f, 1.2f), new Vector3(0.05f, 0.08f, 8.8f), materials.Edge);

            for (var i = 0; i < 12; i++)
            {
                var z = -2.6f + i * 0.72f;
                CreateCube($"Menu Beat Stripe {i:00}", new Vector3(0f, 0.01f, z), new Vector3(4.4f, 0.035f, 0.025f), i % 4 == 0 ? materials.Selected : materials.Edge);
            }

            CreateCube("Distant Portal Hint", new Vector3(0f, 2.05f, 7.4f), new Vector3(1.6f, 1.6f, 0.08f), materials.Hover);
            CreateCube("Distant Portal Core", new Vector3(0f, 2.05f, 7.34f), new Vector3(1.05f, 1.05f, 0.06f), materials.Platform);
        }

        static VRMenuButton[] CreateMenuPanel(MenuSceneSpec spec, MenuMaterials materials)
        {
            var panelRoot = new GameObject("VR Menu Panel");
            panelRoot.transform.position = new Vector3(0f, 1.72f, 2.15f);

            CreateText(panelRoot.transform, "Title", spec.Title, new Vector3(0f, 1.2f, -0.08f), 0.145f, TextAnchor.MiddleCenter, Color.white);
            CreateText(panelRoot.transform, "Subtitle", spec.Subtitle, new Vector3(0f, 0.84f, -0.08f), 0.057f, TextAnchor.MiddleCenter, new Color(0.68f, 0.93f, 1f, 1f));

            if (!string.IsNullOrEmpty(spec.Description))
                CreateText(panelRoot.transform, "Description", spec.Description, new Vector3(0f, 0.54f, -0.08f), 0.04f, TextAnchor.MiddleCenter, new Color(0.78f, 0.82f, 1f, 0.88f));

            var buttons = new List<VRMenuButton>();
            var firstButtonY = string.IsNullOrEmpty(spec.Description) ? 0.36f : 0.2f;
            for (var i = 0; i < spec.Buttons.Length; i++)
            {
                var y = firstButtonY - i * 0.62f;
                buttons.Add(CreateButton(panelRoot.transform, spec.Buttons[i], i, new Vector3(0f, y, 0f), materials));
            }

            CreateText(panelRoot.transform, "Control Hint", "Point with a controller and press trigger. Keyboard: Up/Down + Space.", new Vector3(0f, -1.55f, -0.08f), 0.033f, TextAnchor.MiddleCenter, new Color(0.45f, 0.82f, 1f, 0.8f));
            return buttons.ToArray();
        }

        static VRMenuButton CreateButton(Transform parent, ButtonSpec spec, int index, Vector3 localPosition, MenuMaterials materials)
        {
            var root = new GameObject($"Menu Button {index:00} {spec.Label}");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPosition;

            var plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.name = "Plate";
            plate.transform.SetParent(root.transform, false);
            plate.transform.localPosition = Vector3.zero;
            plate.transform.localScale = new Vector3(3.55f, 0.46f, 0.08f);
            var renderer = plate.GetComponent<Renderer>();
            renderer.sharedMaterial = materials.Normal;

            var label = CreateText(root.transform, "Label", spec.Label, new Vector3(0f, 0.07f, -0.075f), 0.065f, TextAnchor.MiddleCenter, Color.white);
            var detail = CreateText(root.transform, "Detail", spec.Detail, new Vector3(0f, -0.12f, -0.075f), 0.033f, TextAnchor.MiddleCenter, new Color(0.7f, 0.88f, 1f, 0.78f));

            var button = root.AddComponent<VRMenuButton>();
            button.Configure(spec.Action, spec.SceneName, label, detail, new[] { renderer }, materials.Normal, materials.Hover, materials.Selected);
            return button;
        }

        static TextMesh CreateText(Transform parent, string name, string text, Vector3 localPosition, float scale, TextAnchor anchor, Color color)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            textObject.transform.localPosition = localPosition;
            textObject.transform.localScale = Vector3.one * scale;

            var mesh = textObject.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.anchor = anchor;
            mesh.alignment = TextAlignment.Center;
            mesh.fontSize = 96;
            mesh.characterSize = 0.2f;
            mesh.color = color;
            return mesh;
        }

        static void CreateMenuController(MenuRig rig, VRMenuButton[] buttons, string backSceneName, MenuMaterials materials)
        {
            var controllerObject = new GameObject("VR Menu Controller");
            var controller = controllerObject.AddComponent<VRMenuController>();
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("inputReader").objectReferenceValue = rig.InputReader;
            serialized.FindProperty("leftRayOrigin").objectReferenceValue = rig.LeftController;
            serialized.FindProperty("rightRayOrigin").objectReferenceValue = rig.RightController;
            serialized.FindProperty("menuCamera").objectReferenceValue = rig.Camera.GetComponent<Camera>();
            serialized.FindProperty("leftRay").objectReferenceValue = rig.LeftRay;
            serialized.FindProperty("rightRay").objectReferenceValue = rig.RightRay;
            serialized.FindProperty("rayDistance").floatValue = 12f;
            serialized.FindProperty("backSceneName").stringValue = backSceneName;
            SetObjectArray(serialized.FindProperty("buttons"), buttons);
            serialized.ApplyModifiedPropertiesWithoutUndo();
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

        static MenuMaterials CreateOrLoadMaterials()
        {
            return new MenuMaterials(
                CreateMaterial(MaterialFolder + "/M_Menu_Button_Normal.mat", new Color(0.08f, 0.08f, 0.18f, 0.86f), true),
                CreateMaterial(MaterialFolder + "/M_Menu_Button_Hover.mat", new Color(0.1f, 0.55f, 1f, 0.84f), true),
                CreateMaterial(MaterialFolder + "/M_Menu_Button_Selected.mat", new Color(1f, 0.08f, 0.42f, 0.9f), true),
                CreateMaterial(MaterialFolder + "/M_Menu_Platform.mat", new Color(0.06f, 0.58f, 1f, 0.24f), true),
                CreateMaterial(MaterialFolder + "/M_Menu_Edge.mat", new Color(0.36f, 1f, 1f, 0.78f), true),
                CreateMaterial(MaterialFolder + "/M_Menu_Ray.mat", new Color(1f, 1f, 1f, 0.85f), true));
        }

        static Material CreateMaterial(string path, Color color, bool transparent)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null)
                    shader = Shader.Find("Standard");

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.name = Path.GetFileNameWithoutExtension(path);
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

            EditorUtility.SetDirty(material);
            return material;
        }

        static void UpdateBuildSettings()
        {
            var orderedPaths = new[]
            {
                MainMenuScene,
                TutorialScene,
                LevelSelectScene,
                DemoScene
            };

            var scenePaths = new List<string>(orderedPaths);
            var existing = EditorBuildSettings.scenes;
            for (var i = 0; i < existing.Length; i++)
            {
                var path = existing[i].path;
                if (!string.IsNullOrEmpty(path) && !scenePaths.Contains(path))
                    scenePaths.Add(path);
            }

            var scenes = new EditorBuildSettingsScene[scenePaths.Count];
            for (var i = 0; i < scenePaths.Count; i++)
                scenes[i] = new EditorBuildSettingsScene(scenePaths[i], true);

            EditorBuildSettings.scenes = scenes;
        }

        static void EnsureFolder(string parent, string child)
        {
            var path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }

        static void SetObjectArray(SerializedProperty property, Object[] values)
        {
            property.arraySize = values != null ? values.Length : 0;
            for (var i = 0; i < property.arraySize; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        readonly struct MenuSceneSpec
        {
            public readonly string ScenePath;
            public readonly string Title;
            public readonly string Subtitle;
            public readonly string Description;
            public readonly string BackSceneName;
            public readonly ButtonSpec[] Buttons;

            public MenuSceneSpec(string scenePath, string title, string subtitle, string description, ButtonSpec[] buttons)
            {
                ScenePath = scenePath;
                Title = title;
                Subtitle = subtitle;
                Description = description;
                Buttons = buttons;
                BackSceneName = scenePath == MainMenuScene ? "" : "VRMainMenu";
            }
        }

        readonly struct ButtonSpec
        {
            public readonly string Label;
            public readonly string Detail;
            public readonly VRMenuButtonAction Action;
            public readonly string SceneName;

            public ButtonSpec(string label, string detail, VRMenuButtonAction action, string sceneName)
            {
                Label = label;
                Detail = detail;
                Action = action;
                SceneName = sceneName;
            }
        }

        readonly struct MenuRig
        {
            public readonly Transform Root;
            public readonly Transform TrackingSpace;
            public readonly Transform Camera;
            public readonly Transform LeftController;
            public readonly Transform RightController;
            public readonly VRInputReader InputReader;
            public readonly LineRenderer LeftRay;
            public readonly LineRenderer RightRay;

            public MenuRig(Transform root, Transform trackingSpace, Transform camera, Transform leftController, Transform rightController, VRInputReader inputReader, LineRenderer leftRay, LineRenderer rightRay)
            {
                Root = root;
                TrackingSpace = trackingSpace;
                Camera = camera;
                LeftController = leftController;
                RightController = rightController;
                InputReader = inputReader;
                LeftRay = leftRay;
                RightRay = rightRay;
            }
        }

        readonly struct MenuMaterials
        {
            public readonly Material Normal;
            public readonly Material Hover;
            public readonly Material Selected;
            public readonly Material Platform;
            public readonly Material Edge;
            public readonly Material Ray;

            public MenuMaterials(Material normal, Material hover, Material selected, Material platform, Material edge, Material ray)
            {
                Normal = normal;
                Hover = hover;
                Selected = selected;
                Platform = platform;
                Edge = edge;
                Ray = ray;
            }
        }
    }
}
