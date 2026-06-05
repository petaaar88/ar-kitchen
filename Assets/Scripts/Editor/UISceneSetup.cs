using ArKitchen.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Creates the runtime AR Kitchen UI as UI Toolkit documents.
/// Safe to re-run: updates existing documents and removes the legacy uGUI Canvas.
/// </summary>
public static class UISceneSetup
{
    const string PanelSettingsPath = "Assets/UI/PanelSettings.asset";
    const string ScanningUxmlPath = "Assets/UI/Documents/Scanning.uxml";
    const string SurfacesUxmlPath = "Assets/UI/Documents/SurfacesFound.uxml";
    const string PlaceUxmlPath = "Assets/UI/Documents/PlaceKitchen.uxml";
    const string PlacedHudUxmlPath = "Assets/UI/Documents/PlacedHud.uxml";
    const string KitchenEditUxmlPath = "Assets/UI/Documents/KitchenEdit.uxml";

    static readonly string[] DefinitionPaths =
    {
        "S1 Fridge", "S2 Fridge", "S3 Fridge", "S4 Fridge",
        "W1 Sink", "W2 Sink", "W3 Sink", "W4 Sink",
        "C1 Stove", "C2 Stove", "C3 Stove",
    };

    [MenuItem("Tools/AR Kitchen/Setup UI")]
    public static void SetupUI()
    {
        var xrOrigin = GameObject.Find("XR Origin (AR)");
        if (xrOrigin == null)
        {
            Debug.LogError("[UISceneSetup] XR Origin (AR) not found.");
            return;
        }

        var placer = xrOrigin.GetComponent<VoxelPlacer>();
        if (placer == null)
        {
            Debug.LogError("[UISceneSetup] VoxelPlacer missing on XR Origin.");
            return;
        }

        var stateManager = xrOrigin.GetComponent<VoxelStateManager>() ?? xrOrigin.AddComponent<VoxelStateManager>();
        var stateSo = new SerializedObject(stateManager);
        stateSo.FindProperty("placer").objectReferenceValue = placer;
        stateSo.ApplyModifiedProperties();

        var planeManager = xrOrigin.GetComponentInChildren<ARPlaneManager>(true);
        var arCamera = xrOrigin.GetComponentInChildren<Camera>(true);
        var uiRoot = EnsureUiRoot();

        var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
        if (panelSettings == null)
        {
            Debug.LogError($"[UISceneSetup] Missing {PanelSettingsPath}.");
            return;
        }

        var scanning = EnsureDocument<ScanningPanel>(uiRoot.transform, "ScanningUI", panelSettings, ScanningUxmlPath, 1);
        var surfaces = EnsureDocument<SurfacesFoundPanel>(uiRoot.transform, "SurfacesFoundUI", panelSettings, SurfacesUxmlPath, 1);
        var place = EnsureDocument<PlaceKitchenPanel>(uiRoot.transform, "PlaceKitchenUI", panelSettings, PlaceUxmlPath, 1);
        var hud = EnsureDocument<PlacedHudPanel>(uiRoot.transform, "PlacedHudUI", panelSettings, PlacedHudUxmlPath, 2);
        var edit = EnsureDocument<KitchenEditPanel>(uiRoot.transform, "KitchenEditUI", panelSettings, KitchenEditUxmlPath, 3);

        WirePlacedHud(hud, stateManager, planeManager);
        WireEditPanel(edit, stateManager, arCamera);
        WireScanFlow(uiRoot.transform, scanning, surfaces, place, placer, planeManager);

        RemoveLegacyCanvas();

        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            EditorUtility.SetDirty(go);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[UISceneSetup] UI Toolkit setup complete. Save the scene (Ctrl+S).");
    }

    static GameObject EnsureUiRoot()
    {
        var root = GameObject.Find("AR Kitchen UI");
        if (root != null) return root;

        root = new GameObject("AR Kitchen UI");
        Undo.RegisterCreatedObjectUndo(root, "Create AR Kitchen UI");
        return root;
    }

    static T EnsureDocument<T>(Transform parent, string objectName, PanelSettings panelSettings, string uxmlPath, int sortingOrder)
        where T : Component
    {
        var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
        if (asset == null)
        {
            Debug.LogError($"[UISceneSetup] Missing {uxmlPath}.");
            return null;
        }

        var go = GameObject.Find(objectName);
        if (go == null)
        {
            go = new GameObject(objectName);
            Undo.RegisterCreatedObjectUndo(go, "Create " + objectName);
        }
        go.transform.SetParent(parent, false);

        var doc = go.GetComponent<UIDocument>() ?? go.AddComponent<UIDocument>();
        var docSo = new SerializedObject(doc);
        docSo.FindProperty("m_PanelSettings").objectReferenceValue = panelSettings;
        docSo.FindProperty("sourceAsset").objectReferenceValue = asset;
        SetSortingOrder(docSo, sortingOrder);
        docSo.ApplyModifiedProperties();

        var component = go.GetComponent<T>() ?? go.AddComponent<T>();
        EditorUtility.SetDirty(go);
        return component;
    }

    static void SetSortingOrder(SerializedObject docSo, int sortingOrder)
    {
        var prop = docSo.FindProperty("m_SortingOrder");
        if (prop == null) return;

        switch (prop.propertyType)
        {
            case SerializedPropertyType.Integer:
                prop.intValue = sortingOrder;
                break;
            case SerializedPropertyType.Float:
                prop.floatValue = sortingOrder;
                break;
        }
    }

    static void WirePlacedHud(PlacedHudPanel hud, VoxelStateManager stateManager, ARPlaneManager planeManager)
    {
        if (hud == null) return;
        var so = new SerializedObject(hud);
        so.FindProperty("stateManager").objectReferenceValue = stateManager;
        so.FindProperty("planeManager").objectReferenceValue = planeManager;
        so.ApplyModifiedProperties();
    }

    static void WireEditPanel(KitchenEditPanel edit, VoxelStateManager stateManager, Camera arCamera)
    {
        if (edit == null) return;
        var so = new SerializedObject(edit);
        so.FindProperty("stateManager").objectReferenceValue = stateManager;
        so.FindProperty("arCamera").objectReferenceValue = arCamera;
        so.FindProperty("mainMaterial").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/KitchenMainMaterial.mat");
        so.FindProperty("secondaryMaterial").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/KitchenSecondaryMaterial.mat");

        var defsProp = so.FindProperty("definitions");
        defsProp.arraySize = DefinitionPaths.Length;
        for (int i = 0; i < DefinitionPaths.Length; i++)
        {
            defsProp.GetArrayElementAtIndex(i).objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<KitchenElementDefinition>(
                    $"Assets/Scripts/Kitchen/Definitions/{DefinitionPaths[i]}.asset");
        }

        so.ApplyModifiedProperties();
    }

    static void WireScanFlow(
        Transform parent,
        ScanningPanel scanning,
        SurfacesFoundPanel surfaces,
        PlaceKitchenPanel place,
        VoxelPlacer placer,
        ARPlaneManager planeManager)
    {
        var flowGo = GameObject.Find("SurfaceScanFlow");
        if (flowGo == null)
        {
            flowGo = new GameObject("SurfaceScanFlow");
            Undo.RegisterCreatedObjectUndo(flowGo, "Create SurfaceScanFlow");
        }
        flowGo.transform.SetParent(parent, false);

        var flow = flowGo.GetComponent<SurfaceScanFlow>() ?? flowGo.AddComponent<SurfaceScanFlow>();
        var so = new SerializedObject(flow);
        so.FindProperty("planeManager").objectReferenceValue = planeManager;
        so.FindProperty("scanningPanel").objectReferenceValue = scanning;
        so.FindProperty("surfacesFoundPanel").objectReferenceValue = surfaces;
        so.FindProperty("placeKitchenPanel").objectReferenceValue = place;
        so.FindProperty("voxelPlacer").objectReferenceValue = placer;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(flowGo);
    }

    static void RemoveLegacyCanvas()
    {
        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            bool arKitchenCanvas =
                canvas.GetComponent<KitchenUI>() != null ||
                canvas.GetComponent<KitchenCatalogUI>() != null ||
                canvas.GetComponent<MaterialColorUI>() != null ||
                canvas.GetComponent<VoxelScaleUI>() != null ||
                canvas.GetComponent<VoxelRotationUI>() != null ||
                canvas.GetComponent<ElementVariantUI>() != null ||
                canvas.GetComponent<PlaneToggleUI>() != null ||
                canvas.GetComponent<VoxelToggleUI>() != null;

            if (!arKitchenCanvas) continue;
            Undo.DestroyObjectImmediate(canvas.gameObject);
            Debug.Log("[UISceneSetup] Removed legacy AR Kitchen uGUI Canvas.");
        }
    }
}
