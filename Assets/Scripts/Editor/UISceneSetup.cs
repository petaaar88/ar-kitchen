using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// One-time menu item to create the Kitchen AR UI in MainScene.
/// Run via Tools > AR Kitchen > Setup UI.
/// </summary>
public static class UISceneSetup
{
    [MenuItem("Tools/AR Kitchen/Setup UI")]
    public static void SetupUI()
    {
        // ── VoxelStateManager ────────────────────────────────────────────
        var xrOrigin = GameObject.Find("XR Origin (AR)");
        if (xrOrigin == null) { Debug.LogError("[UISceneSetup] XR Origin (AR) not found."); return; }

        if (xrOrigin.GetComponent<VoxelStateManager>() == null)
        {
            var sm = xrOrigin.AddComponent<VoxelStateManager>();
            // Wire placer field via SerializedObject so it survives domain reload
            var so = new SerializedObject(sm);
            so.FindProperty("placer").objectReferenceValue = xrOrigin.GetComponent<VoxelPlacer>();
            so.ApplyModifiedProperties();
            Debug.Log("[UISceneSetup] Added VoxelStateManager to XR Origin.");
        }

        // ── Canvas ───────────────────────────────────────────────────────
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            var canvasGO = new GameObject("Canvas");
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");

            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();
            Debug.Log("[UISceneSetup] Created Canvas.");
        }

        // ── EditPanel ─────────────────────────────────────────────────────
        var editPanel = CreateOrFind("EditPanel", canvas.transform);
        {
            var rt = editPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot     = new Vector2(0.5f, 0f);
            rt.offsetMin = new Vector2(0f, 40f);
            rt.offsetMax = new Vector2(0f, 200f);

            if (editPanel.GetComponent<HorizontalLayoutGroup>() == null)
            {
                var hlg = editPanel.AddComponent<HorizontalLayoutGroup>();
                hlg.childAlignment    = TextAnchor.MiddleCenter;
                hlg.spacing           = 20f;
                hlg.childForceExpandWidth  = false;
                hlg.childForceExpandHeight = false;
                hlg.padding = new RectOffset(20, 20, 0, 0);
            }
        }

        // ── Edit button ───────────────────────────────────────────────────
        var editBtn = CreateButton("EditButton", "Edit", editPanel.transform);

        // ── Done button ───────────────────────────────────────────────────
        var doneBtn = CreateButton("DoneButton", "Done", editPanel.transform);
        doneBtn.gameObject.SetActive(false);

        // ── SubButtonPanel ────────────────────────────────────────────────
        var subPanel = CreateOrFind("SubButtonPanel", editPanel.transform);
        {
            if (subPanel.GetComponent<HorizontalLayoutGroup>() == null)
            {
                var hlg = subPanel.AddComponent<HorizontalLayoutGroup>();
                hlg.childAlignment    = TextAnchor.MiddleCenter;
                hlg.spacing           = 16f;
                hlg.childForceExpandWidth  = false;
                hlg.childForceExpandHeight = false;
            }
            var subRt = subPanel.GetComponent<RectTransform>();
            subRt.sizeDelta = new Vector2(400f, 120f);
            subPanel.SetActive(false);
        }

        var scaleBtn     = CreateButton("ScaleButton",     "Scale",  subPanel.transform);
        var placementBtn = CreateButton("PlacementButton", "Move",   subPanel.transform);
        var rotationBtn  = CreateButton("RotationButton",  "Rotate", subPanel.transform);

        // ── KitchenUI component (on Canvas) ───────────────────────────────
        var ui = canvas.GetComponent<KitchenUI>();
        if (ui == null) ui = canvas.gameObject.AddComponent<KitchenUI>();

        var uiSo = new SerializedObject(ui);
        uiSo.FindProperty("stateManager").objectReferenceValue =
            xrOrigin.GetComponent<VoxelStateManager>();
        uiSo.FindProperty("editPanel").objectReferenceValue      = editPanel;
        uiSo.FindProperty("subButtonPanel").objectReferenceValue = subPanel;
        uiSo.FindProperty("editButton").objectReferenceValue     = editBtn;
        uiSo.FindProperty("doneButton").objectReferenceValue     = doneBtn;
        uiSo.FindProperty("scaleButton").objectReferenceValue    = scaleBtn;
        uiSo.FindProperty("placementButton").objectReferenceValue = placementBtn;
        uiSo.FindProperty("rotationButton").objectReferenceValue  = rotationBtn;
        uiSo.ApplyModifiedProperties();

        EditorUtility.SetDirty(canvas.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[UISceneSetup] UI setup complete. Save the scene (Ctrl+S).");
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    static GameObject CreateOrFind(string name, Transform parent)
    {
        var t = parent.Find(name);
        if (t != null) return t.gameObject;

        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    static Button CreateButton(string name, string label, Transform parent)
    {
        var existing = parent.Find(name);
        if (existing != null) return existing.GetComponent<Button>();

        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);

        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(160f, 80f);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 0.85f);
        colors.pressedColor     = new Color(0.05f, 0.05f, 0.05f, 0.85f);
        btn.colors = colors;

        // Label
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        var labelRt = labelGO.AddComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;

        var tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 36f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;

        return btn;
    }
}
