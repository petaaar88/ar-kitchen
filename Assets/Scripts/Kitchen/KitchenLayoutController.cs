using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(VoxelController))]
public class KitchenLayoutController : MonoBehaviour
{
    public enum AddResult { Ok, NoFit, NoDepth, NoPrefab }

    const float Epsilon = 1e-4f;

    // The working desk flexes to fill the free run; never let it shrink to nothing.
    const float MinFillerWidth = 0.1f;

    // Recess the desk a few cm toward the wall so its front sits behind the
    // cabinet line instead of flush with it.
    const float FillerBackInset = 0.05f;

    [SerializeField] VoxelController voxel;
    [SerializeField] KitchenElementView elementPrefab;
    [Tooltip("Filler definition (the working desk) added via the placed-strip markers, not the catalog.")]
    [SerializeField] KitchenElementDefinition fillerDefinition;
    [Tooltip("Material applied to the procedural filler body.")]
    [SerializeField] Material fillerMaterial;
    [Tooltip("Material applied to the filler's white worktop slab (like the sink units).")]
    [SerializeField] Material fillerTopMaterial;

    readonly List<KitchenElementView> _placed = new();

    // The filler lives outside _placed so price/mandatory/ordering logic is
    // unaffected. _fillerSlot is an insertion index in [0, _placed.Count].
    KitchenElementView _filler;
    int _fillerSlot;

    public IReadOnlyList<KitchenElementView> Placed => _placed;
    public float UsedLength { get; private set; }
    // The worktop physically occupies the flexible remainder. Keep that raw
    // span separately for sizing it, while RemainingLength reports genuinely
    // unoccupied space to UI and add/fit checks.
    public float FlexibleLength => Mathf.Max(0f, voxel.Depth - UsedLength);
    public float RemainingLength => _filler == null ? FlexibleLength : 0f;
    public float FillerWidth => _filler != null ? FlexibleLength : 0f;

    public bool HasFiller => _filler != null;
    public int FillerSlot => _fillerSlot;
    public int UnitCount => _placed.Count;

    // A filler can be added when none exists, the voxel is deep enough for it, and
    // there's at least the minimum free run to give it.
    public bool CanAddFiller =>
        fillerDefinition != null && elementPrefab != null && _filler == null
        && DepthFits(fillerDefinition) && RemainingLength >= MinFillerWidth - Epsilon;

    public float TotalPrice
    {
        get
        {
            float total = 0f;
            foreach (var view in _placed)
                if (view != null && view.Definition != null)
                    total += view.Definition.GetVariantPrice(view.CurrentVariantIndex);
            return total;
        }
    }

    public event System.Action OnLayoutChanged;

    public void NotifyLayoutChanged() => OnLayoutChanged?.Invoke();

    void Reset() => voxel = GetComponent<VoxelController>();

    void Awake()
    {
        if (voxel == null) voxel = GetComponent<VoxelController>();
    }

    void OnEnable()
    {
        if (voxel != null) voxel.OnResized += HandleResized;
    }

    void OnDisable()
    {
        if (voxel != null) voxel.OnResized -= HandleResized;
    }

    void HandleResized()
    {
        Reposition();
        OnLayoutChanged?.Invoke();
    }

    public bool DepthFits(KitchenElementDefinition def) =>
        def != null && def.DepthMeters <= voxel.Width + Epsilon;

    public bool LengthFits(KitchenElementDefinition def)
    {
        if (def == null) return false;
        return def.WidthMeters <= RemainingLength + Epsilon;
    }

    public AddResult TryAdd(KitchenElementDefinition def)
    {
        if (def == null || elementPrefab == null) return AddResult.NoPrefab;
        if (!DepthFits(def)) return AddResult.NoDepth;
        if (!LengthFits(def)) return AddResult.NoFit;

        var view = Instantiate(elementPrefab, transform);
        view.Apply(def);
        _placed.Add(view);
        Reposition();
        OnLayoutChanged?.Invoke();
        return AddResult.Ok;
    }

    public bool RemoveLast()
    {
        if (_placed.Count == 0) return false;
        return Remove(_placed[^1]);
    }

    public bool Remove(KitchenElementView view)
    {
        if (view == null) return false;
        int index = _placed.IndexOf(view);
        if (index < 0) return false;

        _placed.RemoveAt(index);
        if (_filler != null && index < _fillerSlot) _fillerSlot--;
        Destroy(view.gameObject);
        Reposition();
        OnLayoutChanged?.Invoke();
        return true;
    }

    public void Clear()
    {
        if (_placed.Count == 0 && _filler == null) return;
        foreach (var view in _placed)
            if (view != null) Destroy(view.gameObject);
        _placed.Clear();
        if (_filler != null) { Destroy(_filler.gameObject); _filler = null; }
        UsedLength = 0f;
        OnLayoutChanged?.Invoke();
    }

    // Adds the working desk at the given slot (insertion index among the placed
    // units). Its width is flex-computed in Reposition each pass.
    public void AddFillerAt(int slot)
    {
        if (!CanAddFiller) return;
        _fillerSlot = Mathf.Clamp(slot, 0, _placed.Count);
        _filler = Instantiate(elementPrefab, transform);
        _filler.ApplyFiller(fillerDefinition, fillerMaterial, fillerTopMaterial);
        Reposition();
        OnLayoutChanged?.Invoke();
    }

    public void MoveFillerTo(int slot)
    {
        if (_filler == null) return;
        _fillerSlot = Mathf.Clamp(slot, 0, _placed.Count);
        Reposition();
        OnLayoutChanged?.Invoke();
    }

    public bool RemoveFiller()
    {
        if (_filler == null) return false;
        Destroy(_filler.gameObject);
        _filler = null;
        Reposition();
        OnLayoutChanged?.Invoke();
        return true;
    }

    void Reposition()
    {
        // Wall = left edge of voxel (x = -hw). Elements line up along +Z starting
        // from -hd, so the placed-strip's left-to-right order (slot 0 first) matches
        // the room's left-to-right - tapping a left worktop slot places it on the
        // left. Rotation is +270° around Y so the labeled face points outward (room
        // side). Pivot is offset by (d, 0, ...) per element to stay snug against -X.
        float hw = voxel.Width * 0.5f;
        float hd = voxel.Depth * 0.5f;
        var rot = Quaternion.Euler(0f, 270f, 0f);

        // Units determine UsedLength; the filler spans whatever run is left.
        float unitsWidth = 0f;
        foreach (var view in _placed)
            if (view != null && view.Definition != null) unitsWidth += view.Definition.WidthMeters;
        UsedLength = unitsWidth;
        float fillerWidth = Mathf.Max(0f, voxel.Depth - unitsWidth);

        // A voxel resize can starve the flex filler below its minimum (or make the
        // voxel too shallow); drop it rather than render a degenerate sliver.
        if (_filler != null && (fillerWidth < MinFillerWidth - Epsilon || !DepthFits(fillerDefinition)))
        {
            Destroy(_filler.gameObject);
            _filler = null;
        }
        if (_filler != null) _fillerSlot = Mathf.Clamp(_fillerSlot, 0, _placed.Count);

        float used = 0f;
        for (int i = 0; i <= _placed.Count; i++)
        {
            if (_filler != null && _fillerSlot == i)
            {
                _filler.SetFillerSize(fillerWidth);
                _filler.transform.localPosition = new Vector3(-hw + fillerDefinition.DepthMeters - FillerBackInset, 0f, -hd + used);
                _filler.transform.localRotation = rot;
                used += fillerWidth;
            }
            if (i == _placed.Count) break;

            var view = _placed[i];
            if (view == null) continue;
            var def = view.Definition;
            view.transform.localPosition = new Vector3(-hw + def.DepthMeters, 0f, -hd + used);
            view.transform.localRotation = rot;
            used += def.WidthMeters;
        }
    }
}
