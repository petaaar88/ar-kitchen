using UnityEditor;
using UnityEngine;

// Attaches KitchenLayoutController to Voxel.prefab and wires the element prefab ref.
// Run this once after creating the KitchenElement prefab.
public static class KitchenLayoutSetup
{
    const string VoxelPrefabPath = "Assets/Prefabs/Voxel.prefab";
    const string ElementPrefabPath = "Assets/Prefabs/KitchenElement.prefab";

    [MenuItem("Tools/AR Kitchen/Attach KitchenLayoutController To Voxel")]
    public static void Attach()
    {
        var voxelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VoxelPrefabPath);
        if (voxelPrefab == null) { Debug.LogError($"[KitchenLayoutSetup] Missing {VoxelPrefabPath}."); return; }

        var elementPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ElementPrefabPath);
        if (elementPrefab == null) { Debug.LogError($"[KitchenLayoutSetup] Missing {ElementPrefabPath}. Run 'Create Kitchen Element Prefab' first."); return; }
        var elementView = elementPrefab.GetComponent<KitchenElementView>();
        if (elementView == null) { Debug.LogError($"[KitchenLayoutSetup] {ElementPrefabPath} is missing KitchenElementView."); return; }

        var root = PrefabUtility.LoadPrefabContents(VoxelPrefabPath);
        try
        {
            var controller = root.GetComponent<KitchenLayoutController>() ?? root.AddComponent<KitchenLayoutController>();
            var so = new SerializedObject(controller);
            so.FindProperty("voxel").objectReferenceValue = root.GetComponent<VoxelController>();
            so.FindProperty("elementPrefab").objectReferenceValue = elementView;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, VoxelPrefabPath);
            Debug.Log($"[KitchenLayoutSetup] Attached KitchenLayoutController to {VoxelPrefabPath}.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
