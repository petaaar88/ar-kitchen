using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ArKitchen.UI
{
    /// <summary>
    /// Controls the "Surfaces found" panel built in UI Toolkit.
    /// Exposes Show/Hide and a PlacePressed event that higher-level
    /// placement logic can hook into. Pure UI, no AR logic here yet.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class SurfacesFoundPanel : MonoBehaviour
    {
        /// <summary>Raised when the user taps "Place kitchen space".</summary>
        public event Action PlacePressed;

        UIDocument _document;
        VisualElement _root;
        Button _placeButton;
        IVisualElementScheduledItem _fadeAnim;

        void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        void OnEnable()
        {
            _root = _document.rootVisualElement;
            _placeButton = _root.Q<Button>("place-button");
            if (_placeButton != null)
                _placeButton.clicked += OnPlaceClicked;
        }

        void OnDisable()
        {
            if (_placeButton != null)
                _placeButton.clicked -= OnPlaceClicked;
        }

        void OnPlaceClicked()
        {
            // Stub: wire this to AR plane / voxel placement later.
            Debug.Log("[SurfacesFoundPanel] Place kitchen space pressed.");
            PlacePressed?.Invoke();
        }

        void EnsureRoot()
        {
            if (_document == null) _document = GetComponent<UIDocument>();
            if (_root == null && _document != null) _root = _document.rootVisualElement;
        }

        /// <summary>Show or hide the whole panel instantly (resets opacity).</summary>
        public void SetVisible(bool visible)
        {
            EnsureRoot();
            if (_root == null) return;
            _fadeAnim?.Pause();
            _root.style.opacity = 1f;
            _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void Show() => SetVisible(true);

        public void Hide() => SetVisible(false);

        /// <summary>Fade the panel in over <paramref name="duration"/> seconds.</summary>
        public void FadeIn(float duration)
        {
            EnsureRoot();
            if (_root != null) _fadeAnim = UIFade.FadeIn(_root, duration, _fadeAnim);
        }

        /// <summary>Fade the panel out, then remove it from layout.</summary>
        public void FadeOut(float duration)
        {
            EnsureRoot();
            if (_root != null) _fadeAnim = UIFade.FadeOut(_root, duration, _fadeAnim);
        }
    }
}
