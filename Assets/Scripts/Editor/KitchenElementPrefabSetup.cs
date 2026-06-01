using TMPro;
using UnityEditor;
using UnityEngine;

// Builds Assets/Prefabs/KitchenElement.prefab via PrefabUtility so the YAML's
// GUIDs (script, shader, TMP font) are correctly resolved.
public static class KitchenElementPrefabSetup
{
    const string PrefabPath = "Assets/Prefabs/KitchenElement.prefab";
    const string MaterialPath = "Assets/Materials/KitchenElementBody.mat";
    const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    [MenuItem("Tools/AR Kitchen/Create Kitchen Element Prefab")]
    public static void CreatePrefab()
    {
        var material = EnsureBodyMaterial();

        var root = new GameObject("KitchenElement");
        root.AddComponent<KitchenElementView>();

        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        Object.DestroyImmediate(body.GetComponent<BoxCollider>());
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0.5f, 0.5f, 0.5f);
        var bodyRenderer = body.GetComponent<MeshRenderer>();
        bodyRenderer.sharedMaterial = material;

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
        var rt = label.rectTransform;
        rt.sizeDelta = new Vector2(2f, 0.4f);

        var view = root.GetComponent<KitchenElementView>();
        var so = new SerializedObject(view);
        so.FindProperty("body").objectReferenceValue = body.transform;
        so.FindProperty("bodyRenderer").objectReferenceValue = bodyRenderer;
        so.FindProperty("label").objectReferenceValue = label;
        so.ApplyModifiedPropertiesWithoutUndo();

        EnsureFolder("Assets/Prefabs");
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool success);
        Object.DestroyImmediate(root);

        if (success) Debug.Log($"[KitchenElementPrefabSetup] Saved {PrefabPath}.");
        else Debug.LogError($"[KitchenElementPrefabSetup] Failed to save {PrefabPath}.");
    }

    static Material EnsureBodyMaterial()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (existing != null) return existing;

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            Debug.LogError("[KitchenElementPrefabSetup] URP/Lit shader not found.");
            return null;
        }
        var mat = new Material(shader) { name = "KitchenElementBody" };
        mat.SetColor("_BaseColor", Color.white);
        EnsureFolder("Assets/Materials");
        AssetDatabase.CreateAsset(mat, MaterialPath);
        AssetDatabase.SaveAssets();
        return mat;
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
