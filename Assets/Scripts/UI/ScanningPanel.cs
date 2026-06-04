using UnityEngine;
using UnityEngine.UIElements;

namespace ArKitchen.UI
{
    /// <summary>
    /// Drives the "Scanning your room" panel: a vertically sweeping scan line
    /// inside the reticle and three pulsing dots in the bottom card.
    /// Animation runs off UI Toolkit's scheduler (no per-frame Update).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class ScanningPanel : MonoBehaviour
    {
        [Tooltip("Seconds for the scan line to travel from top to bottom (one half-cycle).")]
        [SerializeField] float sweepSeconds = 1.6f;

        [Tooltip("Seconds for one full dot pulse cycle.")]
        [SerializeField] float dotCycleSeconds = 1.2f;

        const float Inset = 8f;     // matches scan-line left/right inset-ish padding
        const float GlowHeight = 22f;
        const float LineHeight = 3f;

        UIDocument _document;
        VisualElement _root;
        VisualElement _reticle;
        VisualElement _scanLine;
        VisualElement _scanGlow;
        VisualElement[] _dots;
        IVisualElementScheduledItem _anim;
        IVisualElementScheduledItem _fadeAnim;
        float _startTime;

        void Awake() => _document = GetComponent<UIDocument>();

        void OnEnable()
        {
            _root = _document.rootVisualElement;
            _reticle = _root.Q("reticle");
            _scanLine = _root.Q("scan-line");
            _scanGlow = _root.Q("scan-line-glow");
            _dots = new[] { _root.Q("dot0"), _root.Q("dot1"), _root.Q("dot2") };

            _startTime = Time.time;
            _anim = _root.schedule.Execute(Tick).Every(16);
        }

        void OnDisable()
        {
            _anim?.Pause();
            _anim = null;
        }

        void Tick()
        {
            float t = Time.time - _startTime;

            // Scan line: ping-pong vertically between the top and bottom brackets.
            if (_reticle != null)
            {
                float h = _reticle.resolvedStyle.height;
                if (h > 0f)
                {
                    float travel = Mathf.Max(0f, h - Inset * 2f - LineHeight);
                    float pos = Inset + Mathf.PingPong(t * (travel / Mathf.Max(0.01f, sweepSeconds)), travel);

                    _scanLine.style.top = pos;
                    _scanGlow.style.top = pos - (GlowHeight - LineHeight) * 0.5f;
                }
            }

            // Dots: staggered sine pulse on opacity.
            if (_dots != null)
            {
                for (int i = 0; i < _dots.Length; i++)
                {
                    if (_dots[i] == null) continue;
                    float local = Mathf.Repeat(t / Mathf.Max(0.01f, dotCycleSeconds) - i * 0.18f, 1f);
                    float pulse = Mathf.Sin(local * Mathf.PI); // 0 -> 1 -> 0
                    _dots[i].style.opacity = 0.25f + 0.75f * Mathf.Clamp01(pulse);
                }
            }
        }

        /// <summary>Show or hide the whole panel.</summary>
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
