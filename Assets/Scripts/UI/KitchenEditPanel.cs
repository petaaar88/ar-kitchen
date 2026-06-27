using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace ArKitchen.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class KitchenEditPanel : MonoBehaviour
    {
        enum MoveDir { Forward, Back, Left, Right }

        const float MinSize = 0.3f;
        const float MaxSize = 4f;
        const float SizeStep = 0.1f;
        const float MoveStep = 0.1f;
        const float ToastDuration = 1.6f;

        [SerializeField] VoxelStateManager stateManager;
        [SerializeField] Camera arCamera;
        [SerializeField] KitchenElementDefinition[] definitions;

        static readonly List<string> MissingMandatory = new();
        static readonly List<KitchenEditPanel> Instances = new();

        UIDocument _document;
        VisualElement _root;
        VisualElement _contextPanel;
        VisualElement _headerRight;
        Label _headerIcon;
        Label _headerTitle;
        Label _headerHint;
        Label _toastLabel;
        VisualElement _variantOverlay;
        VisualElement _variantList;
        Label _variantTitle;

        Button _doneButton;
        Button _addWorktopButton;

        readonly Dictionary<VoxelEditMode, Button> _modeButtons = new();
        readonly Dictionary<VoxelEditMode, VisualElement> _modePanels = new();
        readonly Dictionary<KitchenElementGroup, Button> _categoryButtons = new();
        readonly Dictionary<Button, KitchenElementDefinition> _catalogDefinitions = new();
        readonly Dictionary<Button, Label> _catalogActionLabels = new();
        readonly List<Button> _catalogButtons = new();
        readonly List<Button> _placedButtons = new();
        readonly List<Button> _snapButtons = new();

        Slider _widthSlider;
        Slider _depthSlider;
        Slider _heightSlider;

        Label _widthValue;
        Label _depthValue;
        Label _heightValue;
        Label _rotationValue;
        Label _freeLabel;
        Label _mandatoryLabel;

        ScrollView _placedStrip;
        VisualElement _catalogGrid;
        VisualElement _textureGrid;
        Label _textureHint;
        KitchenElementView _textureTarget;
        readonly List<Button> _textureButtons = new();

        KitchenLayoutController _layout;
        KitchenElementGroup _activeGroup = KitchenElementGroup.Storage;
        KitchenElementView _selectedVariantView;
        bool _choosingWorktopSlot;
        IVisualElementScheduledItem _toastHide;
        bool _syncing;

        void Awake() => _document = GetComponent<UIDocument>();

        void OnEnable()
        {
            if (!Instances.Contains(this)) Instances.Add(this);
            EnsureRoot();
            if (stateManager != null)
            {
                stateManager.OnVoxelPlaced += OnVoxelPlaced;
                stateManager.OnEditingChanged += OnEditingChanged;
                stateManager.OnModeChanged += OnModeChanged;
            }
            if (stateManager != null && stateManager.IsEditing) Show();
            else Hide();
        }

        void OnDisable()
        {
            Instances.Remove(this);
            if (stateManager != null)
            {
                stateManager.OnVoxelPlaced -= OnVoxelPlaced;
                stateManager.OnEditingChanged -= OnEditingChanged;
                stateManager.OnModeChanged -= OnModeChanged;
            }
            UnbindLayout();
        }

        public static bool IsPointerOverBlockingUi(Vector2 screenPos)
        {
            foreach (var instance in Instances)
            {
                if (instance != null && instance.IsPointerOverOwnUi(screenPos))
                    return true;
            }
            return false;
        }

        // Texture-mode taps are routed through UI Toolkit's own event (the same one
        // that drives the buttons), so a swatch press is reliably told apart from a
        // 3D-world tap by its target - no screen/panel coordinate guessing.
        void OnTexturePointerDown(PointerDownEvent evt)
        {
            if (stateManager == null || stateManager.CurrentMode != VoxelEditMode.Color)
                return;

            // Landed on the panel or mode bar (a swatch, a mode button)? Let UI handle it.
            for (var e = evt.target as VisualElement; e != null; e = e.parent)
                if (e == _contextPanel || e.name == "mode-bar")
                    return;

            var cam = arCamera != null ? arCamera : Camera.main;
            if (cam == null) return;

            var view = RaycastForElement(cam.ScreenPointToRay(CurrentPointerScreenPos()));
            if (view != null) SelectTextureTarget(view); // tapped an element
            else DeselectTexture();                      // tapped empty world space
        }

        static Vector2 CurrentPointerScreenPos()
        {
            if (Touchscreen.current != null) return Touchscreen.current.primaryTouch.position.ReadValue();
            if (Mouse.current != null) return Mouse.current.position.ReadValue();
            return default;
        }

        void EnsureRoot()
        {
            if (_root != null) return;
            if (_document == null) _document = GetComponent<UIDocument>();
            _root = _document.rootVisualElement;
            if (_root == null) return;
            _root.RegisterCallback<PointerDownEvent>(OnTexturePointerDown);

            _contextPanel = _root.Q("context-panel");
            _headerRight = _root.Q("header-right");
            _headerIcon = _root.Q<Label>("header-icon-label");
            _headerTitle = _root.Q<Label>("header-title");
            _headerHint = _root.Q<Label>("header-hint");
            _toastLabel = _root.Q<Label>("toast-label");
            _doneButton = _root.Q<Button>("done-button");
            _variantOverlay = _root.Q("variant-overlay");
            _variantList = _root.Q("variant-list");
            _variantTitle = _root.Q<Label>("variant-title");

            _modePanels[VoxelEditMode.Scale] = _root.Q("scale-panel");
            _modePanels[VoxelEditMode.Placement] = _root.Q("move-panel");
            _modePanels[VoxelEditMode.Rotation] = _root.Q("rotate-panel");
            _modePanels[VoxelEditMode.FillKitchen] = _root.Q("fill-panel");
            _modePanels[VoxelEditMode.Color] = _root.Q("color-panel");

            BindModeButton(VoxelEditMode.Placement, "mode-move");
            BindModeButton(VoxelEditMode.Scale, "mode-scale");
            BindModeButton(VoxelEditMode.Rotation, "mode-rotate");
            BindModeButton(VoxelEditMode.FillKitchen, "mode-fill");
            BindModeButton(VoxelEditMode.Color, "mode-color");

            if (_doneButton != null) _doneButton.clicked += () => stateManager?.ExitEdit();

            BindScaleControls();
            BindMoveControls();
            BindRotateControls();
            BindFillControls();
            BindTextureControls();
            BindVariantControls();

            BuildCategories();
            BuildCatalog();
            OnModeChanged(VoxelEditMode.Scale);
        }

        void BindModeButton(VoxelEditMode mode, string name)
        {
            var button = _root.Q<Button>(name);
            if (button == null) return;
            _modeButtons[mode] = button;
            button.clicked += () => stateManager?.SetMode(mode);
        }

        void BindScaleControls()
        {
            _widthSlider = _root.Q<Slider>("width-slider");
            _depthSlider = _root.Q<Slider>("depth-slider");
            _heightSlider = _root.Q<Slider>("height-slider");
            _widthValue = _root.Q<Label>("width-value");
            _depthValue = _root.Q<Label>("depth-value");
            _heightValue = _root.Q<Label>("height-value");

            SetupSizeSlider(_widthSlider);
            SetupSizeSlider(_depthSlider);
            SetupSizeSlider(_heightSlider);
            _widthSlider?.RegisterValueChangedCallback(_ => OnSizeChanged());
            _depthSlider?.RegisterValueChangedCallback(_ => OnSizeChanged());
            _heightSlider?.RegisterValueChangedCallback(_ => OnSizeChanged());

            BindSizeStep("width-dec", _widthSlider, -SizeStep);
            BindSizeStep("width-inc", _widthSlider, SizeStep);
            BindSizeStep("depth-dec", _depthSlider, -SizeStep);
            BindSizeStep("depth-inc", _depthSlider, SizeStep);
            BindSizeStep("height-dec", _heightSlider, -SizeStep);
            BindSizeStep("height-inc", _heightSlider, SizeStep);
        }

        static void SetupSizeSlider(Slider slider)
        {
            if (slider == null) return;
            slider.lowValue = MinSize;
            slider.highValue = MaxSize;
        }

        void BindSizeStep(string name, Slider slider, float delta)
        {
            var button = _root.Q<Button>(name);
            if (button != null) button.clicked += () => SetSliderRounded(slider, slider.value + delta);
        }

        static void SetSliderRounded(Slider slider, float value)
        {
            if (slider == null) return;
            slider.value = Mathf.Clamp(Mathf.Round(value * 10f) / 10f, MinSize, MaxSize);
        }

        void BindMoveControls()
        {
            BindMoveButton("move-forward", MoveDir.Forward);
            BindMoveButton("move-back", MoveDir.Back);
            BindMoveButton("move-left", MoveDir.Left);
            BindMoveButton("move-right", MoveDir.Right);
        }

        void BindMoveButton(string name, MoveDir dir)
        {
            var button = _root.Q<Button>(name);
            if (button != null) button.clicked += () =>
            {
                var controller = stateManager != null ? stateManager.Controller : null;
                if (controller == null) return;
                controller.MoveTo(controller.transform.position + ViewRelativeDelta(dir) * MoveStep);
                ShowToast("Moved 10 cm");
            };
        }

        // The pad is screen-relative: "Up" pushes the kitchen away from the
        // viewer along the floor, "Right" to the viewer's right - regardless of
        // where they walk or how the voxel is rotated. World-fixed axes felt
        // arbitrary once the user moved around the space.
        Vector3 ViewRelativeDelta(MoveDir dir)
        {
            var cam = arCamera != null ? arCamera : Camera.main;
            Vector3 forward = Vector3.forward;
            Vector3 right = Vector3.right;
            if (cam != null)
            {
                forward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up);
                // Looking near-straight down/up collapses forward; fall back to
                // the screen-up direction so the pad still tracks the view.
                if (forward.sqrMagnitude < 1e-4f)
                    forward = Vector3.ProjectOnPlane(cam.transform.up, Vector3.up);
                forward.Normalize();
                right = Vector3.ProjectOnPlane(cam.transform.right, Vector3.up).normalized;
            }

            return dir switch
            {
                MoveDir.Forward => forward,
                MoveDir.Back => -forward,
                MoveDir.Right => right,
                _ => -right
            };
        }

        void BindRotateControls()
        {
            _rotationValue = _root.Q<Label>("rotation-value");
            var left = _root.Q<Button>("rotate-left");
            var right = _root.Q<Button>("rotate-right");
            if (left != null) left.clicked += () => RotateBy(-15f);
            if (right != null) right.clicked += () => RotateBy(15f);

            var row = _root.Q("snap-row");
            if (row == null) return;
            row.Clear();
            _snapButtons.Clear();
            int[] snaps = { 0, 45, 90, 135, 180, 225, 270, 315 };
            foreach (int snap in snaps)
            {
                var button = new Button(() => SetRotation(snap)) { text = snap.ToString() };
                button.AddToClassList("snap-button");
                row.Add(button);
                _snapButtons.Add(button);
            }
        }

        void BindFillControls()
        {
            _freeLabel = _root.Q<Label>("free-label");
            _mandatoryLabel = _root.Q<Label>("mandatory-label");
            _placedStrip = _root.Q<ScrollView>("placed-strip");
            _catalogGrid = _root.Q("catalog-grid");
            _addWorktopButton = _root.Q<Button>("add-worktop-button");
            if (_addWorktopButton != null)
                _addWorktopButton.clicked += () =>
                {
                    if (_layout == null || !_layout.CanAddFiller) return;
                    if (_layout.Placed.Count == 0)
                    {
                        _layout.AddFillerAt(0);
                        ShowToast("Worktop added");
                        return;
                    }

                    _choosingWorktopSlot = !_choosingWorktopSlot;
                    UpdateFillState();
                };
        }

        void BindTextureControls()
        {
            _textureGrid = _root.Q("texture-grid");
            _textureHint = _root.Q<Label>("texture-hint");
        }

        void BindVariantControls()
        {
            var close = _root.Q<Button>("variant-close");
            var backdrop = _root.Q("variant-backdrop");
            if (close != null) close.clicked += CloseVariantSheet;
            backdrop?.RegisterCallback<PointerDownEvent>(_ => CloseVariantSheet());
        }

        void OnVoxelPlaced()
        {
            BindLayout();
            SyncFromController();
            UpdateFillState();
        }

        void OnEditingChanged(bool editing)
        {
            EnsureRoot();
            if (editing)
            {
                Show();
                if (stateManager != null && stateManager.CurrentMode == VoxelEditMode.None)
                    stateManager.SetMode(VoxelEditMode.Scale);
                else if (stateManager != null)
                    OnModeChanged(stateManager.CurrentMode);
            }
            else
            {
                CloseVariantSheet();
                Hide();
            }
        }

        void OnModeChanged(VoxelEditMode mode)
        {
            EnsureRoot();
            if (mode == VoxelEditMode.None) mode = VoxelEditMode.Scale;

            foreach (var pair in _modeButtons)
                pair.Value.EnableInClassList("is-selected", pair.Key == mode);

            foreach (var pair in _modePanels)
                if (pair.Value != null)
                    pair.Value.EnableInClassList("is-visible", pair.Key == mode);

            UpdateHeader(mode);
            if (mode != VoxelEditMode.Color && _textureTarget != null) DeselectTexture();
            if (mode == VoxelEditMode.Scale) SyncFromController();
            if (mode == VoxelEditMode.FillKitchen) UpdateFillState();
            if (mode == VoxelEditMode.Color) EnterTextureMode();
            UpdateRotationReadout();
        }

        void UpdateHeader(VoxelEditMode mode)
        {
            string title;
            string hint;
            string icon;
            switch (mode)
            {
                case VoxelEditMode.Placement:
                    title = "Move"; hint = "Reposition along the floor"; icon = "M"; break;
                case VoxelEditMode.Rotation:
                    title = "Rotate"; hint = "Align with your wall"; icon = "R"; break;
                case VoxelEditMode.FillKitchen:
                    title = "Add units"; hint = "Choose a type, then pick a size"; icon = "+"; break;
                case VoxelEditMode.Color:
                    title = "Texture"; hint = "Tap an element, pick a finish"; icon = "T"; break;
                default:
                    title = "Scale"; hint = "Set the kitchen footprint"; icon = "S"; break;
            }

            if (_headerTitle != null) _headerTitle.text = title;
            if (_headerHint != null) _headerHint.text = hint;
            if (_headerIcon != null) _headerIcon.text = icon;
            if (_headerRight != null)
                _headerRight.Clear();
        }

        void Show()
        {
            if (_root != null) _root.style.display = DisplayStyle.Flex;
        }

        void Hide()
        {
            if (_root != null) _root.style.display = DisplayStyle.None;
        }

        void BindLayout()
        {
            UnbindLayout();
            var controller = stateManager != null ? stateManager.Controller : null;
            _layout = controller != null ? controller.GetComponent<KitchenLayoutController>() : null;
            if (_layout != null)
                _layout.OnLayoutChanged += UpdateFillState;
        }

        void UnbindLayout()
        {
            if (_layout != null)
                _layout.OnLayoutChanged -= UpdateFillState;
            _layout = null;
        }

        void SyncFromController()
        {
            var controller = stateManager != null ? stateManager.Controller : null;
            if (controller == null || _widthSlider == null || _depthSlider == null || _heightSlider == null) return;

            _syncing = true;
            _widthSlider.value = Mathf.Clamp(controller.Width, MinSize, MaxSize);
            _depthSlider.value = Mathf.Clamp(controller.Depth, MinSize, MaxSize);
            _heightSlider.value = Mathf.Clamp(controller.Height, MinSize, MaxSize);
            _syncing = false;
            UpdateSizeLabels();
        }

        void OnSizeChanged()
        {
            if (_syncing) return;
            var controller = stateManager != null ? stateManager.Controller : null;
            if (controller == null || _widthSlider == null || _depthSlider == null || _heightSlider == null) return;
            controller.Resize(Rounded(_widthSlider.value), Rounded(_depthSlider.value), Rounded(_heightSlider.value));
            SyncFromController();
        }

        static float Rounded(float v) => Mathf.Round(v * 10f) / 10f;

        void UpdateSizeLabels()
        {
            if (_widthValue != null && _widthSlider != null) _widthValue.text = $"{_widthSlider.value:0.0}m";
            if (_depthValue != null && _depthSlider != null) _depthValue.text = $"{_depthSlider.value:0.0}m";
            if (_heightValue != null && _heightSlider != null) _heightValue.text = $"{_heightSlider.value:0.0}m";
        }

        void RotateBy(float degrees)
        {
            var controller = stateManager != null ? stateManager.Controller : null;
            if (controller == null) return;
            controller.Rotate(degrees);
            UpdateRotationReadout();
        }

        void SetRotation(float degrees)
        {
            var controller = stateManager != null ? stateManager.Controller : null;
            if (controller == null) return;
            controller.transform.rotation = Quaternion.Euler(0f, degrees, 0f);
            UpdateRotationReadout();
        }

        void UpdateRotationReadout()
        {
            var controller = stateManager != null ? stateManager.Controller : null;
            float y = controller != null ? Mathf.Repeat(controller.transform.eulerAngles.y, 360f) : 0f;
            if (_rotationValue != null) _rotationValue.text = $"{Mathf.RoundToInt(y)} deg";
            foreach (var button in _snapButtons)
            {
                if (int.TryParse(button.text, out int snap))
                    button.EnableInClassList("is-selected", Mathf.Abs(Mathf.DeltaAngle(y, snap)) < 1f);
            }
        }

        void BuildCategories()
        {
            var row = _root.Q("category-row");
            if (row == null) return;
            row.Clear();
            _categoryButtons.Clear();
            AddCategory(row, KitchenElementGroup.Storage, "Fridges");
            AddCategory(row, KitchenElementGroup.Washing, "Sinks");
            AddCategory(row, KitchenElementGroup.Cooking, "Stoves");
            UpdateCategoryClasses();
        }

        void AddCategory(VisualElement row, KitchenElementGroup group, string label)
        {
            var button = new Button(() =>
            {
                _activeGroup = group;
                UpdateCategoryClasses();
                BuildCatalog();
            }) { text = label };
            button.AddToClassList("category-button");
            row.Add(button);
            _categoryButtons[group] = button;
        }

        void UpdateCategoryClasses()
        {
            foreach (var pair in _categoryButtons)
                pair.Value.EnableInClassList("is-selected", pair.Key == _activeGroup);
        }

        void BuildCatalog()
        {
            if (_catalogGrid == null) return;
            _catalogGrid.Clear();
            _catalogButtons.Clear();
            _catalogDefinitions.Clear();
            _catalogActionLabels.Clear();
            if (definitions == null) return;

            foreach (var def in definitions)
            {
                if (def == null || def.IsFiller || def.Group != _activeGroup) continue;
                var captured = def;
                var button = CreateCatalogButton(captured);
                _catalogGrid.Add(button);
                _catalogButtons.Add(button);
                _catalogDefinitions[button] = captured;
            }
            UpdateFillState();
        }

        Button CreateCatalogButton(KitchenElementDefinition def)
        {
            var button = new Button(() => AddDefinition(def));
            button.AddToClassList("catalog-button");

            string title = string.IsNullOrEmpty(def.Code) ? def.DisplayName : $"{def.Code} {def.DisplayName}";
            string price = def.BasePrice > 0f ? $"{def.BasePrice:N0} €" : "Included";
            int widthCm = Mathf.RoundToInt(def.WidthMeters * 100f);

            var top = new VisualElement();
            top.AddToClassList("catalog-card-top");
            var titleLabel = new Label(title);
            titleLabel.AddToClassList("catalog-card-title");
            var priceLabel = new Label(price);
            priceLabel.AddToClassList("catalog-card-price");
            top.Add(titleLabel);
            top.Add(priceLabel);

            var sizeLabel = new Label($"{widthCm} cm wide");
            sizeLabel.AddToClassList("catalog-card-size");
            var actionLabel = new Label("Tap to add");
            actionLabel.AddToClassList("catalog-card-action");
            var bottom = new VisualElement();
            bottom.AddToClassList("catalog-card-bottom");
            bottom.Add(sizeLabel);
            bottom.Add(actionLabel);

            button.Add(top);
            button.Add(bottom);
            _catalogActionLabels[button] = actionLabel;
            return button;
        }

        void AddDefinition(KitchenElementDefinition def)
        {
            if (_layout == null || def == null) return;
            var result = _layout.TryAdd(def);
            switch (result)
            {
                case KitchenLayoutController.AddResult.Ok:
                    ShowToast($"{def.DisplayName} added");
                    break;
                case KitchenLayoutController.AddResult.NoDepth:
                    ShowToast($"Voxel too shallow for {def.DisplayName}");
                    break;
                case KitchenLayoutController.AddResult.NoFit:
                    ShowToast("Not enough space - scale up the voxel");
                    break;
                default:
                    ShowToast("Missing prefab");
                    break;
            }
        }

        void UpdateFillState()
        {
            if (_layout == null) BindLayout();
            float remaining = _layout != null ? _layout.RemainingLength : 0f;
            if (_freeLabel != null) _freeLabel.text = $"{remaining:0.0} m free";
            if (_addWorktopButton != null)
            {
                if (_layout == null || !_layout.CanAddFiller) _choosingWorktopSlot = false;
                _addWorktopButton.style.display = _layout != null && _layout.CanAddFiller
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
                _addWorktopButton.text = _choosingWorktopSlot ? "Cancel" : "+ Worktop";
                _addWorktopButton.EnableInClassList("is-selecting", _choosingWorktopSlot);
            }

            UpdateMandatory();
            UpdatePlacedStrip();
            foreach (var button in _catalogButtons)
            {
                _catalogDefinitions.TryGetValue(button, out var def);
                bool depthFits = _layout != null && def != null && _layout.DepthFits(def);
                bool lengthFits = _layout != null && def != null && _layout.LengthFits(def);
                bool fits = depthFits && lengthFits;
                button.SetEnabled(fits);
                if (_catalogActionLabels.TryGetValue(button, out var action))
                    action.text = fits
                        ? "Tap to add"
                        : !depthFits
                            ? $"Needs {def.DepthMeters:0.0} m depth"
                            : $"Needs {def.WidthMeters:0.0} m free";
            }
        }

        void UpdateMandatory()
        {
            if (_mandatoryLabel == null) return;
            MissingMandatory.Clear();
            if (definitions != null)
            {
                foreach (var def in definitions)
                {
                    if (def == null || !def.IsMandatory) continue;
                    bool found = false;
                    if (_layout != null)
                    {
                        foreach (var view in _layout.Placed)
                        {
                            if (view != null && view.Definition == def)
                            {
                                found = true;
                                break;
                            }
                        }
                    }
                    if (!found) MissingMandatory.Add(def.DisplayName);
                }
            }

            bool visible = MissingMandatory.Count > 0;
            _mandatoryLabel.text = visible ? "Missing: " + string.Join(", ", MissingMandatory) : "";
            _mandatoryLabel.EnableInClassList("is-visible", visible);
        }

        void UpdatePlacedStrip()
        {
            if (_placedStrip == null) return;
            _placedStrip.contentContainer.Clear();
            _placedButtons.Clear();

            if (_layout == null)
            {
                _placedStrip.EnableInClassList("is-visible", false);
                return;
            }

            int units = _layout.Placed.Count;
            bool hasFiller = _layout.HasFiller;
            bool canAddFiller = _layout.CanAddFiller;

            // Keep an empty layout out of the way. Its single worktop action lives
            // in the compact toolbar; the strip appears once there is real content.
            bool visible = units > 0 || hasFiller;
            _placedStrip.EnableInClassList("is-visible", visible);
            if (!visible) return;

            // Walk slots 0..units: a desk chip / '+' marker can sit at each slot,
            // a placed unit between consecutive slots.
            for (int i = 0; i <= units; i++)
            {
                if (hasFiller && _layout.FillerSlot == i)
                    AddFillerChip();
                else if (!hasFiller && canAddFiller && _choosingWorktopSlot)
                    AddInsertMarker(i);

                if (i == units) break;

                var view = _layout.Placed[i];
                if (view != null && view.Definition != null)
                    AddPlacedButton(view);
            }
        }

        void AddPlacedButton(KitchenElementView view)
        {
            var captured = view;
            float placedPrice = view.Definition.GetVariantPrice(view.CurrentVariantIndex);
            int widthCm = Mathf.RoundToInt(view.Definition.WidthMeters * 100f);
            string placedLabel = string.IsNullOrEmpty(view.Definition.Code)
                ? view.Definition.DisplayName
                : $"{view.Definition.Code} {view.Definition.DisplayName}";
            placedLabel += "  ×";
            placedLabel += placedPrice > 0f
                ? $"\n{widthCm} cm  ·  {placedPrice:N0} €"
                : $"\n{widthCm} cm";
            var button = new Button(() =>
            {
                if (_layout != null && _layout.Remove(captured))
                    ShowToast($"{captured.Definition.DisplayName} removed");
            })
            {
                text = placedLabel,
                tooltip = "Tap to remove"
            };
            button.AddToClassList("placed-button");
            _placedStrip.contentContainer.Add(button);
            _placedButtons.Add(button);
        }

        void AddInsertMarker(int slot)
        {
            int captured = slot;
            var button = new Button(() =>
            {
                _choosingWorktopSlot = false;
                _layout?.AddFillerAt(captured);
                ShowToast("Working desk added");
            })
            { text = "Place here" };
            button.AddToClassList("insert-marker");
            _placedStrip.contentContainer.Add(button);
        }

        void AddFillerChip()
        {
            int widthCm = Mathf.RoundToInt(_layout.FillerWidth * 100f);
            var button = new Button(() =>
            {
                _layout?.RemoveFiller();
                ShowToast("Working desk removed");
            })
            { text = $"Worktop\n{widthCm} cm  ·  tap to remove" };
            button.AddToClassList("placed-button");
            button.AddToClassList("filler-chip");
            _placedStrip.contentContainer.Add(button);
        }

        static string GroupLabel(KitchenElementGroup group) =>
            group switch
            {
                KitchenElementGroup.Washing => "Sink",
                KitchenElementGroup.Cooking => "Stove",
                _ => "Fridge"
            };

        // Entering Texture mode: clear any prior selection and prompt the user to
        // tap an element. The grid is populated per-element once one is tapped.
        void EnterTextureMode() => DeselectTexture();

        void DeselectTexture()
        {
            if (_textureTarget != null) _textureTarget.SetSelected(false);
            _textureTarget = null;
            _textureButtons.Clear();
            if (_textureGrid != null) _textureGrid.Clear();
            if (_textureHint != null)
            {
                _textureHint.text = "Tap an element to texture it";
                _textureHint.style.display = DisplayStyle.Flex;
            }
        }

        // A tap in Texture mode picked this element: show the finishes its
        // definition allows (different sets for an appliance vs the desk).
        void SelectTextureTarget(KitchenElementView view)
        {
            if (view == null || view.Definition == null) return;
            if (_textureTarget == view) return;
            if (_textureTarget != null) _textureTarget.SetSelected(false);
            _textureTarget = view;
            _textureTarget.SetSelected(true);
            BuildTextureGrid(view);
        }

        void BuildTextureGrid(KitchenElementView view)
        {
            if (_textureGrid == null) return;
            _textureGrid.Clear();
            _textureButtons.Clear();

            var textures = view.Definition.CompatibleTextures;
            int count = textures != null ? textures.Count : 0;

            if (_textureHint != null)
                _textureHint.text = count > 0
                    ? $"{view.Definition.DisplayName} finish"
                    : $"No finishes set for {view.Definition.DisplayName}";

            for (int i = 0; i < count; i++)
            {
                var tex = textures[i];
                if (tex == null) continue;
                var capturedTex = tex;
                var capturedView = view;
                var button = new Button(() => ChooseTexture(capturedView, capturedTex));
                button.AddToClassList("texture-swatch");
                button.style.backgroundImage = new StyleBackground(tex);
                button.EnableInClassList("is-selected", view.ChosenTexture == tex);
                _textureGrid.Add(button);
                _textureButtons.Add(button);
            }
        }

        // Takes the element explicitly (captured per swatch) so a finish always
        // applies to the element it belongs to, independent of selection state.
        void ChooseTexture(KitchenElementView view, Texture2D texture)
        {
            if (view == null || texture == null) return;
            view.ApplyTexture(texture);
            if (_textureTarget == view) BuildTextureGrid(view); // refresh the selected-swatch highlight
            ShowToast("Finish applied");
        }

        void OpenVariantSheet(KitchenElementView view)
        {
            if (view == null || view.Definition == null || view.Definition.VariantCount <= 1) return;
            _selectedVariantView = view;
            if (_variantTitle != null)
                _variantTitle.text = string.IsNullOrEmpty(view.Definition.Code)
                    ? view.Definition.DisplayName
                    : $"{view.Definition.Code} {view.Definition.DisplayName}";
            if (_variantList != null)
            {
                _variantList.Clear();
                for (int i = 0; i < view.Definition.VariantCount; i++)
                {
                    int index = i;
                    var prefab = view.Definition.GetVariant(i);
                    string variantName = prefab != null ? prefab.name : $"Variant {i + 1}";
                    float variantPrice = view.Definition.GetVariantPrice(i);
                    string variantLabel = variantPrice > 0f ? $"{variantName}\n{variantPrice:N0} €" : variantName;
                    var button = new Button(() => ChooseVariant(index)) { text = variantLabel };
                    button.AddToClassList("variant-button");
                    button.EnableInClassList("is-selected", i == view.CurrentVariantIndex);
                    _variantList.Add(button);
                }
            }
            _variantOverlay?.EnableInClassList("is-visible", true);
        }

        void ChooseVariant(int index)
        {
            if (_selectedVariantView == null) return;
            _selectedVariantView.ApplyVariant(index);
            _layout?.NotifyLayoutChanged();
            ShowToast("Variant swapped");
            CloseVariantSheet();
            UpdatePlacedStrip();
        }

        void CloseVariantSheet()
        {
            _selectedVariantView = null;
            _variantOverlay?.EnableInClassList("is-visible", false);
        }

        void ShowToast(string text)
        {
            if (_toastLabel == null) return;
            _toastLabel.text = text;
            _toastLabel.EnableInClassList("is-visible", true);
            _toastHide?.Pause();
            _toastHide = _toastLabel.schedule.Execute(() => _toastLabel.EnableInClassList("is-visible", false))
                .StartingIn(Mathf.RoundToInt(ToastDuration * 1000f));
        }

        bool IsPointerOverOwnUi(Vector2 screenPos)
        {
            if (_root == null || _root.panel == null || _root.resolvedStyle.display == DisplayStyle.None)
                return false;
            if (_variantOverlay != null && _variantOverlay.ClassListContains("is-visible"))
                return true;

            Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(_root.panel, screenPos);

            // Authoritative hit-test: walk up from the picked element to see if the
            // tap landed inside the context panel or the bottom mode bar (this is
            // what makes swatch/button taps not fall through to the world raycast).
            for (var e = _root.panel.Pick(panelPos); e != null; e = e.parent)
                if (e == _contextPanel || e.name == "mode-bar")
                    return true;

            // Bounds fallback in case something along the path isn't pickable.
            if (_contextPanel != null && _contextPanel.worldBound.Contains(panelPos)) return true;
            var modeBar = _root.Q("mode-bar");
            return modeBar != null && modeBar.worldBound.Contains(panelPos);
        }

        static KitchenElementView RaycastForElement(Ray ray)
        {
            var hits = Physics.RaycastAll(ray, 50f);
            KitchenElementView best = null;
            float bestDist = float.MaxValue;
            foreach (var hit in hits)
            {
                var view = hit.collider.GetComponentInParent<KitchenElementView>();
                if (view != null && hit.distance < bestDist)
                {
                    best = view;
                    bestDist = hit.distance;
                }
            }
            return best;
        }
    }
}
