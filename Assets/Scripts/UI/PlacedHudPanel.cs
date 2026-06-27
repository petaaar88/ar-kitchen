using System.Collections.Generic;
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
        VisualElement _congratsCard;
        VisualElement _congratsBackdrop;
        VisualElement _congratsBadge;
        VisualElement _congratsRing;
        Label _congratsTitle;
        Label _congratsBody;
        Button _congratsClose;
        List<VisualElement> _confettiPieces;
        IVisualElementScheduledItem _congratsAnim;
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
            _congratsCard = _root.Q<VisualElement>("congrats-card");
            _congratsBackdrop = _root.Q<VisualElement>("congrats-backdrop");
            _congratsBadge = _root.Q<VisualElement>("congrats-badge");
            _congratsRing = _root.Q<VisualElement>("congrats-ring");
            _congratsTitle = _root.Q<Label>("congrats-title");
            _congratsBody = _root.Q<Label>("congrats-body");
            _congratsClose = _root.Q<Button>("congrats-close");
            _confettiPieces = new List<VisualElement>();

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

        void ShowCongrats()
        {
            if (_congratsOverlay == null || _congratsCard == null) return;
            _congratsAnim?.Pause();
            ClearConfetti();

            _congratsOverlay.style.display = DisplayStyle.Flex;
            _congratsOverlay.style.opacity = 1f;

            // Hidden starting states - the scheduler eases each one in on its own beat.
            if (_congratsBackdrop != null) _congratsBackdrop.style.opacity = 0f;
            _congratsCard.style.opacity = 0f;
            SetScale(_congratsCard, 0.85f);
            if (_congratsBadge != null) SetScale(_congratsBadge, 0f);
            if (_congratsRing != null) { _congratsRing.style.opacity = 0f; SetScale(_congratsRing, 0.4f); }
            HidePiece(_congratsTitle, 26f);
            HidePiece(_congratsBody, 22f);
            HidePiece(_congratsClose, 20f);

            bool confettiSpawned = false;
            float startTime = Time.time;
            const float duration = 4.2f;

            _congratsAnim = _congratsOverlay.schedule.Execute(() =>
            {
                float e = Time.time - startTime;
                float dt = Mathf.Min(Time.deltaTime, 0.05f);

                if (_congratsBackdrop != null)
                    _congratsBackdrop.style.opacity = Clamp01(e / 0.25f);

                // Card: subtle settle so it doesn't fight the badge's bounce.
                SetScale(_congratsCard, Mathf.Lerp(0.85f, 1f, EaseOut(Clamp01((e - 0.08f) / 0.35f))));
                _congratsCard.style.opacity = Clamp01((e - 0.08f) / 0.2f);

                // Badge: the hero pop, with a shockwave ring bursting behind it.
                if (_congratsBadge != null)
                    SetScale(_congratsBadge, EaseOutBack(Clamp01((e - 0.22f) / 0.5f)));
                if (_congratsRing != null)
                {
                    float rt = Clamp01((e - 0.24f) / 0.55f);
                    SetScale(_congratsRing, Mathf.Lerp(0.4f, 1.9f, EaseOut(rt)));
                    _congratsRing.style.opacity = (1f - rt) * 0.55f;
                }

                // Copy + CTA rise in, staggered.
                ShowPiece(_congratsTitle, (e - 0.44f) / 0.3f, 26f);
                ShowPiece(_congratsBody, (e - 0.56f) / 0.3f, 22f);
                ShowPiece(_congratsClose, (e - 0.68f) / 0.3f, 20f);

                // Confetti bursts the instant the badge pops.
                if (!confettiSpawned && e >= 0.22f) { SpawnConfetti(); confettiSpawned = true; }
                if (confettiSpawned) TickConfetti(e - 0.22f, dt);

                if (e >= duration) { _congratsAnim?.Pause(); _congratsAnim = null; }
            }).Every(16);
        }

        void HideCongrats()
        {
            if (_congratsOverlay == null) return;
            _congratsAnim?.Pause();
            _congratsAnim = null;

            float startTime = Time.time;
            float startCardScale = _congratsCard?.resolvedStyle.scale.value.x ?? 1f;
            float startBackdrop = _congratsBackdrop?.resolvedStyle.opacity ?? 1f;
            float startCardOpacity = _congratsCard?.resolvedStyle.opacity ?? 1f;

            IVisualElementScheduledItem hideAnim = null;
            hideAnim = _congratsOverlay.schedule.Execute(() =>
            {
                // Exit is quicker than the entrance and just recedes (scale to 0.9).
                float t = Clamp01((Time.time - startTime) / 0.22f);
                float k = EaseOut(t);

                if (_congratsBackdrop != null) _congratsBackdrop.style.opacity = Mathf.Lerp(startBackdrop, 0f, k);
                if (_congratsCard != null)
                {
                    SetScale(_congratsCard, Mathf.Lerp(startCardScale, 0.9f, k));
                    _congratsCard.style.opacity = Mathf.Lerp(startCardOpacity, 0f, k);
                }

                foreach (var piece in _confettiPieces)
                    if (piece != null) piece.style.opacity = Mathf.Lerp(piece.resolvedStyle.opacity, 0f, k);

                if (t >= 1f)
                {
                    hideAnim.Pause();
                    _congratsOverlay.style.display = DisplayStyle.None;
                    ClearConfetti();
                }
            }).Every(16);
        }

        void SpawnConfetti()
        {
            ClearConfetti();

            Color[] palette =
            {
                new(1f, 0.84f, 0f),    new(0.18f, 0.80f, 0.44f),
                new(0.20f, 0.60f, 1f), new(1f, 0.30f, 0.55f),
                new(1f, 0.55f, 0.10f), new(0.70f, 0.25f, 1f),
                new(0f, 0.95f, 0.85f), new(1f, 0.25f, 0.25f),
                new(1f, 1f, 1f),
            };

            var rng = new System.Random();
            const int count = 72;

            for (int i = 0; i < count; i++)
            {
                var piece = new VisualElement();
                piece.AddToClassList("confetti-piece");
                piece.pickingMode = PickingMode.Ignore;
                piece.style.position = Position.Absolute;

                // Mix of streamers, squares and rectangles for a richer scatter.
                int shape = rng.Next(10);
                float w, h, radius;
                if (shape < 2)      { w = 5f + Rand(rng) * 3f;  h = 18f + Rand(rng) * 16f; radius = 3f; }
                else if (shape < 4) { w = 9f + Rand(rng) * 5f;  h = w;                     radius = 3f; }
                else                { w = 8f + Rand(rng) * 12f; h = 5f + Rand(rng) * 8f;   radius = 2f; }

                piece.style.width = w;
                piece.style.height = h;
                piece.style.backgroundColor = palette[rng.Next(palette.Length)];
                piece.style.borderTopLeftRadius = radius;
                piece.style.borderTopRightRadius = radius;
                piece.style.borderBottomLeftRadius = radius;
                piece.style.borderBottomRightRadius = radius;

                // Burst out of the badge area, arc up, then fall under gravity.
                float angle = Rand(rng) * Mathf.PI * 2f;
                float speed = 38f + Rand(rng) * 78f;
                var data = new ConfettiData
                {
                    ox = 50f + (Rand(rng) - 0.5f) * 12f,
                    oy = 40f + (Rand(rng) - 0.5f) * 8f,
                    vx = Mathf.Cos(angle) * speed,
                    vy = Mathf.Sin(angle) * speed - (28f + Rand(rng) * 55f),
                    g = 125f + Rand(rng) * 55f,
                    rot = Rand(rng) * 360f,
                    rotSpeed = -520f + Rand(rng) * 1040f,
                    fade = 2.4f + Rand(rng) * 1.1f,
                };

                piece.style.left = Length.Percent(data.ox);
                piece.style.top = Length.Percent(data.oy);
                piece.style.opacity = 1f;
                piece.style.rotate = new Rotate(data.rot);
                piece.userData = data;

                _congratsOverlay.Add(piece);
                _confettiPieces.Add(piece);
            }
        }

        void TickConfetti(float t, float dt)
        {
            for (int i = _confettiPieces.Count - 1; i >= 0; i--)
            {
                var piece = _confettiPieces[i];
                if (piece == null) { _confettiPieces.RemoveAt(i); continue; }

                var d = (ConfettiData)piece.userData;
                float x = d.ox + d.vx * t;
                float y = d.oy + d.vy * t + 0.5f * d.g * t * t;

                float opacity = 1f - Clamp01((t - d.fade) / 0.9f);
                if (y > 100f) opacity = Mathf.Min(opacity, 1f - Clamp01((y - 100f) / 12f));

                d.rot += d.rotSpeed * dt;
                piece.userData = d;

                piece.style.left = Length.Percent(x);
                piece.style.top = Length.Percent(y);
                piece.style.opacity = opacity;
                piece.style.rotate = new Rotate(d.rot);

                if (y > 118f || opacity <= 0f)
                {
                    piece.RemoveFromHierarchy();
                    _confettiPieces.RemoveAt(i);
                }
            }
        }

        static float Rand(System.Random rng) => (float)rng.NextDouble();

        void ClearConfetti()
        {
            if (_confettiPieces == null) return;
            foreach (var piece in _confettiPieces)
                piece?.RemoveFromHierarchy();
            _confettiPieces.Clear();
        }

        struct ConfettiData
        {
            public float ox;
            public float oy;
            public float vx;
            public float vy;
            public float g;
            public float rot;
            public float rotSpeed;
            public float fade;
        }

        static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        static float EaseOut(float t) => 1f - Mathf.Pow(1f - t, 3f);

        static float Clamp01(float v) => Mathf.Clamp01(v);

        static void SetScale(VisualElement el, float s)
        {
            if (el != null) el.style.scale = new Scale(new Vector3(s, s, 1f));
        }

        // Initial hidden state for a staggered "rise + fade in" element.
        static void HidePiece(VisualElement el, float dy)
        {
            if (el == null) return;
            el.style.opacity = 0f;
            el.style.translate = new Translate(0f, dy);
        }

        static void ShowPiece(VisualElement el, float raw, float dy)
        {
            if (el == null) return;
            float o = EaseOut(Clamp01(raw));
            el.style.opacity = o;
            el.style.translate = new Translate(0f, Mathf.Lerp(dy, 0f, o));
        }

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
