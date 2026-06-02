using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class KitchenElementView : MonoBehaviour
{
    // The view's local origin is the element's bottom/back/left corner so the
    // layout controller can place transform.position at the wall without offsets.
    // Apply() instantiates the definition's FBX model and shifts it so its
    // bounding-box min corner sits exactly on that origin.
    [SerializeField] TextMeshPro label;
    [Tooltip("Extra yaw applied to the model so its front faces the room. Models are authored facing the wall, hence 180°.")]
    [SerializeField] float modelYawOffset = 180f;

    KitchenElementDefinition _definition;
    GameObject _modelInstance;

    public KitchenElementDefinition Definition => _definition;

    public void Apply(KitchenElementDefinition def)
    {
        _definition = def;

        if (_modelInstance != null)
        {
            Destroy(_modelInstance);
            _modelInstance = null;
        }

        if (def.ModelPrefab != null)
        {
            // SetParent(..., false) keeps the FBX's imported local rotation/scale
            // (Blender's Z-up→Y-up axis correction).
            _modelInstance = Instantiate(def.ModelPrefab);
            var t = _modelInstance.transform;
            t.SetParent(transform, false);

            // Models are authored facing the wall; spin them about the vertical
            // axis so the front faces the room. Applied before measuring so the
            // footprint AABB still lands in [0,w] x [0,h] x [0,d].
            t.localRotation = Quaternion.Euler(0f, modelYawOffset, 0f) * t.localRotation;
            t.localPosition = Vector3.zero;

            // Drop the model's AABB min corner onto the view origin so the model
            // occupies [0,w] x [0,h] x [0,d] in local space, like the old cube did.
            if (TryGetLocalBounds(t, out var bounds))
                t.localPosition = -bounds.min;
        }

        float w = def.WidthMeters, h = def.HeightMeters, d = def.DepthMeters;
        if (label != null)
        {
            label.text = string.IsNullOrEmpty(def.Code) ? def.DisplayName : $"{def.Code} {def.DisplayName}";
            label.transform.localPosition = new Vector3(w * 0.5f, h + 0.08f, d * 0.5f);
        }
    }

    // Combined render bounds of the model expressed in this view's local space,
    // independent of where the view currently sits in the world.
    bool TryGetLocalBounds(Transform root, out Bounds bounds)
    {
        bounds = default;
        bool has = false;
        var toLocal = transform.worldToLocalMatrix;
        var filters = root.GetComponentsInChildren<MeshFilter>();
        foreach (var mf in filters)
        {
            var mesh = mf.sharedMesh;
            if (mesh == null) continue;

            var m = toLocal * mf.transform.localToWorldMatrix;
            Vector3 min = mesh.bounds.min, max = mesh.bounds.max;
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3(
                    (i & 1) == 0 ? min.x : max.x,
                    (i & 2) == 0 ? min.y : max.y,
                    (i & 4) == 0 ? min.z : max.z);
                var p = m.MultiplyPoint3x4(corner);
                if (!has) { bounds = new Bounds(p, Vector3.zero); has = true; }
                else bounds.Encapsulate(p);
            }
        }
        return has;
    }
}
