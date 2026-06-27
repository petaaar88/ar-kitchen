using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// One-shot wiring for the Texture picker:
//   1. loads the authored finish textures under Assets/Textures/Kitchen,
//   2. switches KitchenMainMaterial (the shared primary surface, which the desk body
//      also uses) to the AR Kitchen/Triplanar shader so finishes keep a consistent
//      world-space scale,
//   3. fills each KitchenElementDefinition's compatibleTextures with a set that suits
//      the element (metal for appliances, wood/stone for the desk).
public static class TexturePickerSetup
{
    const string TextureFolder = "Assets/Textures/Kitchen";
    const string MainMaterialPath = "Assets/Materials/KitchenMainMaterial.mat";
    const string DefinitionsFolder = "Assets/Scripts/Kitchen/Definitions";
    const string ShaderName = "AR Kitchen/Triplanar";

    // World tiling for the triplanar finish: ~1 texture repeat per metre.
    const float TextureScale = 1.0f;

    [MenuItem("Tools/AR Kitchen/Setup Texture Picker")]
    public static void Setup()
    {
        AssetDatabase.Refresh();
        EnsureFolder(TextureFolder);

        var textures = LoadTextures();
        AssignShader();
        AssignDefinitions(textures);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[TexturePickerSetup] Wired {textures.Count} finishes to all kitchen definitions.");
    }

    static Dictionary<string, Texture2D> LoadTextures()
    {
        string[] names = { "Steel", "White", "Black", "Oak", "Walnut", "Marble", "Granite" };
        var map = new Dictionary<string, Texture2D>();
        foreach (string name in names)
        {
            string path = $"{TextureFolder}/{name}.png";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
            {
                Debug.LogWarning($"[TexturePickerSetup] Missing finish texture: {path}");
                continue;
            }

            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = true;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.mipmapEnabled = true;
                importer.SaveAndReimport();
            }
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            map[name] = texture;
        }
        return map;
    }

    // ---- material + definition wiring -------------------------------------

    static void AssignShader()
    {
        var shader = Shader.Find(ShaderName);
        if (shader == null)
        {
            Debug.LogError($"[TexturePickerSetup] Shader '{ShaderName}' not found. Let it compile, then re-run this menu.");
            return;
        }

        var mat = AssetDatabase.LoadAssetAtPath<Material>(MainMaterialPath);
        if (mat == null)
        {
            Debug.LogWarning($"[TexturePickerSetup] {MainMaterialPath} not found; primary surfaces keep their current shader.");
            return;
        }

        mat.shader = shader;
        // Finish PNGs contain their final albedo color. White is the neutral
        // multiplier, so the material never tints or darkens a chosen texture.
        mat.SetColor("_BaseColor", Color.white);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
        mat.SetFloat("_TextureScale", TextureScale);
        EditorUtility.SetDirty(mat);
    }

    static void AssignDefinitions(Dictionary<string, Texture2D> tex)
    {
        var guids = AssetDatabase.FindAssets("t:KitchenElementDefinition", new[] { DefinitionsFolder });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var def = AssetDatabase.LoadAssetAtPath<KitchenElementDefinition>(path);
            if (def == null) continue;

            string[] names = def.IsFiller
                ? new[] { "Oak", "Walnut", "Marble", "Granite" }   // desk body finishes
                : def.Group switch
                {
                    KitchenElementGroup.Washing => new[] { "Steel", "Marble", "White" },
                    KitchenElementGroup.Cooking => new[] { "Steel", "Black", "White" },
                    _ => new[] { "Steel", "White", "Black" },       // Storage / fridges
                };

            var list = new List<Texture2D>();
            foreach (var n in names)
                if (tex.TryGetValue(n, out var t) && t != null) list.Add(t);

            var so = new SerializedObject(def);
            var prop = so.FindProperty("compatibleTextures");
            prop.arraySize = list.Count;
            for (int i = 0; i < list.Count; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(def);
        }
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
