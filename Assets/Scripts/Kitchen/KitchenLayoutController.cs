using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(VoxelController))]
public class KitchenLayoutController : MonoBehaviour
{
    public enum AddResult { Ok, NoFit, NoDepth, NoPrefab }

    const float Epsilon = 1e-4f;

    [SerializeField] VoxelController voxel;
    [SerializeField] KitchenElementView elementPrefab;

    readonly List<KitchenElementView> _placed = new();

    public IReadOnlyList<KitchenElementView> Placed => _placed;
    public float UsedLength { get; private set; }
    public float RemainingLength => Mathf.Max(0f, voxel.Depth - UsedLength);

    public event System.Action OnLayoutChanged;

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

    public bool LengthFits(KitchenElementDefinition def) =>
        def != null && def.WidthMeters <= RemainingLength + Epsilon;

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
        var last = _placed[^1];
        _placed.RemoveAt(_placed.Count - 1);
        if (last != null) Destroy(last.gameObject);
        Reposition();
        OnLayoutChanged?.Invoke();
        return true;
    }

    public void Clear()
    {
        if (_placed.Count == 0) return;
        foreach (var view in _placed)
            if (view != null) Destroy(view.gameObject);
        _placed.Clear();
        UsedLength = 0f;
        OnLayoutChanged?.Invoke();
    }

    void Reposition()
    {
        // Wall = left edge of voxel (x = -hw). Elements line up along -Z from the
        // back (+hd) toward the front. Rotation is +270° around Y so the labeled
        // face points outward (room side). Pivot is offset by (d, 0, -w) per
        // element so the body's world AABB stays snug against the -X wall.
        float hw = voxel.Width * 0.5f;
        float hd = voxel.Depth * 0.5f;
        var rot = Quaternion.Euler(0f, 270f, 0f);
        float used = 0f;
        foreach (var view in _placed)
        {
            if (view == null) continue;
            var def = view.Definition;
            view.transform.localPosition = new Vector3(-hw + def.DepthMeters, 0f, hd - used - def.WidthMeters);
            view.transform.localRotation = rot;
            used += def.WidthMeters;
        }
        UsedLength = used;
    }
}
