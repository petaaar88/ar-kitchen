using UnityEngine;
using UnityEditor;
using UnityEngine.XR.ARFoundation;
using UnityEngine.Rendering;

/// <summary>
/// Creates the ARPlane visualization prefab and wires it to ARPlaneManager.
/// Run via Tools > AR Kitchen > Setup AR Plane Visualization.
/// </summary>
public static class ARPlaneSetup
{
    [MenuItem("Tools/AR Kitchen/Setup AR Plane Visualization")]
    public static void SetupARPlaneVisualization()
    {
        // ── Material ─────────────────────────────────────────────────────
        const string matPath = "Assets/Materials/ARPlane.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetColor("_BaseColor", new Color(0.1f, 0.6f, 1f, 0.35f));
            mat.renderQueue = (int)RenderQueue.Transparent;
            AssetDatabase.CreateAsset(mat, matPath);
            Debug.Log("[ARPlaneSetup] Created ARPlane material.");
        }

        // ── Prefab ───────────────────────────────────────────────────────
        const string prefabPath = "Assets/Prefabs/ARPlane.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existing == null)
        {
            var go = new GameObject("ARPlane");
            go.AddComponent<ARPlaneMeshVisualizer>();

            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;

            // Boundary line renderer
            var lr = go.AddComponent<LineRenderer>();
            lr.loop = true;
            lr.startWidth = 0.02f;
            lr.endWidth   = 0.02f;
            lr.sharedMaterial = mat;
            lr.useWorldSpace  = false;

            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            Debug.Log("[ARPlaneSetup] Created ARPlane prefab.");
        }

        // ── Wire to ARPlaneManager ────────────────────────────────────────
        var planeManager = Object.FindFirstObjectByType<ARPlaneManager>();
        if (planeManager != null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var so = new SerializedObject(planeManager);
            so.FindProperty("m_PlanePrefab").objectReferenceValue = prefab;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(planeManager.gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            Debug.Log("[ARPlaneSetup] Wired ARPlane prefab to ARPlaneManager.");
        }
        else
        {
            Debug.LogWarning("[ARPlaneSetup] ARPlaneManager not found in scene.");
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[ARPlaneSetup] Done. Save the scene (Ctrl+S).");
    }
}
