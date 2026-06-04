using UnityEngine;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Feeds each AR plane's plane-space bounding box to the "AR Kitchen/Feathered
/// Plane" shader so the dot pattern fades out near the plane edges instead of
/// stopping at a hard border. Attach to the ARPlane prefab alongside the
/// MeshRenderer that uses the feathered material.
/// </summary>
[RequireComponent(typeof(ARPlane))]
[RequireComponent(typeof(MeshRenderer))]
public class ARPlaneFeather : MonoBehaviour
{
    static readonly int PlaneMinId = Shader.PropertyToID("_PlaneMin");
    static readonly int PlaneMaxId = Shader.PropertyToID("_PlaneMax");

    ARPlane _plane;
    MeshRenderer _renderer;
    MaterialPropertyBlock _mpb;

    void Awake()
    {
        _plane = GetComponent<ARPlane>();
        _renderer = GetComponent<MeshRenderer>();
        _mpb = new MaterialPropertyBlock();
    }

    void OnEnable()
    {
        _plane.boundaryChanged += OnBoundaryChanged;
        UpdateBounds();
    }

    void OnDisable() => _plane.boundaryChanged -= OnBoundaryChanged;

    void OnBoundaryChanged(ARPlaneBoundaryChangedEventArgs _) => UpdateBounds();

    void UpdateBounds()
    {
        var boundary = _plane.boundary; // plane-space (x, z), matches mesh UV0
        if (boundary.Length == 0) return;

        Vector2 min = boundary[0];
        Vector2 max = boundary[0];
        for (int i = 1; i < boundary.Length; i++)
        {
            min = Vector2.Min(min, boundary[i]);
            max = Vector2.Max(max, boundary[i]);
        }

        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetVector(PlaneMinId, new Vector4(min.x, min.y, 0f, 0f));
        _mpb.SetVector(PlaneMaxId, new Vector4(max.x, max.y, 0f, 0f));
        _renderer.SetPropertyBlock(_mpb);
    }
}
