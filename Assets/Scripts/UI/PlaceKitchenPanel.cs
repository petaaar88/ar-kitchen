using UnityEngine;
using UnityEngine.UIElements;

namespace ArKitchen.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class PlaceKitchenPanel : MonoBehaviour
    {
        UIDocument _document;
        VisualElement _root;
        IVisualElementScheduledItem _fadeAnim;

        void Awake() => _document = GetComponent<UIDocument>();

        void OnEnable()
        {
            _root = _document.rootVisualElement;
        }

        void EnsureRoot()
        {
            if (_document == null) _document = GetComponent<UIDocument>();
            if (_root == null && _document != null) _root = _document.rootVisualElement;
        }

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

        public void FadeIn(float duration)
        {
            EnsureRoot();
            if (_root != null) _fadeAnim = UIFade.FadeIn(_root, duration, _fadeAnim);
        }

        public void FadeOut(float duration)
        {
            EnsureRoot();
            if (_root != null) _fadeAnim = UIFade.FadeOut(_root, duration, _fadeAnim);
        }
    }
}
