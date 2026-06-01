using System;
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

    [SerializeField] VoxelStateManager stateManager;
    [SerializeField] GameObject catalogPanel;
    [SerializeField] Entry[] entries;

    KitchenLayoutController _layout;

    void Awake()
    {
        catalogPanel.SetActive(false);

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
    }

    void OnVoxelPlaced()
    {
        _layout = stateManager.Controller != null
            ? stateManager.Controller.GetComponent<KitchenLayoutController>()
            : null;
    }

    void OnModeChanged(VoxelEditMode mode)
    {
        catalogPanel.SetActive(mode == VoxelEditMode.FillKitchen);
    }

    void HandleClick(Entry e)
    {
        if (_layout == null || e.definition == null) return;
        _layout.TryAdd(e.definition);
    }

    static string FormatLabel(KitchenElementDefinition def)
    {
        var hex = ColorUtility.ToHtmlStringRGB(def.Color);
        int widthCm = Mathf.RoundToInt(def.WidthMeters * 100f);
        return $"<color=#{hex}>■</color>\n{def.DisplayName}\n<size=70%>{widthCm} cm</size>";
    }
}
