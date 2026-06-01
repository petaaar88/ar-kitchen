using UnityEditor;
using UnityEngine;

// Spawns the four v1 KitchenElementDefinition assets in one go.
// Asset YAML embeds the script's MonoScript GUID, so generating them via
// CreateAsset is safer than hand-writing the files.
public static class KitchenDefinitionsSetup
{
    const string TargetFolder = "Assets/Scripts/Kitchen/Definitions";

    [MenuItem("Tools/AR Kitchen/Create Default Kitchen Definitions")]
    public static void CreateDefaults()
    {
        EnsureFolder(TargetFolder);

        CreateOrUpdate("Fridge",  0.60f, 1.80f, 0.65f, new Color(0.55f, 0.78f, 0.95f), mandatory: true,  filler: false);
        CreateOrUpdate("Stove",   0.60f, 0.85f, 0.60f, new Color(0.95f, 0.45f, 0.30f), mandatory: true,  filler: false);
        CreateOrUpdate("Sink",    0.80f, 0.85f, 0.60f, new Color(0.70f, 0.75f, 0.80f), mandatory: true,  filler: false);
        CreateOrUpdate("Counter", 0.60f, 0.85f, 0.60f, new Color(0.80f, 0.65f, 0.45f), mandatory: false, filler: true);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[KitchenDefinitionsSetup] Wrote 4 definitions to {TargetFolder}.");
    }

    static void CreateOrUpdate(string displayName, float w, float h, float d, Color color, bool mandatory, bool filler)
    {
        string path = $"{TargetFolder}/{displayName}.asset";
        var def = AssetDatabase.LoadAssetAtPath<KitchenElementDefinition>(path);
        bool created = def == null;
        if (created) def = ScriptableObject.CreateInstance<KitchenElementDefinition>();

        var so = new SerializedObject(def);
        so.FindProperty("displayName").stringValue = displayName;
        so.FindProperty("widthMeters").floatValue = w;
        so.FindProperty("heightMeters").floatValue = h;
        so.FindProperty("depthMeters").floatValue = d;
        so.FindProperty("color").colorValue = color;
        so.FindProperty("isMandatory").boolValue = mandatory;
        so.FindProperty("isFiller").boolValue = filler;
        so.ApplyModifiedPropertiesWithoutUndo();

        if (created) AssetDatabase.CreateAsset(def, path);
        else EditorUtility.SetDirty(def);
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
