using UnityEditor;
using UnityEngine;

// Builds Assets/Prefabs/KitchenElement.prefab via PrefabUtility so the YAML's
// script GUID is correctly resolved. The prefab is a lightweight container with
// just a KitchenElementView; the actual furniture mesh is instantiated at
// runtime from the element definition's model prefab.
public static class KitchenElementPrefabSetup
{
    const string PrefabPath = "Assets/Prefabs/KitchenElement.prefab";

    [MenuItem("Tools/AR Kitchen/Create Kitchen Element Prefab")]
    public static void CreatePrefab()
    {
        var root = new GameObject("KitchenElement");
        root.AddComponent<KitchenElementView>();

        EnsureFolder("Assets/Prefabs");
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool success);
        Object.DestroyImmediate(root);

        if (success) Debug.Log($"[KitchenElementPrefabSetup] Saved {PrefabPath}.");
        else Debug.LogError($"[KitchenElementPrefabSetup] Failed to save {PrefabPath}.");
    }

    static void EnsureFolder(string assetPath)
    {
        if (AssetDatabase.IsValidFolder(assetPath)) return;
        var parts = assetPath.Split('/');
        string parent = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{parent}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(parent, parts[i]);
            parent = next;
        }
    }
}
