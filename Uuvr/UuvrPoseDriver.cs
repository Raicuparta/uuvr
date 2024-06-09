using System;
using System.Reflection;
using UnityEngine;
using Uuvr.UnityTypesHelper;

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
        var inputTrackingType = Type.GetType("UnityEngine.XR.InputTracking, UnityEngine.XRModule") ??
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
        var camera = GetComponent<Camera>();
        if (!camera) return;
        
        var cameraTrackingDisablingMethod = UuvrXrDevice.XrDeviceType?.GetMethod("DisableAutoXRCameraTracking");

        if (cameraTrackingDisablingMethod != null)
        {
            cameraTrackingDisablingMethod.Invoke(null, new object[] { camera, true });
        }
        else
        {
            // TODO: use alternative method for disabling tracking.
            Debug.LogWarning("Failed to find DisableAutoXRCameraTracking method. Using SetStereoViewMatrix, which also prevents Unity from auto-tracking cameras, but can cause other issues.");
            // TODO: this crashes some games? Example Monster Girl Island. Although that game already comes with VR stuff, dunno if could affect.
            // camera.SetStereoViewMatrix(Camera.StereoscopicEye.Left, camera.worldToCameraMatrix);
            // camera.SetStereoViewMatrix(Camera.StereoscopicEye.Right, camera.worldToCameraMatrix);
        }
    }
}
