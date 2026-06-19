using System.Text;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RhythmParkour.Editor
{
    public static class NeoCityAssetDiagnostics
    {
        const string KitRoot = "Assets/External/KitBash3D/NeoCity/neocity";
        const string ScenePath = "Assets/Scenes/NeoCityAssetVisibilityTest.unity";
        const string ReportPath = "Assets/Editor/RhythmParkour/NeoCityAssetVisibilityReport.txt";
        const string RequestPath = "Assets/Editor/RhythmParkour/DiagnoseNeoCityAssets.request";

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
        static void DiagnoseIfRequested()
        {
            if (!File.Exists(RequestPath))
                return;

            EditorApplication.delayCall += () =>
            {
                DiagnoseSceneInstances();
                AssetDatabase.DeleteAsset(RequestPath);
                AssetDatabase.Refresh();
            };
        }

        [MenuItem("Rhythm Parkour/Diagnose Neo City Assets")]
        public static void Diagnose()
        {
            var log = new StringBuilder();
            foreach (var path in BuildingPaths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                log.AppendLine($"ASSET {path}");
                if (prefab == null)
                {
                    log.AppendLine("  missing prefab");
                    continue;
                }

                var renderers = prefab.GetComponentsInChildren<Renderer>(true);
                log.AppendLine($"  renderers={renderers.Length}");
                foreach (var renderer in renderers)
                {
                    var bounds = renderer.bounds;
                    log.AppendLine($"  renderer={renderer.name} enabled={renderer.enabled} active={renderer.gameObject.activeInHierarchy} bounds.center={bounds.center} bounds.size={bounds.size} mats={renderer.sharedMaterials.Length}");
                    for (var i = 0; i < renderer.sharedMaterials.Length; i++)
                    {
                        var material = renderer.sharedMaterials[i];
                        if (material == null)
                        {
                            log.AppendLine($"    mat[{i}]=null");
                            continue;
                        }

                        var shaderName = material.shader != null ? material.shader.name : "null";
                        var queue = material.renderQueue;
                        var baseColor = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : material.HasProperty("_Color") ? material.GetColor("_Color") : Color.clear;
                        var surface = material.HasProperty("_Surface") ? material.GetFloat("_Surface") : -1f;
                        var alphaClip = material.HasProperty("_AlphaClip") ? material.GetFloat("_AlphaClip") : -1f;
                        var baseMap = material.HasProperty("_BaseMap") ? material.GetTexture("_BaseMap") : material.HasProperty("_MainTex") ? material.GetTexture("_MainTex") : null;
                        log.AppendLine($"    mat[{i}]={material.name} shader={shaderName} queue={queue} surface={surface} alphaClip={alphaClip} color={baseColor} baseMap={(baseMap != null ? baseMap.name : "null")}");
                    }
                }
            }

            Debug.Log(log.ToString());
        }

        [MenuItem("Rhythm Parkour/Diagnose Neo City Test Scene Instances")]
        public static void DiagnoseSceneInstances()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var log = new StringBuilder();
            log.AppendLine($"SCENE {scene.path}");

            foreach (var root in scene.GetRootGameObjects())
                AppendGameObject(log, root, 0);

            var reportAbsolutePath = Path.GetFullPath(ReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(reportAbsolutePath));
            File.WriteAllText(reportAbsolutePath, log.ToString());
            Debug.Log($"[RhythmParkour] Wrote Neo City asset visibility report to {ReportPath}.");
        }

        static void AppendGameObject(StringBuilder log, GameObject gameObject, int depth)
        {
            var indent = new string(' ', depth * 2);
            log.AppendLine($"{indent}GO name={gameObject.name} activeSelf={gameObject.activeSelf} layer={gameObject.layer} position={gameObject.transform.position} scale={gameObject.transform.lossyScale}");

            var meshFilter = gameObject.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                var mesh = meshFilter.sharedMesh;
                log.AppendLine($"{indent}  MeshFilter mesh={(mesh != null ? mesh.name : "null")} vertices={(mesh != null ? mesh.vertexCount : 0)} bounds={(mesh != null ? mesh.bounds.ToString() : "null")}");
            }

            var renderer = gameObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                log.AppendLine($"{indent}  Renderer type={renderer.GetType().Name} enabled={renderer.enabled} visible={renderer.isVisible} bounds.center={renderer.bounds.center} bounds.size={renderer.bounds.size} shadow={renderer.shadowCastingMode} receive={renderer.receiveShadows} materials={renderer.sharedMaterials.Length}");
                for (var i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    var material = renderer.sharedMaterials[i];
                    log.AppendLine($"{indent}    mat[{i}]={(material != null ? material.name : "null")} shader={(material != null && material.shader != null ? material.shader.name : "null")} queue={(material != null ? material.renderQueue : -1)} color={GetColor(material)}");
                }
            }

            var lodGroup = gameObject.GetComponent<LODGroup>();
            if (lodGroup != null)
            {
                var lods = lodGroup.GetLODs();
                log.AppendLine($"{indent}  LODGroup lodCount={lods.Length} size={lodGroup.size} localReference={lodGroup.localReferencePoint}");
                for (var i = 0; i < lods.Length; i++)
                    log.AppendLine($"{indent}    lod[{i}] screenHeight={lods[i].screenRelativeTransitionHeight} renderers={lods[i].renderers.Length}");
            }

            foreach (Transform child in gameObject.transform)
                AppendGameObject(log, child.gameObject, depth + 1);
        }

        static Color GetColor(Material material)
        {
            if (material == null)
                return Color.clear;
            if (material.HasProperty("_BaseColor"))
                return material.GetColor("_BaseColor");
            if (material.HasProperty("_Color"))
                return material.GetColor("_Color");
            return Color.clear;
        }
    }
}
