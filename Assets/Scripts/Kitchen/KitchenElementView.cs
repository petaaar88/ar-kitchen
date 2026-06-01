using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class KitchenElementView : MonoBehaviour
{
    // Pivot is the element's bottom/back/left corner so the layout controller can
    // place transform.position at (cornerX + usedLength, 0, -hd) without offsets.
    [SerializeField] Transform body;
    [SerializeField] MeshRenderer bodyRenderer;
    [SerializeField] TextMeshPro label;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    MaterialPropertyBlock _block;

    KitchenElementDefinition _definition;
    public KitchenElementDefinition Definition => _definition;

    public void Apply(KitchenElementDefinition def)
    {
        _definition = def;
        float w = def.WidthMeters, h = def.HeightMeters, d = def.DepthMeters;

        body.localScale = new Vector3(w, h, d);
        body.localPosition = new Vector3(w * 0.5f, h * 0.5f, d * 0.5f);

        _block ??= new MaterialPropertyBlock();
        bodyRenderer.GetPropertyBlock(_block);
        _block.SetColor(BaseColorId, def.Color);
        bodyRenderer.SetPropertyBlock(_block);

        label.text = def.DisplayName;
        label.transform.localPosition = new Vector3(w * 0.5f, h + 0.08f, d * 0.5f);
    }
}
