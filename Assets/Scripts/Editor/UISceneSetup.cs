using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using TMPro;

/// <summary>
/// One-time menu item to create the Kitchen AR UI in MainScene.
/// Run via Tools > AR Kitchen > Setup UI.
/// Safe to re-run — updates existing elements in place.
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
            canvasGO.AddComponent<GraphicRaycaster>();
            Debug.Log("[UISceneSetup] Created Canvas.");
        }

        var scaler = canvas.GetComponent<CanvasScaler>() ?? canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight  = 0f; // match width — keeps canvas always 1080 units wide on portrait

        // ── PlaneToggleButton (top-right) ─────────────────────────────────
        var planeBtn   = CreateButton("PlaneToggleButton", "Planes", canvas.transform, 110f, 110f, 24f);
        var planeBtnRt = planeBtn.GetComponent<RectTransform>();
        planeBtnRt.anchorMin        = new Vector2(1f, 1f);
        planeBtnRt.anchorMax        = new Vector2(1f, 1f);
        planeBtnRt.pivot            = new Vector2(1f, 1f);
        planeBtnRt.anchoredPosition = new Vector2(-20f, -20f);
        planeBtnRt.sizeDelta        = new Vector2(110f, 110f);

        // ── EditPanel (bottom bar) ────────────────────────────────────────
        var editPanel = CreateOrFind("EditPanel", canvas.transform);
        {
            var rt       = editPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot     = new Vector2(0.5f, 0f);
            rt.offsetMin = new Vector2(0f,  20f);
            rt.offsetMax = new Vector2(0f, 200f);

            var hlg = editPanel.GetComponent<HorizontalLayoutGroup>() ?? editPanel.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment         = TextAnchor.MiddleCenter;
            hlg.spacing                = 20f;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = false;
            hlg.padding                = new RectOffset(20, 20, 0, 0);
        }

        var editBtn = CreateButton("EditButton", "Edit", editPanel.transform, 280f, 120f);
        var doneBtn = CreateButton("DoneButton", "Done", editPanel.transform, 280f, 120f);
        doneBtn.gameObject.SetActive(false);

        // ── SubButtonPanel ────────────────────────────────────────────────
        var subPanel = CreateOrFind("SubButtonPanel", editPanel.transform);
        {
            var subHlg = subPanel.GetComponent<HorizontalLayoutGroup>() ?? subPanel.AddComponent<HorizontalLayoutGroup>();
            subHlg.childAlignment         = TextAnchor.MiddleCenter;
            subHlg.spacing                = 20f;
            subHlg.childForceExpandWidth  = false;
            subHlg.childForceExpandHeight = false;

            // 3 × 200 + 2 × 20 spacing = 640
            var subRt     = subPanel.GetComponent<RectTransform>();
            subRt.sizeDelta = new Vector2(640f, 100f);
            subPanel.SetActive(false);
        }

        var scaleBtn     = CreateButton("ScaleButton",     "Scale",  subPanel.transform, 200f, 100f);
        var placementBtn = CreateButton("PlacementButton", "Move",   subPanel.transform, 200f, 100f);
        var rotationBtn  = CreateButton("RotationButton",  "Rotate", subPanel.transform, 200f, 100f);

        // ── KitchenUI (on Canvas) ─────────────────────────────────────────
        var ui = canvas.GetComponent<KitchenUI>() ?? canvas.gameObject.AddComponent<KitchenUI>();
        var uiSo = new SerializedObject(ui);
        uiSo.FindProperty("stateManager").objectReferenceValue    = xrOrigin.GetComponent<VoxelStateManager>();
        uiSo.FindProperty("editPanel").objectReferenceValue       = editPanel;
        uiSo.FindProperty("subButtonPanel").objectReferenceValue  = subPanel;
        uiSo.FindProperty("editButton").objectReferenceValue      = editBtn;
        uiSo.FindProperty("doneButton").objectReferenceValue      = doneBtn;
        uiSo.FindProperty("scaleButton").objectReferenceValue     = scaleBtn;
        uiSo.FindProperty("placementButton").objectReferenceValue = placementBtn;
        uiSo.FindProperty("rotationButton").objectReferenceValue  = rotationBtn;
        uiSo.ApplyModifiedProperties();

        // ── ScalePanel ────────────────────────────────────────────────────
        // No VerticalLayoutGroup — rows use stretch anchors so they always
        // fill the panel width regardless of canvas scaling or aspect ratio.
        // Row height 100, spacing 10, padding 10 → total 3×100 + 2×10 + 2×10 = 340
        var scalePanel = CreateOrFind("ScalePanel", canvas.transform);
        {
            var rt       = scalePanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot     = new Vector2(0.5f, 0f);
            rt.offsetMin = new Vector2(20f,  210f);
            rt.offsetMax = new Vector2(-20f, 560f);

            var vlg = scalePanel.GetComponent<VerticalLayoutGroup>();
            if (vlg != null) Object.DestroyImmediate(vlg);
            scalePanel.SetActive(false);
        }

        // Rows: yMin/yMax relative to ScalePanel bottom (padding 10, spacing 10)
        // Width (top):  270–370    Depth (mid): 140–240    Height (bot): 10–110
        TextMeshProUGUI widthLbl, depthLbl, heightLbl;
        var widthSlider  = CreateSliderRow("WidthRow",  "W 1.0m", scalePanel.transform, out widthLbl,  270f, 370f);
        var depthSlider  = CreateSliderRow("DepthRow",  "D 1.0m", scalePanel.transform, out depthLbl,  140f, 240f);
        var heightSlider = CreateSliderRow("HeightRow", "H 1.0m", scalePanel.transform, out heightLbl,  10f, 110f);

        // ── VoxelScaleUI (on Canvas) ──────────────────────────────────────
        var scaleUI = canvas.GetComponent<VoxelScaleUI>() ?? canvas.gameObject.AddComponent<VoxelScaleUI>();
        var scaleSo = new SerializedObject(scaleUI);
        scaleSo.FindProperty("stateManager").objectReferenceValue  = xrOrigin.GetComponent<VoxelStateManager>();
        scaleSo.FindProperty("scalePanel").objectReferenceValue    = scalePanel;
        scaleSo.FindProperty("widthSlider").objectReferenceValue   = widthSlider;
        scaleSo.FindProperty("depthSlider").objectReferenceValue   = depthSlider;
        scaleSo.FindProperty("heightSlider").objectReferenceValue  = heightSlider;
        scaleSo.FindProperty("widthLabel").objectReferenceValue    = widthLbl;
        scaleSo.FindProperty("depthLabel").objectReferenceValue    = depthLbl;
        scaleSo.FindProperty("heightLabel").objectReferenceValue   = heightLbl;
        scaleSo.ApplyModifiedProperties();

        // ── RotationPanel ─────────────────────────────────────────────────
        var rotPanel = CreateOrFind("RotationPanel", canvas.transform);
        {
            var rt       = rotPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot     = new Vector2(0.5f, 0f);
            rt.offsetMin = new Vector2(0f, 210f);
            rt.offsetMax = new Vector2(0f, 380f);

            var hlg = rotPanel.GetComponent<HorizontalLayoutGroup>() ?? rotPanel.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment         = TextAnchor.MiddleCenter;
            hlg.spacing                = 40f;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = false;
            rotPanel.SetActive(false);
        }

        var rotLeftBtn  = CreateButton("RotateLeftButton",  "◄ 15°", rotPanel.transform, 260f, 120f);
        var rotRightBtn = CreateButton("RotateRightButton", "15° ►", rotPanel.transform, 260f, 120f);

        // ── VoxelRotationUI (on Canvas) ───────────────────────────────────
        var rotUI = canvas.GetComponent<VoxelRotationUI>() ?? canvas.gameObject.AddComponent<VoxelRotationUI>();
        var rotSo = new SerializedObject(rotUI);
        rotSo.FindProperty("stateManager").objectReferenceValue      = xrOrigin.GetComponent<VoxelStateManager>();
        rotSo.FindProperty("rotationPanel").objectReferenceValue     = rotPanel;
        rotSo.FindProperty("rotateLeftButton").objectReferenceValue  = rotLeftBtn;
        rotSo.FindProperty("rotateRightButton").objectReferenceValue = rotRightBtn;
        rotSo.ApplyModifiedProperties();

        // ── PlaneToggleUI (on Canvas) ─────────────────────────────────────
        var planeManager = xrOrigin.GetComponentInChildren<ARPlaneManager>();
        if (planeManager == null)
            Debug.LogWarning("[UISceneSetup] ARPlaneManager not found on XR Origin — wire it manually.");

        var ptUI = canvas.GetComponent<PlaneToggleUI>() ?? canvas.gameObject.AddComponent<PlaneToggleUI>();
        var ptSo = new SerializedObject(ptUI);
        ptSo.FindProperty("planeManager").objectReferenceValue = planeManager;
        ptSo.FindProperty("toggleButton").objectReferenceValue = planeBtn;
        ptSo.ApplyModifiedProperties();

        // Mark every modified object dirty so Unity serializes all changes
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            EditorUtility.SetDirty(go);
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

    static Button CreateButton(string name, string label, Transform parent,
                               float w = 280f, float h = 120f, float fontSize = 36f)
    {
        var existing = parent.Find(name);
        if (existing != null)
        {
            existing.GetComponent<RectTransform>().sizeDelta = new Vector2(w, h);
            var existingTmp = existing.GetComponentInChildren<TextMeshProUGUI>();
            if (existingTmp != null) { existingTmp.text = label; existingTmp.fontSize = fontSize; }
            return existing.GetComponent<Button>();
        }

        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);

        var rt       = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(w, h);

        var img   = go.AddComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);

        var btn    = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 0.85f);
        colors.pressedColor     = new Color(0.05f, 0.05f, 0.05f, 0.85f);
        btn.colors = colors;

        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        var labelRt       = labelGO.AddComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;

        var tmp       = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;

        return btn;
    }

    // yMin/yMax: row bottom and top in local coords of parent (ScalePanel)
    static Slider CreateSliderRow(string rowName, string labelText, Transform parent,
                                  out TextMeshProUGUI labelOut, float yMin, float yMax)
    {
        var row   = CreateOrFind(rowName, parent);
        var rowRt = row.GetComponent<RectTransform>();

        // Stretch anchors: row always fills full panel width, regardless of canvas scale
        rowRt.anchorMin = new Vector2(0f, 0f);
        rowRt.anchorMax = new Vector2(1f, 0f);
        rowRt.pivot     = new Vector2(0.5f, 0f);
        rowRt.offsetMin = new Vector2(20f, yMin);   // 20px left margin
        rowRt.offsetMax = new Vector2(-20f, yMax);  // 20px right margin

        var hlg = row.GetComponent<HorizontalLayoutGroup>() ?? row.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment         = TextAnchor.MiddleLeft;
        hlg.spacing                = 16f;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;
        hlg.padding                = new RectOffset(10, 10, 0, 0);

        // Label
        var labelGO = CreateOrFind(rowName + "_Label", row.transform);
        labelGO.GetComponent<RectTransform>().sizeDelta = new Vector2(180f, 80f);

        labelOut           = labelGO.GetComponent<TextMeshProUGUI>() ?? labelGO.AddComponent<TextMeshProUGUI>();
        labelOut.text      = labelText;
        labelOut.fontSize  = 36f;
        labelOut.alignment = TextAlignmentOptions.MidlineLeft;
        labelOut.color     = Color.white;

        // Slider: width fills the row width minus label, spacing, and padding.
        // Row width = panel_width - 20 - 20 (offsetMin/Max) - 10 - 10 (HLG padding) - 180 (label) - 16 (spacing)
        // But we set an explicit 780px so the slider's internal stretch children always have a concrete size.
        var sliderGO = CreateOrFind(rowName + "_Slider", row.transform);
        var slider   = sliderGO.GetComponent<Slider>();
        GameObject sliderRoot;

        if (slider == null)
        {
            var built = DefaultControls.CreateSlider(new DefaultControls.Resources());
            built.name = rowName + "_Slider";
            built.transform.SetParent(row.transform, false);
            Object.DestroyImmediate(sliderGO);
            slider     = built.GetComponent<Slider>();
            sliderRoot = built;
        }
        else
        {
            sliderRoot = sliderGO;
        }

        sliderRoot.GetComponent<RectTransform>().sizeDelta = new Vector2(780f, 60f);

        return slider;
    }
}
