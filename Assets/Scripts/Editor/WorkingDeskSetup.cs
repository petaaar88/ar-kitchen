using UnityEditor;
using UnityEngine;

// Creates the Working Desk filler definition and wires it (plus its material)
// onto the KitchenLayoutController on Voxel.prefab. The desk has no FBX: it's a
// flex-width filler rendered as a procedural box, added via the placed-strip
// markers rather than the catalog. Run after the layout controller is attached.
public static class WorkingDeskSetup
{
    const string DefinitionPath = "Assets/Scripts/Kitchen/Definitions/WD Working Desk.asset";
    const string VoxelPrefabPath = "Assets/Prefabs/Voxel.prefab";
    const string MaterialPath = "Assets/Materials/KitchenMainMaterial.mat";
    const string TopMaterialPath = "Assets/Materials/KitchenElementBody.mat";

    static readonly Color DeskColor = new Color(0.82f, 0.66f, 0.45f);

    [MenuItem("Tools/AR Kitchen/Create Working Desk Filler")]
    public static void Create()
    {
        var def = CreateOrUpdateDefinition();
        WireToVoxel(def);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static KitchenElementDefinition CreateOrUpdateDefinition()
    {
        var def = AssetDatabase.LoadAssetAtPath<KitchenElementDefinition>(DefinitionPath);
        bool created = def == null;
        if (created) def = ScriptableObject.CreateInstance<KitchenElementDefinition>();

        var so = new SerializedObject(def);
        so.FindProperty("displayName").stringValue = "Working Desk";
        so.FindProperty("code").stringValue = "WD";
        // Group is irrelevant for a filler (skipped by the catalog), but keep it valid.
        so.FindProperty("group").enumValueIndex = (int)KitchenElementGroup.Storage;
        so.FindProperty("modelPrefab").objectReferenceValue = null;
        so.FindProperty("widthMeters").floatValue = 0.6f;   // placeholder; flex-computed at runtime
        so.FindProperty("heightMeters").floatValue = 0.93f;
        so.FindProperty("depthMeters").floatValue = 0.6f;
        so.FindProperty("color").colorValue = DeskColor;
        so.FindProperty("isMandatory").boolValue = false;
        so.FindProperty("isFiller").boolValue = true;
        so.FindProperty("basePrice").floatValue = 0f;
        so.ApplyModifiedPropertiesWithoutUndo();

        if (created) AssetDatabase.CreateAsset(def, DefinitionPath);
        else EditorUtility.SetDirty(def);
        return def;
    }

    static void WireToVoxel(KitchenElementDefinition def)
    {
        var root = PrefabUtility.LoadPrefabContents(VoxelPrefabPath);
        try
        {
            var controller = root.GetComponent<KitchenLayoutController>();
            if (controller == null)
            {
                Debug.LogError($"[WorkingDeskSetup] {VoxelPrefabPath} has no KitchenLayoutController. Run 'Attach KitchenLayoutController To Voxel' first.");
                return;
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
                Debug.LogWarning($"[WorkingDeskSetup] Material not found at {MaterialPath}; the desk will use Unity's default material.");

            var topMaterial = AssetDatabase.LoadAssetAtPath<Material>(TopMaterialPath);
            if (topMaterial == null)
                Debug.LogWarning($"[WorkingDeskSetup] Worktop material not found at {TopMaterialPath}; the desk top will reuse the body material.");

            var so = new SerializedObject(controller);
            so.FindProperty("fillerDefinition").objectReferenceValue = def;
            so.FindProperty("fillerMaterial").objectReferenceValue = material;
            so.FindProperty("fillerTopMaterial").objectReferenceValue = topMaterial;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, VoxelPrefabPath);
            Debug.Log($"[WorkingDeskSetup] Wired Working Desk filler to {VoxelPrefabPath}.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
