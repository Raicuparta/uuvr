using System;
using System.Reflection;
using UnityEngine;

namespace Uuvr;

public class UuvrPoseDriver: UuvrBehaviour
{
#if CPP
    public UuvrPoseDriver(IntPtr pointer) : base(pointer)
    {
    }
#endif

    private MethodInfo? _trackingRotationMethod;
    private readonly object[] _trackingRotationMethodArgs = {
        2 // Enum value for XRNode.CenterEye
    };

    protected override void Awake()
    {
        base.Awake();
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

    protected override void OnBeforeRender()
    {
        base.OnBeforeRender();
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
        if (_trackingRotationMethod != null)
        {
            transform.localRotation = (Quaternion)_trackingRotationMethod.Invoke(null, _trackingRotationMethodArgs);
        }
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
