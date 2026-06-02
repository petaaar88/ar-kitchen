using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Toggles rendering of the placed voxel's marker visuals (fill + wireframe).
/// Placed kitchen elements stay visible. Re-applies the current state whenever
/// a fresh voxel is placed.
/// </summary>
public class VoxelToggleUI : MonoBehaviour
{
    [SerializeField] VoxelStateManager stateManager;
    [SerializeField] Button toggleButton;

    bool _visible = true;

    static readonly Color OnColor  = new Color(0.15f, 0.15f, 0.15f, 0.85f);
    static readonly Color OffColor = new Color(0.55f, 0.15f, 0.15f, 0.85f);

    void Awake()
    {
        toggleButton.onClick.AddListener(Toggle);
        Refresh();
    }

    void OnEnable()  => stateManager.OnVoxelPlaced += ApplyToCurrent;
    void OnDisable() => stateManager.OnVoxelPlaced -= ApplyToCurrent;

    void Toggle()
    {
        _visible = !_visible;
        ApplyToCurrent();
        Refresh();
    }

    void ApplyToCurrent()
    {
        var controller = stateManager.Controller;
        if (controller != null) controller.SetVisible(_visible);
    }

    void Refresh()
    {
        var img = toggleButton.GetComponent<Image>();
        if (img != null) img.color = _visible ? OnColor : OffColor;
    }
}
