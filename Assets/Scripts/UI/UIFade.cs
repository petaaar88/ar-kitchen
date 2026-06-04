using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ArKitchen.UI
{
    /// <summary>
    /// Opacity cross-fade helpers for a panel's root VisualElement, driven by
    /// UI Toolkit's scheduler. FadeIn flips display on before fading up; FadeOut
    /// fades down then flips display off so the element leaves layout.
    /// </summary>
    static class UIFade
    {
        public static IVisualElementScheduledItem FadeIn(VisualElement root, float duration, IVisualElementScheduledItem current)
        {
            current?.Pause();
            root.style.display = DisplayStyle.Flex;
            return Run(root, 0f, 1f, duration, null);
        }

        public static IVisualElementScheduledItem FadeOut(VisualElement root, float duration, IVisualElementScheduledItem current)
        {
            current?.Pause();
            return Run(root, root.resolvedStyle.opacity, 0f, duration, () => root.style.display = DisplayStyle.None);
        }

        static IVisualElementScheduledItem Run(VisualElement root, float from, float to, float duration, Action onComplete)
        {
            if (duration <= 0f)
            {
                root.style.opacity = to;
                onComplete?.Invoke();
                return null;
            }

            float start = Time.time;
            root.style.opacity = from;

            IVisualElementScheduledItem item = null;
            item = root.schedule.Execute(() =>
            {
                float t = Mathf.Clamp01((Time.time - start) / duration);
                root.style.opacity = Mathf.Lerp(from, to, t);
                if (t >= 1f)
                {
                    item.Pause();
                    onComplete?.Invoke();
                }
            }).Every(16);
            return item;
        }
    }
}
