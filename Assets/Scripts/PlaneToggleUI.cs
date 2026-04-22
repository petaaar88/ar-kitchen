using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

public class PlaneToggleUI : MonoBehaviour
{
    [SerializeField] ARPlaneManager planeManager;
    [SerializeField] Button toggleButton;

    bool _visible = true;

    static readonly Color OnColor  = new Color(0.15f, 0.15f, 0.15f, 0.85f);
    static readonly Color OffColor = new Color(0.55f, 0.15f, 0.15f, 0.85f);

    void Awake()
    {
        toggleButton.onClick.AddListener(Toggle);
        Refresh();
    }

    void Toggle()
    {
        _visible = !_visible;
        planeManager.enabled = _visible;
        foreach (var plane in planeManager.trackables)
            plane.gameObject.SetActive(_visible);
        Refresh();
    }

    void Refresh()
    {
        var img = toggleButton.GetComponent<Image>();
        if (img != null) img.color = _visible ? OnColor : OffColor;
    }
}
