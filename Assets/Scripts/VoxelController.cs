using UnityEngine;

public class VoxelController : MonoBehaviour
{
    const float EdgeThickness = 0.02f;

    [SerializeField] Transform fill;

    float _width = 1f, _depth = 1f, _height = 1f;

    public float Width  => _width;
    public float Depth  => _depth;
    public float Height => _height;

    public event System.Action OnResized;

    void Start() => UpdateGeometry();

    public void Resize(float width, float depth, float height)
    {
        _width  = Mathf.Max(0.1f, width);
        _depth  = Mathf.Max(0.1f, depth);
        _height = Mathf.Max(0.1f, height);
        UpdateGeometry();
        OnResized?.Invoke();
    }

    public void MoveTo(Vector3 worldPos) => transform.position = worldPos;

    public void Rotate(float degrees) =>
        transform.Rotate(0f, degrees, 0f, Space.World);

    /// <summary>
    /// Shows or hides the voxel's own marker visuals (fill + wireframe edges)
    /// without touching placed kitchen elements, which are separate children.
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (fill != null) fill.gameObject.SetActive(visible);
        foreach (Transform child in transform)
            if (child.name.StartsWith("Edge_"))
                child.gameObject.SetActive(visible);
    }

    void UpdateGeometry()
    {
        float w = _width, h = _height, d = _depth;
        float hw = w * 0.5f, hh = h * 0.5f, hd = d * 0.5f;

        fill.localScale    = new Vector3(w, h, d);
        fill.localPosition = new Vector3(0f, hh, 0f);

        float t = EdgeThickness;
        SetEdge("Edge_BottomFront",    new Vector3(0f,   0f,  -hd), new Vector3(w, t, t));
        SetEdge("Edge_BottomBack",     new Vector3(0f,   0f,  +hd), new Vector3(w, t, t));
        SetEdge("Edge_BottomLeft",     new Vector3(-hw,  0f,   0f), new Vector3(t, t, d));
        SetEdge("Edge_BottomRight",    new Vector3(+hw,  0f,   0f), new Vector3(t, t, d));
        SetEdge("Edge_TopFront",       new Vector3(0f,   h,   -hd), new Vector3(w, t, t));
        SetEdge("Edge_TopBack",        new Vector3(0f,   h,   +hd), new Vector3(w, t, t));
        SetEdge("Edge_TopLeft",        new Vector3(-hw,  h,    0f), new Vector3(t, t, d));
        SetEdge("Edge_TopRight",       new Vector3(+hw,  h,    0f), new Vector3(t, t, d));
        SetEdge("Edge_VertFrontLeft",  new Vector3(-hw,  hh,  -hd), new Vector3(t, h, t));
        SetEdge("Edge_VertFrontRight", new Vector3(+hw,  hh,  -hd), new Vector3(t, h, t));
        SetEdge("Edge_VertBackLeft",   new Vector3(-hw,  hh,  +hd), new Vector3(t, h, t));
        SetEdge("Edge_VertBackRight",  new Vector3(+hw,  hh,  +hd), new Vector3(t, h, t));
    }

    void SetEdge(string childName, Vector3 pos, Vector3 scale)
    {
        var child = transform.Find(childName);
        if (child == null) return;
        child.localPosition = pos;
        child.localScale    = scale;
    }
}
