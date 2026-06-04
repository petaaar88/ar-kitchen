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
        [SerializeField] float fadeSeconds = 0.3f;

        UIDocument _document;
        VisualElement _root;
        VisualElement _bottomBar;
        Button _voxelToggle;
        Button _planesToggle;
        Button _editButton;
        IVisualElementScheduledItem _fadeAnim;

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

            if (_voxelToggle != null)  _voxelToggle.clicked  += ToggleVoxel;
            if (_planesToggle != null) _planesToggle.clicked += TogglePlanes;
            if (_editButton != null)   _editButton.clicked   += () => stateManager?.EnterEdit();

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
            FadeIn();
        }

        void OnEditingChanged(bool editing)
        {
            EnsureRoot();
            // Keep the top toggles, but let the edit-mode panels own the bottom.
            if (_bottomBar != null)
                _bottomBar.style.display = editing ? DisplayStyle.None : DisplayStyle.Flex;
        }

        // ── Toggles ──────────────────────────────────────────────────────
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

        // ── Visibility ───────────────────────────────────────────────────
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
