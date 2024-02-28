using System;
using System.Reflection;
using UnityEngine;

namespace Uuvr;

public class UuvrPoseDriver: MonoBehaviour
{
    private MethodInfo? _trackingRotationMethod;
    private readonly object[] getLocalRotationArgs = {
        2 // Enum value for XRNode.CenterEye
    };
    
    public static UuvrPoseDriver Create(Transform parent)
    {
        return new GameObject(nameof(UuvrPoseDriver))
        {
            transform =
            {
                parent = parent,
                localPosition = Vector3.zero,
                localRotation = Quaternion.identity
            }
        }.AddComponent<UuvrPoseDriver>();
    }

    private void Awake()
    {
        Type? inputTrackingType = Type.GetType("UnityEngine.XR.InputTracking, UnityEngine.XRModule") ??
                                 Type.GetType("UnityEngine.XR.InputTracking, UnityEngine.VRModule") ??
                                 Type.GetType("UnityEngine.VR.InputTracking, UnityEngine.VRModule") ??
                                 Type.GetType("UnityEngine.VR.InputTracking, UnityEngine");

        _trackingRotationMethod = inputTrackingType?.GetMethod("GetLocalRotation");

        if (_trackingRotationMethod == null)
        {
            Debug.LogError("Failed to find InputTracking.GetLocalRotation. Destroying UUVR Pose Driver.");
            Destroy(this);
            return;
        }

        DisableCameraAutoTracking();
    }

    private void OnEnable()
    {
        Application.onBeforeRender += OnBeforeRender;
    }

    private void OnDisable()
    {
        Application.onBeforeRender -= OnBeforeRender;
    }

    private void OnBeforeRender()
    {
        UpdateTransform();
    }

    private void Update()
    {
        UpdateTransform();
    }

    private void LateUpdate()
    {
        UpdateTransform();
    }

    private void UpdateTransform()
    {
        transform.localRotation = (Quaternion)_trackingRotationMethod.Invoke(null, getLocalRotationArgs);
    }

    private void DisableCameraAutoTracking()
    {
        Camera camera = GetComponent<Camera>();
        if (!camera) return;
        
        Type? xrDeviceType = Type.GetType("UnityEngine.XR.XRDevice, UnityEngine.XRModule") ??
                            Type.GetType("UnityEngine.XR.XRDevice, UnityEngine.VRModule") ??
                            Type.GetType("UnityEngine.VR.VRDevice, UnityEngine.VRModule") ??
                            Type.GetType("UnityEngine.VR.VRDevice, UnityEngine");

        MethodInfo? cameraTrackingDisablingMethod = xrDeviceType?.GetMethod("DisableAutoXRCameraTracking");

        if (cameraTrackingDisablingMethod != null)
        {
            cameraTrackingDisablingMethod.Invoke(null, new object[] { camera, true });
        }
        else
        {
            Debug.LogWarning("Failed to find DisableAutoXRCameraTracking method");
        }
    }
}
