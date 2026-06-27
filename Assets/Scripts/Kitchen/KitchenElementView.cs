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
    BoxCollider _fillerCollider;

    static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int TextureScaleId = Shader.PropertyToID("_TextureScale");
    static readonly int MetallicId = Shader.PropertyToID("_Metallic");
    static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");

    // A solid colour applied to an element's secondary surfaces, independent of the
    // body texture. Metallic entries (gold, silver) also drive _Metallic/_Smoothness,
    // otherwise they'd read as flat mustard/grey rather than metal.
    public readonly struct ColorOption
    {
        public readonly string Name;
        public readonly Color Color;
        public readonly bool Metallic;
        public ColorOption(string name, Color color, bool metallic)
        {
            Name = name;
            Color = color;
            Metallic = metallic;
        }
    }

    // Shared palette shown in the Texture picker's colour row.
    public static readonly ColorOption[] ColorPalette =
    {
        new ColorOption("Black",  new Color(0.05f, 0.05f, 0.06f), false),
        new ColorOption("Gold",   new Color(1.00f, 0.78f, 0.34f), true),
        new ColorOption("White",  new Color(0.95f, 0.95f, 0.95f), false),
        new ColorOption("Silver", new Color(0.90f, 0.90f, 0.92f), true),
    };

    // The shared secondary material (handles/trims/accents) the colour picker tints;
    // matched by name so the body texture and white worktops are left alone.
    const string SecondaryMaterialName = "KitchenSecondaryMaterial";
    const float MetalSmoothness = 0.85f;
    const float MatteSmoothness = 0.30f;

    Texture _chosenTexture;
    ColorOption? _chosenColor;

    GameObject _highlight;
    static Material _highlightMaterial;

    public KitchenElementDefinition Definition => _definition;
    public int CurrentVariantIndex => _variantIndex;
    public bool IsFiller { get; private set; }
    public Texture ChosenTexture => _chosenTexture;
    public ColorOption? ChosenColor => _chosenColor;

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

        // Tap target so the desk is selectable in Texture mode - the primitive
        // colliders are stripped in CreateFillerPart. Sized in SetFillerSize.
        _fillerCollider = GetComponent<BoxCollider>();
        if (_fillerCollider == null) _fillerCollider = gameObject.AddComponent<BoxCollider>();

        SetFillerSize(def.DepthMeters);
        ReapplyFinish();
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

        if (_fillerCollider != null)
        {
            _fillerCollider.center = new Vector3(w * 0.5f, h * 0.5f, d * 0.5f);
            _fillerCollider.size = new Vector3(w, h, d);
        }
    }

    // Texture finish: drives the primary body surfaces. "Primary" = whatever uses the
    // triplanar shader (it alone exposes _TextureScale) - KitchenMainMaterial on FBX
    // units and the desk body. Secondary/detail surfaces carry the colour finish
    // instead (see ApplyColor). The material is instanced per renderer so each placed
    // unit keeps its own finish.
    public void ApplyTexture(Texture texture)
    {
        _chosenTexture = texture;
        ReapplyFinish();
    }

    // Secondary colour finish: tints the shared secondary material (handles, trims,
    // accents) per instance, independent of the body texture. A white _BaseMap keeps
    // the colour flat; metallic swatches (gold, silver) drive _Metallic/_Smoothness
    // so they read as metal rather than mustard/grey.
    public void ApplyColor(ColorOption option)
    {
        _chosenColor = option;
        ReapplyFinish();
    }

    // True when the model has a secondary surface a colour can tint (most appliances
    // do; the imported C1 cooktop and the procedural desk don't).
    public bool HasSecondarySurface
    {
        get
        {
            foreach (var meshRenderer in GetComponentsInChildren<MeshRenderer>(true))
                foreach (var shared in meshRenderer.sharedMaterials)
                    if (shared != null && shared.name.StartsWith(SecondaryMaterialName))
                        return true;
            return false;
        }
    }

    // Re-pushes the chosen finishes onto the current renderers: the texture onto the
    // primary body, the colour onto the secondary surfaces - each independent. Called
    // after the model is (re)built, since a variant swap or filler rebuild replaces
    // the meshes.
    void ReapplyFinish()
    {
        if (_chosenTexture == null && !_chosenColor.HasValue) return;

        var renderers = GetComponentsInChildren<MeshRenderer>(true);
        bool modelHasPrimary = false;
        foreach (var meshRenderer in renderers)
        {
            foreach (var shared in meshRenderer.sharedMaterials)
            {
                if (shared != null && shared.HasProperty(TextureScaleId))
                {
                    modelHasPrimary = true;
                    break;
                }
            }
            if (modelHasPrimary) break;
        }

        foreach (var meshRenderer in renderers)
        {
            // meshRenderer.materials instantiates so the override stays local to this unit.
            foreach (var instance in meshRenderer.materials)
            {
                if (instance == null) continue;

                // C1 is an imported single-finish cooktop without a triplanar surface;
                // treat its ordinary BaseMap surface as primary so it still textures.
                bool isPrimary = instance.HasProperty(TextureScaleId)
                    || (!modelHasPrimary && instance.HasProperty(BaseMapId));

                if (isPrimary)
                {
                    if (_chosenTexture != null) ApplyTextureTo(instance);
                }
                else if (_chosenColor.HasValue && instance.name.StartsWith(SecondaryMaterialName))
                {
                    ApplyColorTo(instance, _chosenColor.Value);
                }
            }
        }
    }

    void ApplyTextureTo(Material instance)
    {
        instance.SetTexture(BaseMapId, _chosenTexture);
        if (instance.HasProperty(BaseColorId)) instance.SetColor(BaseColorId, Color.white);
    }

    // Always sets metal/smoothness explicitly so a matte pick after a metallic one
    // (or vice versa) never leaves stale shininess.
    static void ApplyColorTo(Material instance, ColorOption option)
    {
        if (instance.HasProperty(BaseMapId)) instance.SetTexture(BaseMapId, Texture2D.whiteTexture);
        if (instance.HasProperty(BaseColorId)) instance.SetColor(BaseColorId, option.Color);
        if (instance.HasProperty(MetallicId)) instance.SetFloat(MetallicId, option.Metallic ? 1f : 0f);
        if (instance.HasProperty(SmoothnessId))
            instance.SetFloat(SmoothnessId, option.Metallic ? MetalSmoothness : MatteSmoothness);
    }

    // Selection halo for Texture mode: a translucent box wrapping the element's
    // footprint (read from the tap collider), parented so it tracks the view.
    public void SetSelected(bool on)
    {
        if (!on)
        {
            if (_highlight != null) Destroy(_highlight);
            _highlight = null;
            return;
        }

        var box = GetComponent<BoxCollider>();
        if (box == null) return;

        if (_highlight == null)
        {
            _highlight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _highlight.name = "SelectionHighlight";
            var primitiveCollider = _highlight.GetComponent<Collider>();
            if (primitiveCollider != null) Destroy(primitiveCollider);
            _highlight.transform.SetParent(transform, false);

            var r = _highlight.GetComponent<MeshRenderer>();
            r.sharedMaterial = HighlightMaterial();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        _highlight.transform.localPosition = box.center;
        _highlight.transform.localScale = box.size * 1.04f + new Vector3(0.012f, 0.012f, 0.012f);
    }

    // Shared transparent unlit material for the selection halo.
    static Material HighlightMaterial()
    {
        if (_highlightMaterial != null) return _highlightMaterial;

        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        var mat = new Material(shader != null ? shader : Shader.Find("Sprites/Default"));
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);          // transparent
        if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
        mat.DisableKeyword("_SURFACE_TYPE_OPAQUE");
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        var tint = new Color(0.16f, 1f, 0.28f, 0.42f); // bright green, clearly visible
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
        else mat.color = tint;
        return _highlightMaterial = mat;
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
            // declared footprint. Imported mesh bounds can contain stray geometry
            // and produce a huge invisible hit area, so never use them as the
            // interactive collider size.
            var box = GetComponent<BoxCollider>();
            if (box == null) box = gameObject.AddComponent<BoxCollider>();
            box.size = new Vector3(
                Mathf.Max(0.01f, _definition.DepthMeters),
                Mathf.Max(0.01f, _definition.HeightMeters),
                Mathf.Max(0.01f, _definition.WidthMeters));
            box.center = box.size * 0.5f;
        }

        ReapplyFinish();
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
