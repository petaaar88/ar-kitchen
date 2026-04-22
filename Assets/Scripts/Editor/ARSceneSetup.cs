using UnityEngine;
using UnityEditor;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering.Universal;

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
            Debug.Log("[ARSceneSetup] Created XR Origin.");
        }

        // Camera Offset
        var offsetChild = xrOrigin.transform.Find("Camera Offset");
        if (offsetChild == null)
        {
            var offsetGO = new GameObject("Camera Offset");
            Undo.RegisterCreatedObjectUndo(offsetGO, "Create Camera Offset");
            offsetGO.transform.SetParent(xrOrigin.transform, false);
            offsetChild = offsetGO.transform;
        }
        xrOrigin.CameraFloorOffsetObject = offsetChild.gameObject;

        // AR Camera
        var camChild = offsetChild.Find("Main Camera");
        if (camChild == null)
        {
            var arCameraGO = new GameObject("Main Camera");
            Undo.RegisterCreatedObjectUndo(arCameraGO, "Create AR Camera");
            arCameraGO.tag = "MainCamera";
            arCameraGO.transform.SetParent(offsetChild, false);
            camChild = arCameraGO.transform;
            Debug.Log("[ARSceneSetup] Created AR Camera.");
        }

        if (camChild.GetComponent<Camera>() == null)
        {
            var cam = camChild.gameObject.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Color;
            cam.backgroundColor = Color.black;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 20f;
        }

        // URP requires this on every camera
        if (camChild.GetComponent<UniversalAdditionalCameraData>() == null)
            camChild.gameObject.AddComponent<UniversalAdditionalCameraData>();

        if (camChild.GetComponent<AudioListener>() == null)
            camChild.gameObject.AddComponent<AudioListener>();
        if (camChild.GetComponent<ARCameraManager>() == null)
            camChild.gameObject.AddComponent<ARCameraManager>();
        if (camChild.GetComponent<ARCameraBackground>() == null)
            camChild.gameObject.AddComponent<ARCameraBackground>();

        // TrackedPoseDriver — must use SerializedObject so expectedControlType
        // and bindings survive scene serialization (setting via InputActionProperty
        // assignment at edit time drops these fields).
        var tpd = camChild.GetComponent<TrackedPoseDriver>();
        if (tpd == null) tpd = camChild.gameObject.AddComponent<TrackedPoseDriver>();
        ConfigureTrackedPoseDriverAction(tpd, "m_PositionInput", "Position", "Vector3",
            "<HandheldARInputDevice>/devicePosition");
        ConfigureTrackedPoseDriverAction(tpd, "m_RotationInput", "Rotation", "Quaternion",
            "<HandheldARInputDevice>/deviceRotation");

        xrOrigin.Camera = camChild.GetComponent<Camera>();
        Debug.Log("[ARSceneSetup] Wired XR Origin camera references.");

        // AR Input Manager — starts XRInputSubsystem so HandheldARInputDevice is available
        if (xrOrigin.GetComponent<ARInputManager>() == null)
        {
            xrOrigin.gameObject.AddComponent<ARInputManager>();
            Debug.Log("[ARSceneSetup] Added ARInputManager.");
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

        // EventSystem — required for all UI button clicks; must use
        // InputSystemUIInputModule with the New Input System
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            Undo.RegisterCreatedObjectUndo(esGO, "Create EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            Debug.Log("[ARSceneSetup] Created EventSystem with InputSystemUIInputModule.");
        }

        EditorUtility.SetDirty(xrOrigin.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[ARSceneSetup] AR scene setup complete. Save the scene (Ctrl+S).");
    }

    static void ConfigureTrackedPoseDriverAction(TrackedPoseDriver tpd, string propName,
        string actionName, string controlType, string bindingPath)
    {
        var so = new SerializedObject(tpd);
        var prop = so.FindProperty(propName);
        prop.FindPropertyRelative("m_UseReference").boolValue = false;

        var action = prop.FindPropertyRelative("m_Action");
        action.FindPropertyRelative("m_Name").stringValue = actionName;
        action.FindPropertyRelative("m_Type").intValue = 0; // Value
        action.FindPropertyRelative("m_ExpectedControlType").stringValue = controlType;

        var bindings = action.FindPropertyRelative("m_SingletonActionBindings");
        bindings.ClearArray();
        bindings.InsertArrayElementAtIndex(0);
        var b = bindings.GetArrayElementAtIndex(0);
        b.FindPropertyRelative("m_Name").stringValue = "";
        b.FindPropertyRelative("m_Id").stringValue = System.Guid.NewGuid().ToString();
        b.FindPropertyRelative("m_Path").stringValue = bindingPath;
        b.FindPropertyRelative("m_Action").stringValue = actionName;
        b.FindPropertyRelative("m_Flags").intValue = 0;
        b.FindPropertyRelative("m_Groups").stringValue = "";
        b.FindPropertyRelative("m_Interactions").stringValue = "";
        b.FindPropertyRelative("m_Processors").stringValue = "";

        so.ApplyModifiedProperties();
    }
}
