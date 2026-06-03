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

        // ── VoxelToggleButton (left of the Planes button) ─────────────────
        var voxelBtn   = CreateButton("VoxelToggleButton", "Voxel", canvas.transform, 110f, 110f, 24f);
        var voxelBtnRt = voxelBtn.GetComponent<RectTransform>();
        voxelBtnRt.anchorMin        = new Vector2(1f, 1f);
        voxelBtnRt.anchorMax        = new Vector2(1f, 1f);
        voxelBtnRt.pivot            = new Vector2(1f, 1f);
        voxelBtnRt.anchoredPosition = new Vector2(-150f, -20f); // 20 + 110 + 20 left of Planes
        voxelBtnRt.sizeDelta        = new Vector2(110f, 110f);

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

            // 5 × 150 + 4 × 20 spacing = 830
            var subRt     = subPanel.GetComponent<RectTransform>();
            subRt.sizeDelta = new Vector2(830f, 100f);
            subPanel.SetActive(false);
        }

        var scaleBtn     = CreateButton("ScaleButton",     "Scale",  subPanel.transform, 150f, 100f, 30f);
        var placementBtn = CreateButton("PlacementButton", "Move",   subPanel.transform, 150f, 100f, 30f);
        var rotationBtn  = CreateButton("RotationButton",  "Rotate", subPanel.transform, 150f, 100f, 30f);
        var fillBtn      = CreateButton("FillButton",      "Fill",   subPanel.transform, 150f, 100f, 30f);
        var colorBtn     = CreateButton("ColorButton",     "Color",  subPanel.transform, 150f, 100f, 30f);

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
        uiSo.FindProperty("fillButton").objectReferenceValue      = fillBtn;
        uiSo.FindProperty("colorButton").objectReferenceValue     = colorBtn;
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

        // ── ColorPanel (Color mode) ───────────────────────────────────────
        // Selector row (Main/Secondary + preview swatch) on top, then R/G/B
        // slider rows. Same stretch-anchored row scheme as ScalePanel.
        var colorPanel = CreateOrFind("ColorPanel", canvas.transform);
        {
            var rt       = colorPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot     = new Vector2(0.5f, 0f);
            rt.offsetMin = new Vector2(20f,  210f);
            rt.offsetMax = new Vector2(-20f, 680f);

            var vlg = colorPanel.GetComponent<VerticalLayoutGroup>();
            if (vlg != null) Object.DestroyImmediate(vlg);
            colorPanel.SetActive(false);
        }

        // Selector row (top): Main / Secondary buttons + a colour preview swatch
        var selectorRow = CreateOrFind("ColorSelectorRow", colorPanel.transform);
        {
            var rt       = selectorRow.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot     = new Vector2(0.5f, 0f);
            rt.offsetMin = new Vector2(20f,  340f);
            rt.offsetMax = new Vector2(-20f, 440f);

            var hlg = selectorRow.GetComponent<HorizontalLayoutGroup>() ?? selectorRow.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment         = TextAnchor.MiddleCenter;
            hlg.spacing                = 16f;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = false;
            hlg.padding                = new RectOffset(10, 10, 0, 0);
        }
        var mainColorBtn      = CreateButton("MainColorButton",      "Main",      selectorRow.transform, 320f, 90f, 32f);
        var secondaryColorBtn = CreateButton("SecondaryColorButton", "Secondary", selectorRow.transform, 320f, 90f, 32f);
        var previewGO = CreateOrFind("ColorPreview", selectorRow.transform);
        previewGO.GetComponent<RectTransform>().sizeDelta = new Vector2(120f, 90f);
        var previewImg = previewGO.GetComponent<Image>() ?? previewGO.AddComponent<Image>();

        TextMeshProUGUI rLbl, gLbl, bLbl;
        var rSlider = CreateSliderRow("ColorRRow", "R 0.00", colorPanel.transform, out rLbl, 230f, 330f);
        var gSlider = CreateSliderRow("ColorGRow", "G 0.00", colorPanel.transform, out gLbl, 120f, 220f);
        var bSlider = CreateSliderRow("ColorBRow", "B 0.00", colorPanel.transform, out bLbl,  10f, 110f);

        var mainMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/KitchenMainMaterial.mat");
        var secMat  = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/KitchenSecondaryMaterial.mat");
        if (mainMat == null || secMat == null)
            Debug.LogWarning("[UISceneSetup] Kitchen materials not found under Assets/Materials — wire them manually.");

        var colorUI = canvas.GetComponent<MaterialColorUI>() ?? canvas.gameObject.AddComponent<MaterialColorUI>();
        var colorSo = new SerializedObject(colorUI);
        colorSo.FindProperty("stateManager").objectReferenceValue      = xrOrigin.GetComponent<VoxelStateManager>();
        colorSo.FindProperty("colorPanel").objectReferenceValue        = colorPanel;
        colorSo.FindProperty("mainMaterial").objectReferenceValue      = mainMat;
        colorSo.FindProperty("secondaryMaterial").objectReferenceValue = secMat;
        colorSo.FindProperty("mainButton").objectReferenceValue        = mainColorBtn;
        colorSo.FindProperty("secondaryButton").objectReferenceValue   = secondaryColorBtn;
        colorSo.FindProperty("previewSwatch").objectReferenceValue     = previewImg;
        colorSo.FindProperty("redSlider").objectReferenceValue         = rSlider;
        colorSo.FindProperty("greenSlider").objectReferenceValue       = gSlider;
        colorSo.FindProperty("blueSlider").objectReferenceValue        = bSlider;
        colorSo.FindProperty("redLabel").objectReferenceValue          = rLbl;
        colorSo.FindProperty("greenLabel").objectReferenceValue        = gLbl;
        colorSo.FindProperty("blueLabel").objectReferenceValue         = bLbl;
        colorSo.ApplyModifiedProperties();

        // ── CatalogPanel (FillKitchen mode) ───────────────────────────────
        // One definition per standard model, grouped Storage → Washing → Cooking.
        var defPaths = new[]
        {
            "S1 Fridge", "S2 Fridge", "S3 Fridge", "S4 Fridge",
            "W1 Sink", "W2 Sink", "W3 Sink", "W4 Sink",
            "C1 Stove", "C2 Stove", "C3 Stove",
        };
        var defs = new KitchenElementDefinition[defPaths.Length];
        bool defsOk = true;
        for (int i = 0; i < defPaths.Length; i++)
        {
            defs[i] = AssetDatabase.LoadAssetAtPath<KitchenElementDefinition>(
                $"Assets/Scripts/Kitchen/Definitions/{defPaths[i]}.asset");
            if (defs[i] == null) { Debug.LogWarning($"[UISceneSetup] Missing definition '{defPaths[i]}'. Run 'Create Default Kitchen Definitions' first."); defsOk = false; }
        }

        var catalogPanel = CreateOrFind("CatalogPanel", canvas.transform);
        {
            var rt       = catalogPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot     = new Vector2(0.5f, 0f);
            rt.offsetMin = new Vector2(10f,  210f);
            rt.offsetMax = new Vector2(-10f, 590f);

            // 11 models fit a 4-column grid (3 rows). No scrolling needed.
            var hlg = catalogPanel.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null) Object.DestroyImmediate(hlg);
            var grid = catalogPanel.GetComponent<GridLayoutGroup>() ?? catalogPanel.AddComponent<GridLayoutGroup>();
            grid.cellSize       = new Vector2(246f, 108f);
            grid.spacing        = new Vector2(14f, 14f);
            grid.padding        = new RectOffset(10, 10, 10, 10);
            grid.startCorner    = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis      = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint     = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            catalogPanel.SetActive(false);
        }

        // Clear old buttons so re-runs (incl. the legacy 4-button strip) rebuild cleanly.
        for (int i = catalogPanel.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(catalogPanel.transform.GetChild(i).gameObject);

        var catalogButtons = new Button[defs.Length];
        var catalogLabels  = new TextMeshProUGUI[defs.Length];
        for (int i = 0; i < defs.Length; i++)
        {
            string title = defsOk ? $"{defs[i].Code} {defs[i].DisplayName}" : $"Slot {i}";
            var btn = CreateButton($"CatalogButton_{(defsOk ? defs[i].Code : i.ToString())}", title, catalogPanel.transform, 246f, 108f, 26f);
            // Tint by group so Storage/Washing/Cooking read as distinct bands.
            if (defsOk)
            {
                var c = defs[i].Color;
                btn.GetComponent<Image>().color = new Color(c.r * 0.5f, c.g * 0.5f, c.b * 0.5f, 0.9f);
            }
            catalogButtons[i] = btn;
            catalogLabels[i]  = btn.GetComponentInChildren<TextMeshProUGUI>();
        }

        // RemainingLabel — left portion of row above the grid (y 600..680)
        var remainingGO = CreateOrFind("RemainingLabel", canvas.transform);
        {
            var rt       = remainingGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot     = new Vector2(0.5f, 0f);
            rt.offsetMin = new Vector2(20f,  600f);
            rt.offsetMax = new Vector2(-320f, 680f);
            remainingGO.SetActive(false);
        }
        var remainingTmp = remainingGO.GetComponent<TextMeshProUGUI>() ?? remainingGO.AddComponent<TextMeshProUGUI>();
        remainingTmp.text      = "0.0 m free";
        remainingTmp.fontSize  = 48f;
        remainingTmp.alignment = TextAlignmentOptions.Center;
        remainingTmp.color     = Color.white;

        // RemoveLastButton — right side of the readout row
        var removeBtn = CreateButton("RemoveLastButton", "Remove last", canvas.transform, 280f, 80f, 28f);
        {
            var rt = removeBtn.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(1f, 0f);
            rt.anchorMax        = new Vector2(1f, 0f);
            rt.pivot            = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-20f, 610f);
            rt.sizeDelta        = new Vector2(280f, 80f);
            removeBtn.gameObject.SetActive(false);
        }

        // Toast — warning bar above the readout row (y 700..800)
        var toastGO = CreateOrFind("KitchenToast", canvas.transform);
        {
            var rt       = toastGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot     = new Vector2(0.5f, 0f);
            rt.offsetMin = new Vector2(40f,  700f);
            rt.offsetMax = new Vector2(-40f, 800f);
            toastGO.SetActive(false);
        }
        var toastBg = toastGO.GetComponent<Image>() ?? toastGO.AddComponent<Image>();
        toastBg.color = new Color(0.6f, 0.2f, 0.2f, 0.85f);

        var toastLabelGO = CreateOrFind("Label", toastGO.transform);
        var toastLabelRt = toastLabelGO.GetComponent<RectTransform>();
        toastLabelRt.anchorMin = Vector2.zero;
        toastLabelRt.anchorMax = Vector2.one;
        toastLabelRt.offsetMin = new Vector2(20f, 0f);
        toastLabelRt.offsetMax = new Vector2(-20f, 0f);
        var toastTmp = toastLabelGO.GetComponent<TextMeshProUGUI>() ?? toastLabelGO.AddComponent<TextMeshProUGUI>();
        toastTmp.text      = "";
        toastTmp.fontSize  = 32f;
        toastTmp.alignment = TextAlignmentOptions.Center;
        toastTmp.color     = Color.white;

        var catalogUI = canvas.GetComponent<KitchenCatalogUI>() ?? canvas.gameObject.AddComponent<KitchenCatalogUI>();
        var catSo = new SerializedObject(catalogUI);
        catSo.FindProperty("stateManager").objectReferenceValue   = xrOrigin.GetComponent<VoxelStateManager>();
        catSo.FindProperty("catalogPanel").objectReferenceValue   = catalogPanel;
        // MandatoryBanner — top-of-screen warning bar, left of the Planes button
        var bannerGO = CreateOrFind("MandatoryBanner", canvas.transform);
        {
            var rt       = bannerGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(20f, -130f);
            rt.offsetMax = new Vector2(-150f, -20f);
            bannerGO.SetActive(false);
        }
        var bannerBg = bannerGO.GetComponent<Image>() ?? bannerGO.AddComponent<Image>();
        bannerBg.color = new Color(0.85f, 0.55f, 0.1f, 0.85f);

        var bannerLabelGO = CreateOrFind("Label", bannerGO.transform);
        var bannerLabelRt = bannerLabelGO.GetComponent<RectTransform>();
        bannerLabelRt.anchorMin = Vector2.zero;
        bannerLabelRt.anchorMax = Vector2.one;
        bannerLabelRt.offsetMin = new Vector2(20f, 0f);
        bannerLabelRt.offsetMax = new Vector2(-20f, 0f);
        var bannerTmp = bannerLabelGO.GetComponent<TextMeshProUGUI>() ?? bannerLabelGO.AddComponent<TextMeshProUGUI>();
        bannerTmp.text      = "";
        bannerTmp.fontSize  = 36f;
        bannerTmp.alignment = TextAlignmentOptions.Center;
        bannerTmp.color     = Color.white;

        catSo.FindProperty("remainingLabel").objectReferenceValue       = remainingTmp;
        catSo.FindProperty("removeLastButton").objectReferenceValue     = removeBtn;
        catSo.FindProperty("toast").objectReferenceValue                = toastGO;
        catSo.FindProperty("toastLabel").objectReferenceValue           = toastTmp;
        catSo.FindProperty("mandatoryBanner").objectReferenceValue      = bannerGO;
        catSo.FindProperty("mandatoryBannerLabel").objectReferenceValue = bannerTmp;
        var entriesProp = catSo.FindProperty("entries");
        entriesProp.arraySize = defs.Length;
        for (int i = 0; i < defs.Length; i++)
        {
            var elem = entriesProp.GetArrayElementAtIndex(i);
            elem.FindPropertyRelative("definition").objectReferenceValue = defs[i];
            elem.FindPropertyRelative("button").objectReferenceValue     = catalogButtons[i];
            elem.FindPropertyRelative("label").objectReferenceValue      = catalogLabels[i];
        }
        catSo.ApplyModifiedProperties();

        // ── PlaneToggleUI (on Canvas) ─────────────────────────────────────
        var planeManager = xrOrigin.GetComponentInChildren<ARPlaneManager>();
        if (planeManager == null)
            Debug.LogWarning("[UISceneSetup] ARPlaneManager not found on XR Origin — wire it manually.");

        var ptUI = canvas.GetComponent<PlaneToggleUI>() ?? canvas.gameObject.AddComponent<PlaneToggleUI>();
        var ptSo = new SerializedObject(ptUI);
        ptSo.FindProperty("planeManager").objectReferenceValue = planeManager;
        ptSo.FindProperty("toggleButton").objectReferenceValue = planeBtn;
        ptSo.ApplyModifiedProperties();

        // ── VoxelToggleUI (on Canvas) ─────────────────────────────────────
        var vtUI = canvas.GetComponent<VoxelToggleUI>() ?? canvas.gameObject.AddComponent<VoxelToggleUI>();
        var vtSo = new SerializedObject(vtUI);
        vtSo.FindProperty("stateManager").objectReferenceValue = xrOrigin.GetComponent<VoxelStateManager>();
        vtSo.FindProperty("toggleButton").objectReferenceValue = voxelBtn;
        vtSo.ApplyModifiedProperties();

        // ── VariantPanel (FillKitchen mode: tap a placed element) ─────────
        // Horizontal strip of variant buttons, shown only while an element is
        // selected. Sits above the catalog readout row so it doesn't overlap.
        var variantPanel = CreateOrFind("VariantPanel", canvas.transform);
        {
            var rt       = variantPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot     = new Vector2(0.5f, 0f);
            rt.offsetMin = new Vector2(20f,  810f);
            rt.offsetMax = new Vector2(-20f, 940f);

            var bg = variantPanel.GetComponent<Image>() ?? variantPanel.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

            var hlg = variantPanel.GetComponent<HorizontalLayoutGroup>() ?? variantPanel.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment         = TextAnchor.MiddleCenter;
            hlg.spacing                = 16f;
            hlg.childControlWidth       = false;
            hlg.childControlHeight      = false;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = false;
            hlg.padding                = new RectOffset(16, 16, 14, 14);
            variantPanel.SetActive(false);
        }

        // Template cloned once per variant at runtime; kept inactive in the scene.
        var variantTemplate = CreateButton("VariantButtonTemplate", "Variant", variantPanel.transform, 240f, 100f, 28f);
        variantTemplate.gameObject.SetActive(false);

        var variantUI = canvas.GetComponent<ElementVariantUI>() ?? canvas.gameObject.AddComponent<ElementVariantUI>();
        var variantSo = new SerializedObject(variantUI);
        variantSo.FindProperty("stateManager").objectReferenceValue    = xrOrigin.GetComponent<VoxelStateManager>();
        variantSo.FindProperty("arCamera").objectReferenceValue        = xrOrigin.GetComponentInChildren<Camera>();
        variantSo.FindProperty("variantPanel").objectReferenceValue    = variantPanel;
        variantSo.FindProperty("buttonContainer").objectReferenceValue = variantPanel.transform;
        variantSo.FindProperty("buttonTemplate").objectReferenceValue  = variantTemplate;
        variantSo.ApplyModifiedProperties();

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
