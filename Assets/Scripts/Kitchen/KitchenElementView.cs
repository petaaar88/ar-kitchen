using UnityEngine;

[DisallowMultipleComponent]
public class KitchenElementView : MonoBehaviour
{
    // The view's local origin is the element's bottom/back/left corner so the
    // layout controller can place transform.position at the wall without offsets.
    // Apply() instantiates the definition's FBX model and shifts it so its
    // bounding-box min corner sits exactly on that origin.
    [Tooltip("Extra yaw applied to the model so its front faces the room. Models are authored facing the wall, hence 180°.")]
    [SerializeField] float modelYawOffset = 180f;

    // Thickness of the white worktop slab capping the filler, like the sink units.
    const float FillerTopThickness = 0.05f;

    KitchenElementDefinition _definition;
    GameObject _modelInstance;
    int _variantIndex;

    Transform _fillerBody;
    Transform _fillerTop;

    public KitchenElementDefinition Definition => _definition;
    public int CurrentVariantIndex => _variantIndex;
    public bool IsFiller { get; private set; }

    public void Apply(KitchenElementDefinition def)
    {
        _definition = def;
        IsFiller = false;
        ApplyVariant(0);
    }

    // Filler elements (e.g. the working desk) have no authored FBX: their width
    // flexes to span the free run, so we render scaled primitive boxes instead of
    // the measure-the-prefab path used by Apply/ApplyVariant. It's built from two
    // boxes - a coloured body and a thin white worktop on top - to read like the
    // sink units. Height and depth come from the definition; width is set every
    // layout pass via SetFillerSize.
    public void ApplyFiller(KitchenElementDefinition def, Material bodyMaterial, Material topMaterial)
    {
        _definition = def;
        IsFiller = true;
        _variantIndex = 0;

        if (_modelInstance != null)
        {
            Destroy(_modelInstance);
            _modelInstance = null;
        }
        _fillerBody = null;
        _fillerTop = null;
        if (def == null) return;

        // Empty container so a single Destroy clears both parts.
        _modelInstance = new GameObject("Filler");
        _modelInstance.transform.SetParent(transform, false);

        _fillerBody = CreateFillerPart("Body", bodyMaterial);
        _fillerTop = CreateFillerPart("Top", topMaterial != null ? topMaterial : bodyMaterial);

        SetFillerSize(def.DepthMeters);
    }

    Transform CreateFillerPart(string partName, Material material)
    {
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = partName;
        // The primitive's own collider would intercept taps; the layout doesn't
        // need one for the filler (no variants to pick).
        var primitiveCollider = box.GetComponent<Collider>();
        if (primitiveCollider != null) Destroy(primitiveCollider);

        var t = box.transform;
        t.SetParent(_modelInstance.transform, false);

        if (material != null)
        {
            var renderer = box.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.sharedMaterial = material;
        }
        return t;
    }

    // Scales the filler boxes to width x height x depth, keeping the combined min
    // corner on the view origin so KitchenLayoutController's placement formula
    // (shared with real units) lands it correctly. A unit cube spans [-0.5,0.5]^3,
    // so scaling by the size and offsetting by half puts each part in [0,size].
    public void SetFillerSize(float width)
    {
        if (!IsFiller || _fillerBody == null || _fillerTop == null || _definition == null) return;
        float w = Mathf.Max(0.001f, width);
        float h = _definition.HeightMeters;
        float d = _definition.DepthMeters;
        float top = Mathf.Min(FillerTopThickness, h);
        float body = Mathf.Max(0.001f, h - top);

        _fillerBody.localScale = new Vector3(w, body, d);
        _fillerBody.localPosition = new Vector3(w * 0.5f, body * 0.5f, d * 0.5f);

        _fillerTop.localScale = new Vector3(w, top, d);
        _fillerTop.localPosition = new Vector3(w * 0.5f, body + top * 0.5f, d * 0.5f);
    }

    // Swaps the visible model to the given variant. Variants share the footprint,
    // so the layout (position/rotation set by KitchenLayoutController) is unaffected.
    public void ApplyVariant(int index)
    {
        if (_definition == null) return;
        _variantIndex = Mathf.Clamp(index, 0, _definition.VariantCount - 1);

        if (_modelInstance != null)
        {
            Destroy(_modelInstance);
            _modelInstance = null;
        }

        var prefab = _definition.GetVariant(_variantIndex);
        if (prefab == null) return;

        // SetParent(..., false) keeps the FBX's imported local rotation/scale
        // (Blender's Z-up→Y-up axis correction).
        _modelInstance = Instantiate(prefab);
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
        {
            t.localPosition = -bounds.min;

            // Tap target for variant selection: a box wrapping the model's
            // footprint in this view's local space (now anchored at the origin).
            var box = GetComponent<BoxCollider>();
            if (box == null) box = gameObject.AddComponent<BoxCollider>();
            box.center = bounds.size * 0.5f;
            box.size = bounds.size;
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
