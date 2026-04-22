using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem;

[RequireComponent(typeof(ARRaycastManager))]
public class VoxelMovement : MonoBehaviour
{
    [SerializeField] VoxelStateManager stateManager;

    ARRaycastManager _raycastManager;
    static readonly List<ARRaycastHit> Hits = new();

    void Awake() => _raycastManager = GetComponent<ARRaycastManager>();

    void Update()
    {
        if (stateManager.CurrentMode != VoxelEditMode.Placement) return;
        if (stateManager.Controller == null) return;
        if (Touchscreen.current == null) return;

        var touch = Touchscreen.current.primaryTouch;
        var phase = touch.phase.ReadValue();
        if (phase != UnityEngine.InputSystem.TouchPhase.Moved &&
            phase != UnityEngine.InputSystem.TouchPhase.Began) return;

        if (!_raycastManager.Raycast(touch.position.ReadValue(), Hits, TrackableType.PlaneWithinPolygon))
            return;

        stateManager.Controller.MoveTo(Hits[0].pose.position);
    }
}
