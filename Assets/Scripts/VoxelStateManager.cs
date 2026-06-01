using UnityEngine;

public enum VoxelEditMode { None, Scale, Placement, Rotation, FillKitchen }

public class VoxelStateManager : MonoBehaviour
{
    [SerializeField] VoxelPlacer placer;

    VoxelController _controller;
    bool _editing;

    public bool IsEditing => _editing;
    public VoxelEditMode CurrentMode { get; private set; } = VoxelEditMode.None;
    public VoxelController Controller => _controller;

    public event System.Action OnVoxelPlaced;
    public event System.Action<bool> OnEditingChanged;
    public event System.Action<VoxelEditMode> OnModeChanged;

    void Awake()
    {
        placer.OnPlaced += HandlePlaced;
    }

    void OnDestroy()
    {
        placer.OnPlaced -= HandlePlaced;
    }

    void HandlePlaced()
    {
        _controller = placer.Voxel.GetComponent<VoxelController>();
        OnVoxelPlaced?.Invoke();
    }

    public void EnterEdit()
    {
        if (!placer.IsPlaced) return;
        _editing = true;
        OnEditingChanged?.Invoke(true);
    }

    public void ExitEdit()
    {
        SetMode(VoxelEditMode.None);
        _editing = false;
        OnEditingChanged?.Invoke(false);
    }

    public void SetMode(VoxelEditMode mode)
    {
        CurrentMode = mode;
        placer.PlacementEnabled = (mode == VoxelEditMode.Placement);
        OnModeChanged?.Invoke(mode);
    }
}
