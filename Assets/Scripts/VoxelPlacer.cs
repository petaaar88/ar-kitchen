using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem;

/// <summary>
/// Handles one-shot voxel placement on a detected AR plane.
/// Attach to the XR Origin. Assign VoxelPrefab in the Inspector.
/// After placement, IsPlaced becomes true and further taps are ignored
/// unless explicitly re-enabled by VoxelController (Placement edit mode).
/// </summary>
[RequireComponent(typeof(ARRaycastManager))]
public class VoxelPlacer : MonoBehaviour
{
    [SerializeField] GameObject voxelPrefab;

    ARRaycastManager _raycastManager;
    static readonly List<ARRaycastHit> Hits = new();

    public GameObject Voxel { get; private set; }
    public bool IsPlaced { get; private set; }
    public bool PlacementEnabled { get; set; } = true;

    public event System.Action OnPlaced;

    void Awake() => _raycastManager = GetComponent<ARRaycastManager>();

    void Update()
    {
        if (!PlacementEnabled) return;
        if (IsPlaced) return;

        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;
            if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
                TryPlace(touch.position.ReadValue());
        }
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryPlace(Mouse.current.position.ReadValue());
        }
    }

    void TryPlace(Vector2 screenPos)
    {
        if (!_raycastManager.Raycast(screenPos, Hits, TrackableType.PlaneWithinPolygon))
            return;

        var pose = Hits[0].pose;
        Voxel = Instantiate(voxelPrefab, pose.position, Quaternion.identity);
        IsPlaced = true;
        OnPlaced?.Invoke();
    }

    /// <summary>Re-enables placement for Edit > Placement mode.</summary>
    public void AllowReplace()
    {
        IsPlaced = false;
        PlacementEnabled = true;
    }
}
