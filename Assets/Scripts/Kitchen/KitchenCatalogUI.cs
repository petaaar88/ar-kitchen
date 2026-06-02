using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KitchenCatalogUI : MonoBehaviour
{
    [Serializable]
    public class Entry
    {
        public KitchenElementDefinition definition;
        public Button button;
        public TextMeshProUGUI label;
    }

    const float ToastDuration = 2.5f;

    [SerializeField] VoxelStateManager stateManager;
    [SerializeField] GameObject catalogPanel;
    [SerializeField] TextMeshProUGUI remainingLabel;
    [SerializeField] Button removeLastButton;
    [SerializeField] GameObject toast;
    [SerializeField] TextMeshProUGUI toastLabel;
    [SerializeField] GameObject mandatoryBanner;
    [SerializeField] TextMeshProUGUI mandatoryBannerLabel;
    [SerializeField] Entry[] entries;

    readonly List<string> _missingBuffer = new();

    KitchenLayoutController _layout;

    void Awake()
    {
        catalogPanel.SetActive(false);
        if (remainingLabel != null) remainingLabel.gameObject.SetActive(false);
        if (removeLastButton != null)
        {
            removeLastButton.gameObject.SetActive(false);
            removeLastButton.onClick.AddListener(HandleRemoveLast);
        }
        if (toast != null) toast.SetActive(false);
        if (mandatoryBanner != null) mandatoryBanner.SetActive(false);

        foreach (var e in entries)
        {
            var captured = e;
            if (captured.label != null && captured.definition != null)
                captured.label.text = FormatLabel(captured.definition);
            if (captured.button != null)
                captured.button.onClick.AddListener(() => HandleClick(captured));
        }

        stateManager.OnVoxelPlaced  += OnVoxelPlaced;
        stateManager.OnModeChanged  += OnModeChanged;
    }

    void OnDestroy()
    {
        stateManager.OnVoxelPlaced  -= OnVoxelPlaced;
        stateManager.OnModeChanged  -= OnModeChanged;
        if (_layout != null) _layout.OnLayoutChanged -= UpdateState;
    }

    void OnVoxelPlaced()
    {
        _layout = stateManager.Controller != null
            ? stateManager.Controller.GetComponent<KitchenLayoutController>()
            : null;
        if (_layout != null) _layout.OnLayoutChanged += UpdateState;
        UpdateState();
    }

    void OnModeChanged(VoxelEditMode mode)
    {
        bool active = mode == VoxelEditMode.FillKitchen;
        catalogPanel.SetActive(active);
        if (remainingLabel != null) remainingLabel.gameObject.SetActive(active);
        if (removeLastButton != null) removeLastButton.gameObject.SetActive(active);
        if (!active && mandatoryBanner != null) mandatoryBanner.SetActive(false);
        if (active) UpdateState();
    }

    void HandleClick(Entry e)
    {
        if (_layout == null || e.definition == null) return;
        var result = _layout.TryAdd(e.definition);
        if (result == KitchenLayoutController.AddResult.NoDepth)
        {
            int depthCm = Mathf.RoundToInt(e.definition.DepthMeters * 100f);
            ShowToast($"Voxel too shallow for {e.definition.DisplayName} — needs {depthCm} cm depth");
        }
    }

    void HandleRemoveLast()
    {
        _layout?.RemoveLast();
    }

    void UpdateState()
    {
        float remaining = _layout != null ? _layout.RemainingLength : 0f;
        if (remainingLabel != null) remainingLabel.text = $"{remaining:0.0} m free";

        foreach (var e in entries)
        {
            if (e.button == null) continue;
            // Depth check stays tappable so HandleClick can surface a toast.
            e.button.interactable = _layout != null && _layout.LengthFits(e.definition);
        }

        if (removeLastButton != null)
            removeLastButton.interactable = _layout != null && _layout.Placed.Count > 0;

        UpdateMandatoryBanner();
    }

    void UpdateMandatoryBanner()
    {
        if (mandatoryBanner == null) return;

        _missingBuffer.Clear();
        foreach (var e in entries)
        {
            if (e.definition == null || !e.definition.IsMandatory) continue;
            bool present = false;
            if (_layout != null)
            {
                for (int i = 0; i < _layout.Placed.Count; i++)
                {
                    var v = _layout.Placed[i];
                    if (v != null && v.Definition == e.definition) { present = true; break; }
                }
            }
            if (!present) _missingBuffer.Add(e.definition.DisplayName);
        }

        if (_missingBuffer.Count == 0)
        {
            mandatoryBanner.SetActive(false);
            return;
        }

        if (mandatoryBannerLabel != null)
            mandatoryBannerLabel.text = "Missing: " + string.Join(", ", _missingBuffer);
        mandatoryBanner.SetActive(true);
    }

    void ShowToast(string text)
    {
        if (toast == null) return;
        if (toastLabel != null) toastLabel.text = text;
        toast.SetActive(true);
        CancelInvoke(nameof(HideToast));
        Invoke(nameof(HideToast), ToastDuration);
    }

    void HideToast()
    {
        if (toast != null) toast.SetActive(false);
    }

    static string FormatLabel(KitchenElementDefinition def)
    {
        int w = Mathf.RoundToInt(def.WidthMeters * 100f);
        int h = Mathf.RoundToInt(def.HeightMeters * 100f);
        int d = Mathf.RoundToInt(def.DepthMeters * 100f);
        string title = string.IsNullOrEmpty(def.Code) ? def.DisplayName : $"{def.Code} {def.DisplayName}";
        return $"{title}\n<size=70%>{w}×{h}×{d}</size>";
    }
}
