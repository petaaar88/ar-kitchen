using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// In FillKitchen mode, tapping a placed kitchen element selects it and opens a
/// horizontal strip of variant buttons (labelled by prefab name). Tapping a
/// button swaps that element's model to the chosen variant. Tapping empty space
/// or leaving the mode closes the strip.
///
/// Selection uses a plain Physics raycast against the BoxCollider that
/// KitchenElementView adds to each placed element.
/// </summary>
public class ElementVariantUI : MonoBehaviour
{
    [SerializeField] VoxelStateManager stateManager;
    [SerializeField] Camera arCamera;
    [SerializeField] GameObject variantPanel;     // strip container, hidden by default
    [SerializeField] Transform buttonContainer;   // HorizontalLayoutGroup the buttons go under
    [SerializeField] Button buttonTemplate;       // inactive template, cloned per variant

    const float RayMaxDistance = 50f;

    static readonly List<RaycastResult> UIHits = new();
    readonly List<Button> _spawned = new();

    KitchenElementView _selected;

    void Awake()
    {
        if (buttonTemplate != null) buttonTemplate.gameObject.SetActive(false);
        if (variantPanel != null) variantPanel.SetActive(false);
        stateManager.OnModeChanged += OnModeChanged;
    }

    void OnDestroy()
    {
        stateManager.OnModeChanged -= OnModeChanged;
    }

    void OnModeChanged(VoxelEditMode mode)
    {
        if (mode != VoxelEditMode.FillKitchen) Deselect();
    }

    void Update()
    {
        if (stateManager.CurrentMode != VoxelEditMode.FillKitchen) return;
        if (!TryGetTap(out var screenPos)) return;
        // Taps on the catalog grid or the variant strip itself are UI, not the world.
        if (IsPointerOverUI(screenPos)) return;

        var cam = arCamera != null ? arCamera : Camera.main;
        if (cam == null) return;

        var view = RaycastForElement(cam.ScreenPointToRay(screenPos));
        if (view != null) Select(view);
        else Deselect();
    }

    // Nearest element under the ray. RaycastAll (not Raycast) because the voxel's
    // own box collider encloses the volume and would otherwise be hit first.
    static KitchenElementView RaycastForElement(Ray ray)
    {
        var hits = Physics.RaycastAll(ray, RayMaxDistance);
        KitchenElementView best = null;
        float bestDist = float.MaxValue;
        foreach (var h in hits)
        {
            var view = h.collider.GetComponentInParent<KitchenElementView>();
            if (view != null && h.distance < bestDist) { best = view; bestDist = h.distance; }
        }
        return best;
    }

    static bool TryGetTap(out Vector2 pos)
    {
        pos = default;
        if (Touchscreen.current != null)
        {
            if (Touchscreen.current.primaryTouch.phase.ReadValue() != UnityEngine.InputSystem.TouchPhase.Began)
                return false;
            pos = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            pos = Mouse.current.position.ReadValue();
            return true;
        }
        return false;
    }

    void Select(KitchenElementView view)
    {
        // Nothing to choose when the element only has its default model.
        if (view.Definition == null || view.Definition.VariantCount <= 1) { Deselect(); return; }

        _selected = view;
        BuildButtons(view);
        if (variantPanel != null) variantPanel.SetActive(true);
    }

    void Deselect()
    {
        _selected = null;
        if (variantPanel != null) variantPanel.SetActive(false);
    }

    void BuildButtons(KitchenElementView view)
    {
        foreach (var b in _spawned)
            if (b != null) Destroy(b.gameObject);
        _spawned.Clear();

        var def = view.Definition;
        if (def == null || buttonTemplate == null || buttonContainer == null) return;

        for (int i = 0; i < def.VariantCount; i++)
        {
            int index = i;
            var prefab = def.GetVariant(i);

            var btn = Instantiate(buttonTemplate, buttonContainer);
            btn.gameObject.SetActive(true);

            var label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = prefab != null ? prefab.name : $"Variant {i + 1}";

            btn.interactable = i != view.CurrentVariantIndex;
            btn.onClick.AddListener(() =>
            {
                if (_selected == null) return;
                _selected.ApplyVariant(index);
                RefreshInteractable();
            });
            _spawned.Add(btn);
        }
    }

    // The currently applied variant's button is non-interactable so the active
    // choice reads as selected.
    void RefreshInteractable()
    {
        if (_selected == null) return;
        for (int i = 0; i < _spawned.Count; i++)
            if (_spawned[i] != null) _spawned[i].interactable = i != _selected.CurrentVariantIndex;
    }

    static bool IsPointerOverUI(Vector2 screenPos)
    {
        if (EventSystem.current == null) return false;
        var data = new PointerEventData(EventSystem.current) { position = screenPos };
        UIHits.Clear();
        EventSystem.current.RaycastAll(data, UIHits);
        return UIHits.Count > 0;
    }
}
