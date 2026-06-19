using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RhythmParkour.Editor
{
    public static class URPProjectConfigurator
    {
        private const string RequestPath = "Assets/Editor/RhythmParkour/ConfigureURP.request";
        private const string FolderPath = "Assets/Settings/URP";
        private const string RendererPath = FolderPath + "/VR_URP_Renderer.asset";
        private const string PipelinePath = FolderPath + "/VR_URP_Pipeline.asset";

        [InitializeOnLoadMethod]
        private static void ConfigureIfRequested()
        {
            if (!File.Exists(RequestPath))
            {
                return;
            }

            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                {
                    EditorApplication.delayCall += ConfigureIfRequested;
                    return;
                }

                Configure();
                AssetDatabase.DeleteAsset(RequestPath);
                AssetDatabase.Refresh();
            };
        }

        [MenuItem("Rhythm Parkour/Configure URP Project")]
        public static void Configure()
        {
            Directory.CreateDirectory(FolderPath);

            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, RendererPath);
            }

            var pipelineAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipelineAsset == null)
            {
                pipelineAsset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
                AssetDatabase.CreateAsset(pipelineAsset, PipelinePath);
            }

            ConfigurePipelineAsset(pipelineAsset, rendererData);
            GraphicsSettings.defaultRenderPipeline = pipelineAsset;
            GraphicsSettings.renderPipelineAsset = pipelineAsset;
            QualitySettings.renderPipeline = pipelineAsset;

            EditorUtility.SetDirty(rendererData);
            EditorUtility.SetDirty(pipelineAsset);
            AssetDatabase.SaveAssets();
            Debug.Log($"[RhythmParkour] Configured URP pipeline asset at {PipelinePath}.");
        }

        private static void ConfigurePipelineAsset(UniversalRenderPipelineAsset pipelineAsset, UniversalRendererData rendererData)
        {
            var serialized = new SerializedObject(pipelineAsset);

            SetInt(serialized, "m_MSAA", 4);
            SetBool(serialized, "m_SupportsHDR", true);
            SetBool(serialized, "m_RequireDepthTexture", false);
            SetBool(serialized, "m_RequireOpaqueTexture", false);
            SetBool(serialized, "m_SupportsCameraDepthTexture", false);
            SetBool(serialized, "m_SupportsCameraOpaqueTexture", false);
            SetInt(serialized, "m_DefaultRendererIndex", 0);

            var rendererList = serialized.FindProperty("m_RendererDataList");
            if (rendererList != null)
            {
                rendererList.arraySize = 1;
                rendererList.GetArrayElementAtIndex(0).objectReferenceValue = rendererData;
            }

            var rendererDataProperty = serialized.FindProperty("m_RendererData");
            if (rendererDataProperty != null)
            {
                rendererDataProperty.objectReferenceValue = rendererData;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();

            ConfigureRendererData(rendererData);
        }

        private static void ConfigureRendererData(UniversalRendererData rendererData)
        {
            var serialized = new SerializedObject(rendererData);
            SetInt(serialized, "m_DepthPrimingMode", 0);
            SetInt(serialized, "m_CopyDepthMode", 0);
            SetInt(serialized, "m_IntermediateTextureMode", 1);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBool(SerializedObject serialized, string propertyName, bool value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.boolValue = value;
            }
        }

        private static void SetInt(SerializedObject serialized, string propertyName, int value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.intValue = value;
            }
        }
    }
}
