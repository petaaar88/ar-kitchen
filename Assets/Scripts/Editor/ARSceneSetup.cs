using UnityEngine;
using UnityEditor;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;

/// <summary>
/// One-time menu item to convert MainScene's bare camera into a full AR scene:
/// ARSession, XROrigin (with AR Camera), ARPlaneManager, ARRaycastManager.
/// Run via Tools > AR Kitchen > Setup AR Scene.
/// </summary>
public static class ARSceneSetup
{
    [MenuItem("Tools/AR Kitchen/Setup AR Scene")]
    public static void SetupARScene()
    {
        // Remove existing Main Camera
        var oldCam = GameObject.FindWithTag("MainCamera");
        if (oldCam != null)
        {
            Undo.DestroyObjectImmediate(oldCam);
            Debug.Log("[ARSceneSetup] Removed old Main Camera.");
        }

        // AR Session
        if (Object.FindFirstObjectByType<ARSession>() == null)
        {
            var arSessionGO = new GameObject("AR Session");
            Undo.RegisterCreatedObjectUndo(arSessionGO, "Create AR Session");
            arSessionGO.AddComponent<ARSession>();
            Debug.Log("[ARSceneSetup] Created AR Session.");
        }

        // XR Origin
        XROrigin xrOrigin = Object.FindFirstObjectByType<XROrigin>();
        if (xrOrigin == null)
        {
            var xrOriginGO = new GameObject("XR Origin (AR)");
            Undo.RegisterCreatedObjectUndo(xrOriginGO, "Create XR Origin");
            xrOrigin = xrOriginGO.AddComponent<XROrigin>();

            // Camera Offset
            var offsetGO = new GameObject("Camera Offset");
            offsetGO.transform.SetParent(xrOriginGO.transform, false);

            // AR Camera
            var arCameraGO = new GameObject("Main Camera");
            arCameraGO.tag = "MainCamera";
            arCameraGO.transform.SetParent(offsetGO.transform, false);

            var cam = arCameraGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Color;
            cam.backgroundColor = Color.black;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 20f;

            arCameraGO.AddComponent<AudioListener>();
            arCameraGO.AddComponent<ARCameraManager>();
            arCameraGO.AddComponent<ARCameraBackground>();

            xrOrigin.Camera = cam;
            xrOrigin.CameraFloorOffsetObject = offsetGO;

            Debug.Log("[ARSceneSetup] Created XR Origin with AR Camera.");
        }

        // AR Plane Manager
        if (xrOrigin.GetComponent<ARPlaneManager>() == null)
        {
            xrOrigin.gameObject.AddComponent<ARPlaneManager>();
            Debug.Log("[ARSceneSetup] Added ARPlaneManager.");
        }

        // AR Raycast Manager
        if (xrOrigin.GetComponent<ARRaycastManager>() == null)
        {
            xrOrigin.gameObject.AddComponent<ARRaycastManager>();
            Debug.Log("[ARSceneSetup] Added ARRaycastManager.");
        }

        EditorUtility.SetDirty(xrOrigin.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[ARSceneSetup] AR scene setup complete. Save the scene (Ctrl+S).");
    }
}
