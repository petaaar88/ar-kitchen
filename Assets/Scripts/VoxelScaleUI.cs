using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Shows Width/Depth/Height sliders when in Scale mode.
/// Sliders drive VoxelController.Resize directly.
/// Range: 0.5m – 6m per axis.
/// </summary>
public class VoxelScaleUI : MonoBehaviour
{
    const float MinSize = 0.5f;
    const float MaxSize = 6f;

    [SerializeField] VoxelStateManager stateManager;
    [SerializeField] GameObject scalePanel;
    [SerializeField] Slider widthSlider;
    [SerializeField] Slider depthSlider;
    [SerializeField] Slider heightSlider;
    [SerializeField] TextMeshProUGUI widthLabel;
    [SerializeField] TextMeshProUGUI depthLabel;
    [SerializeField] TextMeshProUGUI heightLabel;

    bool _syncing;

    void Awake()
    {
        scalePanel.SetActive(false);

        widthSlider.minValue  = MinSize;
        widthSlider.maxValue  = MaxSize;
        depthSlider.minValue  = MinSize;
        depthSlider.maxValue  = MaxSize;
        heightSlider.minValue = MinSize;
        heightSlider.maxValue = MaxSize;

        widthSlider.onValueChanged.AddListener(_  => OnSliderChanged());
        depthSlider.onValueChanged.AddListener(_  => OnSliderChanged());
        heightSlider.onValueChanged.AddListener(_ => OnSliderChanged());

        stateManager.OnModeChanged   += OnModeChanged;
        stateManager.OnVoxelPlaced   += SyncFromController;
    }

    void OnDestroy()
    {
        stateManager.OnModeChanged -= OnModeChanged;
        stateManager.OnVoxelPlaced -= SyncFromController;
    }

    void OnModeChanged(VoxelEditMode mode)
    {
        bool active = mode == VoxelEditMode.Scale;
        scalePanel.SetActive(active);
        if (active) SyncFromController();
    }

    void SyncFromController()
    {
        var c = stateManager.Controller;
        if (c == null) return;
        _syncing = true;
        widthSlider.value  = c.Width;
        depthSlider.value  = c.Depth;
        heightSlider.value = c.Height;
        _syncing = false;
        UpdateLabels();
    }

    void OnSliderChanged()
    {
        if (_syncing) return;
        stateManager.Controller?.Resize(widthSlider.value, depthSlider.value, heightSlider.value);
        UpdateLabels();
    }

    void UpdateLabels()
    {
        if (widthLabel)  widthLabel.text  = $"W {widthSlider.value:0.0}m";
        if (depthLabel)  depthLabel.text  = $"D {depthSlider.value:0.0}m";
        if (heightLabel) heightLabel.text = $"H {heightSlider.value:0.0}m";
    }
}
