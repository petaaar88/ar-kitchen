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
}
