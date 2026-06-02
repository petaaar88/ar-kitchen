using TMPro;
using UnityEditor;
using UnityEngine;

// Builds Assets/Prefabs/KitchenElement.prefab via PrefabUtility so the YAML's
// GUIDs (script, TMP font) are correctly resolved. The prefab is a lightweight
// container: a KitchenElementView plus a floating label. The actual furniture
// mesh is instantiated at runtime from the element definition's model prefab.
public static class KitchenElementPrefabSetup
{
    const string PrefabPath = "Assets/Prefabs/KitchenElement.prefab";
    const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    [MenuItem("Tools/AR Kitchen/Create Kitchen Element Prefab")]
    public static void CreatePrefab()
    {
        var root = new GameObject("KitchenElement");
        root.AddComponent<KitchenElementView>();

        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(root.transform, false);
        labelGO.transform.localPosition = new Vector3(0.5f, 1.08f, 0.5f);
        var label = labelGO.AddComponent<TextMeshPro>();
        label.text = "Element";
        label.fontSize = 1.6f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.black;
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font != null) label.font = font;
        label.rectTransform.sizeDelta = new Vector2(2f, 0.4f);

        var view = root.GetComponent<KitchenElementView>();
        var so = new SerializedObject(view);
        so.FindProperty("label").objectReferenceValue = label;
        so.ApplyModifiedPropertiesWithoutUndo();

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
