using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem;
using ArKitchen.UI;

[RequireComponent(typeof(ARRaycastManager))]
public class VoxelMovement : MonoBehaviour
{
    [SerializeField] VoxelStateManager stateManager;

    ARRaycastManager _raycastManager;
    static readonly List<ARRaycastHit>  Hits    = new();
    static readonly List<RaycastResult> UIHits  = new();

    void Awake() => _raycastManager = GetComponent<ARRaycastManager>();

    void Update()
    {
        if (stateManager.CurrentMode != VoxelEditMode.Placement) return;
        if (stateManager.Controller == null) return;

        Vector2 screenPos;
        if (Touchscreen.current != null)
        {
            var phase = Touchscreen.current.primaryTouch.phase.ReadValue();
            if (phase != UnityEngine.InputSystem.TouchPhase.Moved &&
                phase != UnityEngine.InputSystem.TouchPhase.Began) return;
            screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
        }
        else if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            screenPos = Mouse.current.position.ReadValue();
        }
        else return;

        if (IsPointerOverUI(screenPos)) return;

        if (!_raycastManager.Raycast(screenPos, Hits, TrackableType.PlaneWithinPolygon))
            return;

        stateManager.Controller.MoveTo(Hits[0].pose.position);
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
