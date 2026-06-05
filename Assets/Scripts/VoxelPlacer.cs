using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem;
using ArKitchen.UI;

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
    static readonly List<ARRaycastHit>  Hits   = new();
    static readonly List<RaycastResult> UIHits = new();

    public GameObject Voxel { get; private set; }
    public bool IsPlaced { get; private set; }
    public bool PlacementEnabled { get; set; } = false;

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
        if (IsPointerOverUI(screenPos)) return;

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

    static bool IsPointerOverUI(Vector2 screenPos)
    {
        if (KitchenEditPanel.IsPointerOverBlockingUi(screenPos)) return true;
        if (EventSystem.current == null) return false;
        var data = new PointerEventData(EventSystem.current) { position = screenPos };
        UIHits.Clear();
        EventSystem.current.RaycastAll(data, UIHits);
        return UIHits.Count > 0;
    }
}
