using UnityEngine;

[CreateAssetMenu(menuName = "AR Kitchen/Kitchen Element Definition", fileName = "KitchenElement")]
public class KitchenElementDefinition : ScriptableObject
{
    [SerializeField] string displayName = "Element";
    [SerializeField, Min(0.05f)] float widthMeters = 0.6f;
    [SerializeField, Min(0.05f)] float heightMeters = 0.85f;
    [SerializeField, Min(0.05f)] float depthMeters = 0.6f;
    [SerializeField] Color color = Color.gray;
    [SerializeField] bool isMandatory;
    [SerializeField] bool isFiller;

    public string DisplayName => displayName;
    public float WidthMeters => widthMeters;
    public float HeightMeters => heightMeters;
    public float DepthMeters => depthMeters;
    public Color Color => color;
    public bool IsMandatory => isMandatory;
    public bool IsFiller => isFiller;
}
