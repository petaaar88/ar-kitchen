using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR.ARFoundation;

namespace ArKitchen.UI
{
    /// <summary>
    /// Post-placement HUD: top-right Voxel / Planes render toggles plus a
    /// bottom "Kitchen space" card and Edit button. Appears once the voxel is
    /// placed; the Edit button hands off to the existing edit-mode UI and the
    /// bottom bar steps aside while editing so it doesn't overlap those panels.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class PlacedHudPanel : MonoBehaviour
    {
        [SerializeField] VoxelStateManager stateManager;
        [SerializeField] ARPlaneManager planeManager;
        [SerializeField] KitchenElementDefinition[] definitions;
        [SerializeField] float fadeSeconds = 0.3f;

        UIDocument _document;
        VisualElement _root;
        VisualElement _bottomBar;
        Button _voxelToggle;
        Button _planesToggle;
        Button _editButton;
        Button _purchaseButton;
        Label _cardSubtitle;
        Label _cardPrice;
        VisualElement _congratsOverlay;
        IVisualElementScheduledItem _fadeAnim;
        KitchenLayoutController _layout;

        bool _voxelVisible = true;
        bool _planesVisible = true;

        void Awake() => _document = GetComponent<UIDocument>();

        void OnEnable()
        {
            EnsureRoot();
            if (stateManager != null)
            {
                stateManager.OnVoxelPlaced += OnVoxelPlaced;
                stateManager.OnEditingChanged += OnEditingChanged;
            }
        }

        void OnDisable()
        {
            if (stateManager != null)
            {
                stateManager.OnVoxelPlaced -= OnVoxelPlaced;
                stateManager.OnEditingChanged -= OnEditingChanged;
            }
            UnbindLayout();
        }

        void EnsureRoot()
        {
            if (_document == null) _document = GetComponent<UIDocument>();
            if (_root != null || _document == null) return;

            _root = _document.rootVisualElement;
            if (_root == null) return;

            _bottomBar    = _root.Q<VisualElement>("bottom-bar");
            _voxelToggle  = _root.Q<Button>("voxel-toggle");
            _planesToggle = _root.Q<Button>("planes-toggle");
            _editButton   = _root.Q<Button>("edit-button");
            _cardSubtitle    = _root.Q<Label>("card-subtitle");
            _cardPrice       = _root.Q<Label>("card-price");
            _purchaseButton  = _root.Q<Button>("purchase-button");
            _congratsOverlay = _root.Q<VisualElement>("congrats-overlay");

            if (_voxelToggle != null)   _voxelToggle.clicked   += ToggleVoxel;
            if (_planesToggle != null)  _planesToggle.clicked  += TogglePlanes;
            if (_editButton != null)    _editButton.clicked    += () => stateManager?.EnterEdit();
            if (_purchaseButton != null) _purchaseButton.clicked += ShowCongrats;

            var congratsClose = _root.Q<Button>("congrats-close");
            if (congratsClose != null) congratsClose.clicked += HideCongrats;
            var backdrop = _root.Q<VisualElement>("congrats-backdrop");
            backdrop?.RegisterCallback<PointerDownEvent>(_ => HideCongrats());

            ApplyVoxelClass();
            ApplyPlanesClass();
            _root.style.display = DisplayStyle.None;
        }

        void OnVoxelPlaced()
        {
            EnsureRoot();
            _voxelVisible = true;
            ApplyVoxelClass();
            ApplyToCurrentVoxel();
            BindLayout();
            UpdateSubtitle();
            FadeIn();
        }

        void OnEditingChanged(bool editing)
        {
            EnsureRoot();
            // Keep the top toggles, but let the edit-mode panels own the bottom.
            if (_bottomBar != null)
                _bottomBar.style.display = editing ? DisplayStyle.None : DisplayStyle.Flex;
        }

        void BindLayout()
        {
            UnbindLayout();
            var controller = stateManager != null ? stateManager.Controller : null;
            _layout = controller != null ? controller.GetComponent<KitchenLayoutController>() : null;
            if (_layout != null)
                _layout.OnLayoutChanged += UpdateSubtitle;
        }

        void UnbindLayout()
        {
            if (_layout != null)
                _layout.OnLayoutChanged -= UpdateSubtitle;
            _layout = null;
        }

        void UpdateSubtitle()
        {
            if (_cardSubtitle == null) return;

            int count = _layout != null ? _layout.Placed.Count : 0;
            _cardSubtitle.text = count == 0
                ? "Empty - ready to fill"
                : $"{count} unit{(count == 1 ? "" : "s")} placed";

            if (_cardPrice != null)
            {
                float price = _layout != null ? _layout.TotalPrice : 0f;
                bool hasPrice = count > 0 && price > 0f;
                _cardPrice.text = hasPrice ? $"{price:N0} €" : "";
                _cardPrice.EnableInClassList("has-price", hasPrice);
            }

            _purchaseButton?.EnableInClassList("is-visible", AllMandatoryPlaced());
        }

        bool AllMandatoryPlaced()
        {
            if (definitions == null || _layout == null) return false;
            foreach (var def in definitions)
            {
                if (def == null || !def.IsMandatory) continue;
                bool found = false;
                foreach (var view in _layout.Placed)
                {
                    if (view != null && view.Definition == def) { found = true; break; }
                }
                if (!found) return false;
            }
            // At least one item must be placed.
            return _layout.Placed.Count > 0;
        }

        void ShowCongrats() =>
            _congratsOverlay?.EnableInClassList("is-visible", true);

        void HideCongrats() =>
            _congratsOverlay?.EnableInClassList("is-visible", false);

        // Toggles
        void ToggleVoxel()
        {
            _voxelVisible = !_voxelVisible;
            ApplyToCurrentVoxel();
            ApplyVoxelClass();
        }

        void ApplyToCurrentVoxel()
        {
            var controller = stateManager != null ? stateManager.Controller : null;
            if (controller != null) controller.SetVisible(_voxelVisible);
        }

        void TogglePlanes()
        {
            _planesVisible = !_planesVisible;
            if (planeManager != null)
            {
                planeManager.enabled = _planesVisible;
                foreach (var plane in planeManager.trackables)
                    plane.gameObject.SetActive(_planesVisible);
            }
            ApplyPlanesClass();
        }

        void ApplyVoxelClass()  => SetOn(_voxelToggle, _voxelVisible);
        void ApplyPlanesClass() => SetOn(_planesToggle, _planesVisible);

        static void SetOn(VisualElement pill, bool on)
        {
            if (pill == null) return;
            pill.EnableInClassList("is-on", on);
        }

        // Visibility
        public void Show()
        {
            EnsureRoot();
            if (_root == null) return;
            _fadeAnim?.Pause();
            _root.style.opacity = 1f;
            _root.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            EnsureRoot();
            if (_root == null) return;
            _fadeAnim?.Pause();
            _root.style.display = DisplayStyle.None;
        }

        public void FadeIn()
        {
            EnsureRoot();
            if (_root != null) _fadeAnim = UIFade.FadeIn(_root, fadeSeconds, _fadeAnim);
        }
    }
}
