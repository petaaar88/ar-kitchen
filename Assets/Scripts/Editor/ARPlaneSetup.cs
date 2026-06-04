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
        // ── Fill material (tiled scanning dots, feathered edges) ─────────
        // Plane mesh UVs are (vertex.x, vertex.z) in metres, so the dot
        // texture tiles in world space — dots stay a constant real-world
        // size regardless of plane size. _Scale 10 ≈ one dot every 0.1 m.
        // The "AR Kitchen/Feathered Plane" shader fades the dots out near
        // each plane edge (ARPlaneFeather feeds it the plane bounds).
        var dotTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/PlaneDots.png");

        const string matPath = "Assets/Materials/ARPlane.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("AR Kitchen/Feathered Plane"));
            mat.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.9f));
            mat.SetFloat("_FeatherWidth", 0.25f);
            if (dotTex != null)
            {
                mat.SetTexture("_BaseMap", dotTex);
                mat.SetTextureScale("_BaseMap", new Vector2(10f, 10f));
            }
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

            // Feeds plane bounds to the feathered shader (no hard border).
            go.AddComponent<ARPlaneFeather>();

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
