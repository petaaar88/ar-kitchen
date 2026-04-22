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

        // ── ScalePanel ────────────────────────────────────────────────────
        var scalePanel = CreateOrFind("ScalePanel", canvas.transform);
        {
            var rt = scalePanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot     = new Vector2(0.5f, 0f);
            rt.offsetMin = new Vector2(20f, 220f);
            rt.offsetMax = new Vector2(-20f, 560f);

            if (scalePanel.GetComponent<VerticalLayoutGroup>() == null)
            {
                var vlg = scalePanel.AddComponent<VerticalLayoutGroup>();
                vlg.childAlignment         = TextAnchor.MiddleCenter;
                vlg.spacing                = 10f;
                vlg.childForceExpandWidth  = true;
                vlg.childForceExpandHeight = false;
                vlg.padding                = new RectOffset(10, 10, 10, 10);
            }
            scalePanel.SetActive(false);
        }

        TextMeshProUGUI widthLbl, depthLbl, heightLbl;
        var widthSlider  = CreateSliderRow("WidthRow",  "W 1.0m", scalePanel.transform, out widthLbl);
        var depthSlider  = CreateSliderRow("DepthRow",  "D 1.0m", scalePanel.transform, out depthLbl);
        var heightSlider = CreateSliderRow("HeightRow", "H 1.0m", scalePanel.transform, out heightLbl);

        // ── VoxelScaleUI (on Canvas) ──────────────────────────────────────
        var scaleUI = canvas.GetComponent<VoxelScaleUI>();
        if (scaleUI == null) scaleUI = canvas.gameObject.AddComponent<VoxelScaleUI>();

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
            var rt = rotPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot     = new Vector2(0.5f, 0f);
            rt.offsetMin = new Vector2(0f, 220f);
            rt.offsetMax = new Vector2(0f, 340f);

            if (rotPanel.GetComponent<HorizontalLayoutGroup>() == null)
            {
                var hlg = rotPanel.AddComponent<HorizontalLayoutGroup>();
                hlg.childAlignment         = TextAnchor.MiddleCenter;
                hlg.spacing                = 40f;
                hlg.childForceExpandWidth  = false;
                hlg.childForceExpandHeight = false;
            }
            rotPanel.SetActive(false);
        }

        var rotLeftBtn  = CreateButton("RotateLeftButton",  "◄ 15°", rotPanel.transform);
        var rotRightBtn = CreateButton("RotateRightButton", "15° ►", rotPanel.transform);

        // ── VoxelRotationUI (on Canvas) ───────────────────────────────────
        var rotUI = canvas.GetComponent<VoxelRotationUI>();
        if (rotUI == null) rotUI = canvas.gameObject.AddComponent<VoxelRotationUI>();

        var rotSo = new SerializedObject(rotUI);
        rotSo.FindProperty("stateManager").objectReferenceValue    = xrOrigin.GetComponent<VoxelStateManager>();
        rotSo.FindProperty("rotationPanel").objectReferenceValue   = rotPanel;
        rotSo.FindProperty("rotateLeftButton").objectReferenceValue  = rotLeftBtn;
        rotSo.FindProperty("rotateRightButton").objectReferenceValue = rotRightBtn;
        rotSo.ApplyModifiedProperties();

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

    static Slider CreateSliderRow(string rowName, string labelText, Transform parent, out TextMeshProUGUI labelOut)
    {
        var row = CreateOrFind(rowName, parent);
        var rowRt = row.GetComponent<RectTransform>();
        rowRt.sizeDelta = new Vector2(0f, 80f);

        if (row.GetComponent<HorizontalLayoutGroup>() == null)
        {
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment         = TextAnchor.MiddleLeft;
            hlg.spacing                = 10f;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = true;
            hlg.padding                = new RectOffset(10, 10, 0, 0);
        }

        // Label
        var labelGO = CreateOrFind(rowName + "_Label", row.transform);
        var labelRt = labelGO.GetComponent<RectTransform>();
        labelRt.sizeDelta = new Vector2(160f, 0f);
        labelOut = labelGO.GetComponent<TextMeshProUGUI>();
        if (labelOut == null) labelOut = labelGO.AddComponent<TextMeshProUGUI>();
        labelOut.text      = labelText;
        labelOut.fontSize  = 32f;
        labelOut.alignment = TextAlignmentOptions.MidlineLeft;
        labelOut.color     = Color.white;

        // Slider (use Unity default controls)
        var sliderGO = CreateOrFind(rowName + "_Slider", row.transform);
        var sliderRt = sliderGO.GetComponent<RectTransform>();
        sliderRt.sizeDelta = new Vector2(560f, 0f);

        var slider = sliderGO.GetComponent<Slider>();
        if (slider == null)
        {
            // Build minimal slider: background + fill + handle
            var resources = new DefaultControls.Resources();
            var builtSlider = DefaultControls.CreateSlider(resources);
            builtSlider.name = rowName + "_Slider";
            builtSlider.transform.SetParent(row.transform, false);
            var brt = builtSlider.GetComponent<RectTransform>();
            brt.sizeDelta = new Vector2(560f, 40f);
            slider = builtSlider.GetComponent<Slider>();

            // Remove the label placeholder we made since CreateSlider made its own GO
            Object.DestroyImmediate(sliderGO);
        }

        return slider;
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
