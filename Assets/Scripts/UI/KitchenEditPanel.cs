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
        [SerializeField] Material mainMaterial;
        [SerializeField] Material secondaryMaterial;
        [SerializeField] KitchenElementDefinition[] definitions;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
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
        Button _removeLastButton;
        Button _mainColorButton;
        Button _secondaryColorButton;

        readonly Dictionary<VoxelEditMode, Button> _modeButtons = new();
        readonly Dictionary<VoxelEditMode, VisualElement> _modePanels = new();
        readonly Dictionary<KitchenElementGroup, Button> _categoryButtons = new();
        readonly Dictionary<Button, KitchenElementDefinition> _catalogDefinitions = new();
        readonly List<Button> _catalogButtons = new();
        readonly List<Button> _placedButtons = new();
        readonly List<Button> _snapButtons = new();

        Slider _widthSlider;
        Slider _depthSlider;
        Slider _heightSlider;
        Slider _redSlider;
        Slider _greenSlider;
        Slider _blueSlider;

        Label _widthValue;
        Label _depthValue;
        Label _heightValue;
        Label _rotationValue;
        Label _freeLabel;
        Label _mandatoryLabel;
        Label _redValue;
        Label _greenValue;
        Label _blueValue;
        Label _hexLabel;

        ScrollView _placedStrip;
        VisualElement _catalogGrid;
        VisualElement _colorPreview;

        KitchenLayoutController _layout;
        KitchenElementGroup _activeGroup = KitchenElementGroup.Storage;
        Material _activeMaterial;
        KitchenElementView _selectedVariantView;
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

        void Update()
        {
            if (stateManager == null || stateManager.CurrentMode != VoxelEditMode.FillKitchen)
                return;
            if (!TryGetTap(out var screenPos) || IsPointerOverOwnUi(screenPos))
                return;

            var cam = arCamera != null ? arCamera : Camera.main;
            if (cam == null) return;

            var view = RaycastForElement(cam.ScreenPointToRay(screenPos));
            if (view != null) OpenVariantSheet(view);
        }

        void EnsureRoot()
        {
            if (_root != null) return;
            if (_document == null) _document = GetComponent<UIDocument>();
            _root = _document.rootVisualElement;
            if (_root == null) return;

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
            BindColorControls();
            BindVariantControls();

            _activeMaterial = mainMaterial;
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
            _removeLastButton = _root.Q<Button>("remove-last-button");
            if (_removeLastButton != null)
                _removeLastButton.clicked += () => _layout?.RemoveLast();
        }

        void BindColorControls()
        {
            _mainColorButton = _root.Q<Button>("main-color-button");
            _secondaryColorButton = _root.Q<Button>("secondary-color-button");
            _colorPreview = _root.Q("color-preview");
            _redSlider = _root.Q<Slider>("red-slider");
            _greenSlider = _root.Q<Slider>("green-slider");
            _blueSlider = _root.Q<Slider>("blue-slider");
            _redValue = _root.Q<Label>("red-value");
            _greenValue = _root.Q<Label>("green-value");
            _blueValue = _root.Q<Label>("blue-value");
            _hexLabel = _root.Q<Label>("hex-label");

            if (_mainColorButton != null) _mainColorButton.clicked += () => SelectMaterial(mainMaterial);
            if (_secondaryColorButton != null) _secondaryColorButton.clicked += () => SelectMaterial(secondaryMaterial);
            _redSlider?.RegisterValueChangedCallback(_ => OnColorChanged());
            _greenSlider?.RegisterValueChangedCallback(_ => OnColorChanged());
            _blueSlider?.RegisterValueChangedCallback(_ => OnColorChanged());
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
            if (mode == VoxelEditMode.Scale) SyncFromController();
            if (mode == VoxelEditMode.FillKitchen) UpdateFillState();
            if (mode == VoxelEditMode.Color) SelectMaterial(_activeMaterial != null ? _activeMaterial : mainMaterial);
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
                    title = "Fill"; hint = "Tap to add kitchen units"; icon = "F"; break;
                case VoxelEditMode.Color:
                    title = "Color"; hint = "Pick a cabinet finish"; icon = "C"; break;
                default:
                    title = "Scale"; hint = "Set the kitchen footprint"; icon = "S"; break;
            }

            if (_headerTitle != null) _headerTitle.text = title;
            if (_headerHint != null) _headerHint.text = hint;
            if (_headerIcon != null) _headerIcon.text = icon;
            if (_headerRight != null)
            {
                _headerRight.Clear();
                if (mode == VoxelEditMode.FillKitchen && _freeLabel != null)
                {
                    var label = new Label(_freeLabel.text);
                    label.AddToClassList("free-label");
                    _headerRight.Add(label);
                }
            }
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
            AddCategory(row, KitchenElementGroup.Storage, "Fridge");
            AddCategory(row, KitchenElementGroup.Washing, "Sink");
            AddCategory(row, KitchenElementGroup.Cooking, "Stove");
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
            if (definitions == null) return;

            foreach (var def in definitions)
            {
                if (def == null || def.Group != _activeGroup) continue;
                var captured = def;
                var button = new Button(() => AddDefinition(captured))
                {
                    text = FormatCatalogLabel(captured)
                };
                button.AddToClassList("catalog-button");
                _catalogGrid.Add(button);
                _catalogButtons.Add(button);
                _catalogDefinitions[button] = captured;
            }
            UpdateFillState();
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
            if (stateManager != null && stateManager.CurrentMode == VoxelEditMode.FillKitchen)
                UpdateHeader(VoxelEditMode.FillKitchen);
            if (_removeLastButton != null)
                _removeLastButton.SetEnabled(_layout != null && _layout.Placed.Count > 0);

            UpdateMandatory();
            UpdatePlacedStrip();
            foreach (var button in _catalogButtons)
            {
                _catalogDefinitions.TryGetValue(button, out var def);
                button.SetEnabled(_layout != null && def != null && _layout.LengthFits(def));
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

            bool hasPlaced = _layout != null && _layout.Placed.Count > 0;
            _placedStrip.EnableInClassList("is-visible", hasPlaced);
            if (!hasPlaced) return;

            for (int i = 0; i < _layout.Placed.Count; i++)
            {
                var view = _layout.Placed[i];
                if (view == null || view.Definition == null) continue;
                var captured = view;
                var button = new Button(() => OpenVariantSheet(captured))
                {
                    text = $"{GroupLabel(view.Definition.Group)} {view.Definition.DisplayName}"
                };
                button.AddToClassList("placed-button");
                _placedStrip.contentContainer.Add(button);
                _placedButtons.Add(button);
            }
        }

        static string FormatCatalogLabel(KitchenElementDefinition def)
        {
            int w = Mathf.RoundToInt(def.WidthMeters * 100f);
            int h = Mathf.RoundToInt(def.HeightMeters * 100f);
            int d = Mathf.RoundToInt(def.DepthMeters * 100f);
            string title = string.IsNullOrEmpty(def.Code) ? def.DisplayName : $"{def.Code} {def.DisplayName}";
            return $"{title}\n{w}x{h}x{d} cm";
        }

        static string GroupLabel(KitchenElementGroup group) =>
            group switch
            {
                KitchenElementGroup.Washing => "Sink",
                KitchenElementGroup.Cooking => "Stove",
                _ => "Fridge"
            };

        void SelectMaterial(Material mat)
        {
            if (mat == null) return;
            _activeMaterial = mat;
            _mainColorButton?.EnableInClassList("is-selected", mat == mainMaterial);
            _secondaryColorButton?.EnableInClassList("is-selected", mat == secondaryMaterial);
            SyncColorFromMaterial();
        }

        void SyncColorFromMaterial()
        {
            if (_activeMaterial == null || _redSlider == null || _greenSlider == null || _blueSlider == null) return;
            Color c = GetColor(_activeMaterial);
            _syncing = true;
            _redSlider.value = c.r;
            _greenSlider.value = c.g;
            _blueSlider.value = c.b;
            _syncing = false;
            UpdateColorLabels(c);
        }

        void OnColorChanged()
        {
            if (_syncing || _activeMaterial == null || _redSlider == null || _greenSlider == null || _blueSlider == null)
                return;
            var c = new Color(_redSlider.value, _greenSlider.value, _blueSlider.value, 1f);
            SetColor(_activeMaterial, c);
            UpdateColorLabels(c);
        }

        static Color GetColor(Material material) =>
            material.HasProperty(BaseColorId) ? material.GetColor(BaseColorId) : material.color;

        static void SetColor(Material material, Color color)
        {
            if (material.HasProperty(BaseColorId)) material.SetColor(BaseColorId, color);
            material.color = color;
        }

        void UpdateColorLabels(Color c)
        {
            int r = Mathf.RoundToInt(c.r * 255f);
            int g = Mathf.RoundToInt(c.g * 255f);
            int b = Mathf.RoundToInt(c.b * 255f);
            if (_redValue != null) _redValue.text = r.ToString();
            if (_greenValue != null) _greenValue.text = g.ToString();
            if (_blueValue != null) _blueValue.text = b.ToString();
            if (_hexLabel != null) _hexLabel.text = $"#{r:X2}{g:X2}{b:X2}";
            if (_colorPreview != null) _colorPreview.style.backgroundColor = c;
        }

        void OpenVariantSheet(KitchenElementView view)
        {
            if (view == null || view.Definition == null || view.Definition.VariantCount <= 1) return;
            _selectedVariantView = view;
            if (_variantTitle != null)
                _variantTitle.text = $"{GroupLabel(view.Definition.Group)} - {view.Definition.DisplayName}";
            if (_variantList != null)
            {
                _variantList.Clear();
                for (int i = 0; i < view.Definition.VariantCount; i++)
                {
                    int index = i;
                    var prefab = view.Definition.GetVariant(i);
                    string name = prefab != null ? prefab.name : $"Variant {i + 1}";
                    var button = new Button(() => ChooseVariant(index)) { text = name };
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
            if (_root == null || _root.resolvedStyle.display == DisplayStyle.None) return false;
            if (_contextPanel != null && ContainsScreenPoint(_contextPanel, screenPos)) return true;
            var modeBar = _root.Q("mode-bar");
            if (modeBar != null && ContainsScreenPoint(modeBar, screenPos)) return true;
            if (_variantOverlay != null && _variantOverlay.ClassListContains("is-visible")) return true;
            return false;
        }

        static bool ContainsScreenPoint(VisualElement element, Vector2 screenPos)
        {
            if (element == null || element.panel == null) return false;
            Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(element.panel, screenPos);
            return element.worldBound.Contains(panelPos);
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
