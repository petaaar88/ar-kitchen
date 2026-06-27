using System.Collections.Generic;
using UnityEngine;

public enum KitchenElementGroup { Storage, Washing, Cooking }

[CreateAssetMenu(menuName = "AR Kitchen/Kitchen Element Definition", fileName = "KitchenElement")]
public class KitchenElementDefinition : ScriptableObject
{
    [SerializeField] string displayName = "Element";
    [SerializeField] string code = "";
    [SerializeField] KitchenElementGroup group = KitchenElementGroup.Storage;
    [Tooltip("FBX model placed for this element. Authored to the standard real-world size below. This is variant 0 (the default).")]
    [SerializeField] GameObject modelPrefab;
    [Tooltip("Alternative model prefabs, selectable at runtime by tapping a placed element. Must share the same footprint as the default model so the layout is unaffected.")]
    [SerializeField] GameObject[] variantPrefabs;
    [SerializeField, Min(0.01f)] float widthMeters = 0.6f;
    [SerializeField, Min(0.01f)] float heightMeters = 0.85f;
    [SerializeField, Min(0.01f)] float depthMeters = 0.6f;
    [SerializeField] Color color = Color.gray;
    [SerializeField] bool isMandatory;
    [SerializeField] bool isFiller;
    [Tooltip("Price in EUR for the default model (variant 0).")]
    [SerializeField, Min(0f)] float basePrice;
    [Tooltip("Price in EUR for each alternative variant (index matches variantPrefabs). Leave empty to use basePrice for all variants.")]
    [SerializeField, Min(0f)] float[] variantPrices;
    [Tooltip("Finishes offered in the Texture picker for this element. Applied to the primary (triplanar) material surfaces only; e.g. metal for appliances, wood/stone for the desk.")]
    [SerializeField] Texture2D[] compatibleTextures;

    public string DisplayName => displayName;
    public string Code => code;
    public KitchenElementGroup Group => group;
    public GameObject ModelPrefab => modelPrefab;
    public float WidthMeters => widthMeters;
    public float HeightMeters => heightMeters;
    public float DepthMeters => depthMeters;
    public Color Color => color;
    public bool IsMandatory => isMandatory;
    public bool IsFiller => isFiller;
    public float BasePrice => basePrice;
    public IReadOnlyList<Texture2D> CompatibleTextures => compatibleTextures;

    // Ordered variant access: index 0 is the default ModelPrefab, then variantPrefabs.
    public int VariantCount => 1 + (variantPrefabs != null ? variantPrefabs.Length : 0);

    public GameObject GetVariant(int index)
    {
        if (index <= 0) return modelPrefab;
        index--;
        if (variantPrefabs != null && index < variantPrefabs.Length && variantPrefabs[index] != null)
            return variantPrefabs[index];
        return modelPrefab;
    }

    public float GetVariantPrice(int index)
    {
        if (index <= 0) return basePrice;
        int i = index - 1;
        if (variantPrices != null && i < variantPrices.Length)
            return variantPrices[i];
        return basePrice;
    }
}
