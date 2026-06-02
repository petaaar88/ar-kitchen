using UnityEngine;

public enum KitchenElementGroup { Storage, Washing, Cooking }

[CreateAssetMenu(menuName = "AR Kitchen/Kitchen Element Definition", fileName = "KitchenElement")]
public class KitchenElementDefinition : ScriptableObject
{
    [SerializeField] string displayName = "Element";
    [SerializeField] string code = "";
    [SerializeField] KitchenElementGroup group = KitchenElementGroup.Storage;
    [Tooltip("FBX model placed for this element. Authored to the standard real-world size below.")]
    [SerializeField] GameObject modelPrefab;
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
}
