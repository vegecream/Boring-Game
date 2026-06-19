using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RhythmParkour.Editor
{
    public static class VolumetricCloudsInstaller
    {
        const string RendererPath = "Assets/Settings/URP/VR_URP_Renderer.asset";
        const string CloudsMaterialPath = "Assets/OpenSource/UnityVolumetricCloudsURP/VolumetricClouds/VolumetricClouds.mat";
        const string VolumeProfileFolder = "Assets/Settings/Volumes";
        static readonly string[] ScenePaths =
        {
            "Assets/Scenes/SkyboxOnlyTest.unity",
            "Assets/Scenes/SynthwaveStageBackgroundPreview.unity",
            "Assets/Scenes/RhythmLightRibbonPreview.unity",
            "Assets/Scenes/NeoCityFallingPreview.unity"
        };

        [MenuItem("Rhythm Parkour/Install Volumetric Clouds Preview")]
        public static void Install()
        {
            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (rendererData == null)
            {
                Debug.LogError($"[RhythmParkour] URP renderer not found at {RendererPath}.");
                return;
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(CloudsMaterialPath);
            if (material == null)
            {
                Debug.LogError($"[RhythmParkour] Volumetric clouds material not found at {CloudsMaterialPath}.");
                return;
            }

            EnsureRendererFeature(rendererData, material);
            ConfigureRendererDepthPath(rendererData);
            foreach (var scenePath in ScenePaths)
                EnsureCloudVolume(scenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[RhythmParkour] Installed volumetric clouds renderer feature and preview scene volumes.");
        }

        static void EnsureRendererFeature(UniversalRendererData rendererData, Material material)
        {
            var featuresProperty = new SerializedObject(rendererData).FindProperty("m_RendererFeatures");
            VolumetricCloudsURP feature = null;

            if (featuresProperty != null)
            {
                for (var i = 0; i < featuresProperty.arraySize; i++)
                {
                    feature = featuresProperty.GetArrayElementAtIndex(i).objectReferenceValue as VolumetricCloudsURP;
                    if (feature != null)
                        break;
                }
            }

            if (feature == null)
            {
                feature = ScriptableObject.CreateInstance<VolumetricCloudsURP>();
                feature.name = "VolumetricCloudsURP";
                AssetDatabase.AddObjectToAsset(feature, rendererData);
            }

            var featureSerialized = new SerializedObject(feature);
            SetObject(featureSerialized, "material", material);
            SetBool(featureSerialized, "renderingDebugger", false);
            SetBool(featureSerialized, "reflectionProbe", false);
            SetFloat(featureSerialized, "resolutionScale", 0.55f);
            SetInt(featureSerialized, "upscaleMode", 1);
            SetInt(featureSerialized, "preferredRenderMode", 1);
            SetInt(featureSerialized, "ambientProbe", 1);
            SetBool(featureSerialized, "sunAttenuation", false);
            SetBool(featureSerialized, "resetOnStart", true);
            SetBool(featureSerialized, "outputDepth", false);
            featureSerialized.ApplyModifiedPropertiesWithoutUndo();
            feature.SetActive(true);
            EditorUtility.SetDirty(feature);

            var rendererSerialized = new SerializedObject(rendererData);
            var rendererFeatures = rendererSerialized.FindProperty("m_RendererFeatures");
            if (rendererFeatures != null && !Contains(rendererFeatures, feature))
            {
                rendererFeatures.InsertArrayElementAtIndex(rendererFeatures.arraySize);
                rendererFeatures.GetArrayElementAtIndex(rendererFeatures.arraySize - 1).objectReferenceValue = feature;
            }

            rendererSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rendererData);
        }

        static void ConfigureRendererDepthPath(UniversalRendererData rendererData)
        {
            var serialized = new SerializedObject(rendererData);
            SetInt(serialized, "m_DepthPrimingMode", 0);
            SetInt(serialized, "m_CopyDepthMode", 0);
            SetInt(serialized, "m_IntermediateTextureMode", 1);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rendererData);
        }

        static void EnsureCloudVolume(string scenePath)
        {
            if (!File.Exists(scenePath))
                return;

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Directory.CreateDirectory(VolumeProfileFolder);
            var volumeObject = GameObject.Find("Global Volumetric Clouds");
            if (volumeObject == null)
                volumeObject = new GameObject("Global Volumetric Clouds");

            var volume = volumeObject.GetComponent<Volume>();
            if (volume == null)
                volume = volumeObject.AddComponent<Volume>();

            volume.isGlobal = true;
            volume.priority = 20f;

            if (volume.sharedProfile == null)
            {
                var profilePath = $"{VolumeProfileFolder}/{Path.GetFileNameWithoutExtension(scenePath)}_VolumetricCloudsProfile.asset";
                var profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.name = $"{Path.GetFileNameWithoutExtension(scenePath)} Volumetric Clouds";
                volume.sharedProfile = profile;
                AssetDatabase.CreateAsset(profile, profilePath);
            }

            volume.sharedProfile.components.RemoveAll(component => component == null);
            if (!volume.sharedProfile.TryGet(out VolumetricClouds clouds))
            {
                clouds = volume.sharedProfile.Add<VolumetricClouds>(true);
                clouds.name = "VolumetricClouds";
                AssetDatabase.AddObjectToAsset(clouds, volume.sharedProfile);
            }

            ConfigureClouds(clouds);
            EditorUtility.SetDirty(clouds);
            EditorUtility.SetDirty(volume.sharedProfile);
            EditorUtility.SetDirty(volume);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static void ConfigureClouds(VolumetricClouds clouds)
        {
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
            clouds.shapeScale.value = 7.0f;
            clouds.erosionFactor.overrideState = true;
            clouds.erosionFactor.value = 0.48f;
            clouds.erosionScale.overrideState = true;
            clouds.erosionScale.value = 80f;
            clouds.bottomAltitude.overrideState = true;
            clouds.bottomAltitude.value = 18f;
            clouds.altitudeRange.overrideState = true;
            clouds.altitudeRange.value = 55f;
            clouds.earthCurvature.overrideState = true;
            clouds.earthCurvature.value = 0f;
            clouds.globalSpeed.overrideState = true;
            clouds.globalSpeed.value = 4f;
            clouds.globalOrientation.overrideState = true;
            clouds.globalOrientation.value = 35f;
        }

        static bool Contains(SerializedProperty array, Object value)
        {
            for (var i = 0; i < array.arraySize; i++)
            {
                if (array.GetArrayElementAtIndex(i).objectReferenceValue == value)
                    return true;
            }

            return false;
        }

        static void SetObject(SerializedObject serialized, string name, Object value)
        {
            var property = serialized.FindProperty(name);
            if (property != null)
                property.objectReferenceValue = value;
        }

        static void SetBool(SerializedObject serialized, string name, bool value)
        {
            var property = serialized.FindProperty(name);
            if (property != null)
                property.boolValue = value;
        }

        static void SetFloat(SerializedObject serialized, string name, float value)
        {
            var property = serialized.FindProperty(name);
            if (property != null)
                property.floatValue = value;
        }

        static void SetInt(SerializedObject serialized, string name, int value)
        {
            var property = serialized.FindProperty(name);
            if (property != null)
                property.enumValueIndex = value;
        }
    }
}
