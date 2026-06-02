using UnityEditor;
using UnityEngine;

// Spawns one KitchenElementDefinition per standard model in Assets/Models and
// wires each to its FBX. Asset YAML embeds the script's MonoScript GUID, so
// generating via CreateAsset is safer than hand-writing the files.
public static class KitchenDefinitionsSetup
{
    const string TargetFolder = "Assets/Scripts/Kitchen/Definitions";

    static readonly Color StorageColor = new Color(0.55f, 0.78f, 0.95f);
    static readonly Color WashingColor = new Color(0.70f, 0.75f, 0.80f);
    static readonly Color CookingColor = new Color(0.95f, 0.45f, 0.30f);

    // code, type, group, model path, width, height, depth (metres)
    struct Spec
    {
        public string Code, Type, ModelPath;
        public KitchenElementGroup Group;
        public float W, H, D;
        public Color Color;
    }

    static Spec[] Specs()
    {
        return new[]
        {
            S("S1", "Fridge", "Storage/S1 Fridge", 0.60f, 0.90f, 0.60f),
            S("S2", "Fridge", "Storage/S2 Fridge", 0.60f, 1.80f, 0.60f),
            S("S3", "Fridge", "Storage/S3 Fridge", 0.90f, 1.80f, 0.60f),
            S("S4", "Fridge", "Storage/S4 Fridge", 1.20f, 1.80f, 0.60f),
            W("W1", "Sink", "Washing/W1 Sink", 0.30f, 0.90f, 0.60f),
            W("W2", "Sink", "Washing/W2 Sink", 0.60f, 0.90f, 0.60f),
            W("W3", "Sink", "Washing/W3 Sink", 0.90f, 0.90f, 0.60f),
            W("W4", "Sink", "Washing/W4 Sink", 1.20f, 0.90f, 0.60f),
            C("C1", "Stove", "Cooking/C1 Stove", 0.30f, 0.02f, 0.60f),
            C("C2", "Stove", "Cooking/C2 Stove", 0.60f, 0.90f, 0.60f),
            C("C3", "Stove", "Cooking/C3 Stove", 1.20f, 1.80f, 0.60f),
        };
    }

    static Spec S(string code, string type, string path, float w, float h, float d) =>
        new Spec { Code = code, Type = type, ModelPath = $"Assets/Models/{path}.fbx", Group = KitchenElementGroup.Storage, W = w, H = h, D = d, Color = StorageColor };
    static Spec W(string code, string type, string path, float w, float h, float d) =>
        new Spec { Code = code, Type = type, ModelPath = $"Assets/Models/{path}.fbx", Group = KitchenElementGroup.Washing, W = w, H = h, D = d, Color = WashingColor };
    static Spec C(string code, string type, string path, float w, float h, float d) =>
        new Spec { Code = code, Type = type, ModelPath = $"Assets/Models/{path}.fbx", Group = KitchenElementGroup.Cooking, W = w, H = h, D = d, Color = CookingColor };

    [MenuItem("Tools/AR Kitchen/Create Default Kitchen Definitions")]
    public static void CreateDefaults()
    {
        EnsureFolder(TargetFolder);

        // Remove the legacy generic definitions (replaced by per-model defs).
        foreach (var legacy in new[] { "Fridge", "Stove", "Sink", "Counter" })
            AssetDatabase.DeleteAsset($"{TargetFolder}/{legacy}.asset");

        var specs = Specs();
        foreach (var s in specs)
            CreateOrUpdate(s);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[KitchenDefinitionsSetup] Wrote {specs.Length} definitions to {TargetFolder}.");
    }

    static void CreateOrUpdate(Spec s)
    {
        string path = $"{TargetFolder}/{s.Code} {s.Type}.asset";
        var def = AssetDatabase.LoadAssetAtPath<KitchenElementDefinition>(path);
        bool created = def == null;
        if (created) def = ScriptableObject.CreateInstance<KitchenElementDefinition>();

        var model = AssetDatabase.LoadAssetAtPath<GameObject>(s.ModelPath);
        if (model == null)
            Debug.LogWarning($"[KitchenDefinitionsSetup] Model not found at {s.ModelPath}; {s.Code} will have no mesh.");

        var so = new SerializedObject(def);
        so.FindProperty("displayName").stringValue = s.Type;
        so.FindProperty("code").stringValue = s.Code;
        so.FindProperty("group").enumValueIndex = (int)s.Group;
        so.FindProperty("modelPrefab").objectReferenceValue = model;
        so.FindProperty("widthMeters").floatValue = s.W;
        so.FindProperty("heightMeters").floatValue = s.H;
        so.FindProperty("depthMeters").floatValue = s.D;
        so.FindProperty("color").colorValue = s.Color;
        so.FindProperty("isMandatory").boolValue = false;
        so.FindProperty("isFiller").boolValue = false;
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
