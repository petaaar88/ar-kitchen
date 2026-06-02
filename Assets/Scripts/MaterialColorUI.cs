using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Shows R/G/B sliders when in Color mode. A Main/Secondary selector picks which
/// shared kitchen material the sliders drive; setting the material's _BaseColor
/// recolours every placed element using that material at once.
///
/// Note: these are shared material assets. In the editor, edits persist to the
/// asset after play mode stops (and dirty the .mat) — that is intentional here so
/// a chosen colour sticks. On device the change is in-memory for the session.
/// </summary>
public class MaterialColorUI : MonoBehaviour
{
    [SerializeField] VoxelStateManager stateManager;
    [SerializeField] GameObject colorPanel;

    [Header("Materials")]
    [SerializeField] Material mainMaterial;
    [SerializeField] Material secondaryMaterial;

    [Header("Selector")]
    [SerializeField] Button mainButton;
    [SerializeField] Button secondaryButton;
    [SerializeField] Image previewSwatch;

    [Header("Sliders")]
    [SerializeField] Slider redSlider;
    [SerializeField] Slider greenSlider;
    [SerializeField] Slider blueSlider;
    [SerializeField] TextMeshProUGUI redLabel;
    [SerializeField] TextMeshProUGUI greenLabel;
    [SerializeField] TextMeshProUGUI blueLabel;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly Color SelectedTint   = new Color(0.30f, 0.30f, 0.30f, 0.85f);
    static readonly Color UnselectedTint = new Color(0.15f, 0.15f, 0.15f, 0.85f);

    Material _active;
    bool _syncing;

    void Awake()
    {
        colorPanel.SetActive(false);

        foreach (var s in new[] { redSlider, greenSlider, blueSlider })
        {
            s.minValue = 0f;
            s.maxValue = 1f;
        }

        redSlider.onValueChanged.AddListener(_   => OnSliderChanged());
        greenSlider.onValueChanged.AddListener(_ => OnSliderChanged());
        blueSlider.onValueChanged.AddListener(_  => OnSliderChanged());

        mainButton.onClick.AddListener(() => SelectMaterial(mainMaterial));
        secondaryButton.onClick.AddListener(() => SelectMaterial(secondaryMaterial));

        _active = mainMaterial;

        stateManager.OnModeChanged += OnModeChanged;
    }

    void OnDestroy()
    {
        stateManager.OnModeChanged -= OnModeChanged;
    }

    void OnModeChanged(VoxelEditMode mode)
    {
        bool active = mode == VoxelEditMode.Color;
        colorPanel.SetActive(active);
        if (active) SelectMaterial(_active);
    }

    void SelectMaterial(Material mat)
    {
        if (mat == null) return;
        _active = mat;
        if (mainButton)      mainButton.GetComponent<Image>().color      = mat == mainMaterial      ? SelectedTint : UnselectedTint;
        if (secondaryButton) secondaryButton.GetComponent<Image>().color = mat == secondaryMaterial ? SelectedTint : UnselectedTint;
        SyncFromMaterial();
    }

    Color GetColor(Material m) =>
        m.HasProperty(BaseColorId) ? m.GetColor(BaseColorId) : m.color;

    void SetColor(Material m, Color c)
    {
        if (m.HasProperty(BaseColorId)) m.SetColor(BaseColorId, c);
        m.color = c; // keep legacy _Color in sync for any non-URP shader fallback
    }

    void SyncFromMaterial()
    {
        if (_active == null) return;
        var c = GetColor(_active);
        _syncing = true;
        redSlider.value   = c.r;
        greenSlider.value = c.g;
        blueSlider.value  = c.b;
        _syncing = false;
        UpdateLabelsAndPreview(c);
    }

    void OnSliderChanged()
    {
        if (_syncing || _active == null) return;
        var c = new Color(redSlider.value, greenSlider.value, blueSlider.value, 1f);
        SetColor(_active, c);
        UpdateLabelsAndPreview(c);
    }

    void UpdateLabelsAndPreview(Color c)
    {
        if (redLabel)      redLabel.text   = $"R {c.r:0.00}";
        if (greenLabel)    greenLabel.text = $"G {c.g:0.00}";
        if (blueLabel)     blueLabel.text  = $"B {c.b:0.00}";
        if (previewSwatch) previewSwatch.color = new Color(c.r, c.g, c.b, 1f);
    }
}
