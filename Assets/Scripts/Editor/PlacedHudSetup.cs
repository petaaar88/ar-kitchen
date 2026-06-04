using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine.XR.ARFoundation;
using ArKitchen.UI;

/// <summary>
/// Adds the new UI-Toolkit placed-kitchen HUD (Voxel / Planes render toggles +
/// Kitchen-space card + Edit button) and retires the old uGUI versions of those
/// three controls so they don't double up. Run via Tools > AR Kitchen > Setup
/// Placed HUD. Safe to re-run.
/// </summary>
public static class PlacedHudSetup
{
    const string PanelSettingsPath = "Assets/UI/PanelSettings.asset";
    const string UxmlPath          = "Assets/UI/Documents/PlacedHud.uxml";

    [MenuItem("Tools/AR Kitchen/Setup Placed HUD")]
    public static void SetupPlacedHud()
    {
        var xrOrigin = GameObject.Find("XR Origin (AR)");
        if (xrOrigin == null) { Debug.LogError("[PlacedHudSetup] XR Origin (AR) not found."); return; }

        var stateManager = xrOrigin.GetComponent<VoxelStateManager>();
        var planeManager = xrOrigin.GetComponentInChildren<ARPlaneManager>(true);
        if (stateManager == null) { Debug.LogError("[PlacedHudSetup] VoxelStateManager missing — run Setup UI first."); return; }

        var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
        var uxml          = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
        if (panelSettings == null || uxml == null)
        {
            Debug.LogError($"[PlacedHudSetup] Missing asset(s): {PanelSettingsPath} / {UxmlPath}");
            return;
        }

        // ── PlacedHudUI (UIDocument) ─────────────────────────────────────
        var hudGO = GameObject.Find("PlacedHudUI");
        if (hudGO == null)
        {
            hudGO = new GameObject("PlacedHudUI");
            Undo.RegisterCreatedObjectUndo(hudGO, "Create PlacedHudUI");
        }

        var doc = hudGO.GetComponent<UIDocument>() ?? hudGO.AddComponent<UIDocument>();
        var docSo = new SerializedObject(doc);
        docSo.FindProperty("m_PanelSettings").objectReferenceValue = panelSettings;
        docSo.FindProperty("sourceAsset").objectReferenceValue     = uxml;
        // Above the onboarding panels (Scanning = 1) so it layers cleanly.
        var sortProp = docSo.FindProperty("m_SortingOrder");
        if (sortProp != null) sortProp.floatValue = 2f;
        docSo.ApplyModifiedProperties();

        var panel = hudGO.GetComponent<PlacedHudPanel>() ?? hudGO.AddComponent<PlacedHudPanel>();
        var panelSo = new SerializedObject(panel);
        panelSo.FindProperty("stateManager").objectReferenceValue = stateManager;
        panelSo.FindProperty("planeManager").objectReferenceValue = planeManager;
        panelSo.ApplyModifiedProperties();

        // ── Retire the old uGUI controls this HUD replaces ───────────────
        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            HideChild(canvas.transform, "VoxelToggleButton");
            HideChild(canvas.transform, "PlaneToggleButton");

            // Detach the old Edit button so KitchenUI stops showing it; Done +
            // the sub-button panels stay for the editing session.
            var editPanel = canvas.transform.Find("EditPanel");
            if (editPanel != null)
            {
                var editBtn = editPanel.Find("EditButton");
                if (editBtn != null) editBtn.gameObject.SetActive(false);
            }

            var kitchenUI = canvas.GetComponent<KitchenUI>();
            if (kitchenUI != null)
            {
                var kSo = new SerializedObject(kitchenUI);
                kSo.FindProperty("editButton").objectReferenceValue = null;
                kSo.ApplyModifiedProperties();
            }
        }
        else
        {
            Debug.LogWarning("[PlacedHudSetup] No uGUI Canvas found — skipped retiring old controls.");
        }

        EditorUtility.SetDirty(hudGO);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[PlacedHudSetup] Placed HUD setup complete. Save the scene (Ctrl+S).");
    }

    static void HideChild(Transform parent, string name)
    {
        var t = parent.Find(name);
        if (t != null) t.gameObject.SetActive(false);
    }
}
