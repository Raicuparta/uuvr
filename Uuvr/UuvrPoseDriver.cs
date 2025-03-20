using System;
using System.Reflection;
using UnityEngine;
using Uuvr.UnityTypesHelper;

using static Uuvr.ModConfiguration;

namespace Uuvr;

public class UuvrPoseDriver: UuvrBehaviour
{
#if CPP
    public UuvrPoseDriver(IntPtr pointer) : base(pointer)
    {
    }
#endif

    private MethodInfo? _trackingRotationMethod;

    private MethodInfo? _trackingPositionMethod;

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

        _trackingPositionMethod = inputTrackingType?.GetMethod("GetLocalPosition");
        if (_trackingPositionMethod == null)
        {
            Debug.LogWarning("Failed to find InputTracking.GetLocalPosition. Position tracking will be disabled.");
        }

        DisableCameraAutoTracking();
        RecenterView();
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

    /// <summary>
    /// This vector is used to recenter the view position.  (its static so that all VRCamera's share the same offset.
    /// </summary>
    private static Vector3? _positionOffset;
    

    public static void RecenterView()
    {
        _positionOffset = null;
    }

    
    private void UpdateTransform()
    {
        if (_trackingRotationMethod != null)
        {
            transform.localRotation = (Quaternion)_trackingRotationMethod.Invoke(null, _trackingRotationMethodArgs);
        }

        if (_trackingPositionMethod != null && ModConfiguration.Instance.HeadsetDOF.Value == DegreesOfFreedom.Six)
        {            
            var pos = (Vector3)_trackingPositionMethod.Invoke(null, _trackingRotationMethodArgs);
            if (_positionOffset == null)
            {
                _positionOffset = -pos;  // First time we get the position, we store the reverse as the offset.
            }
            // add the offset to the position.
            pos += _positionOffset.Value;
            
            transform.localPosition = pos;
        }
        else
        {
            transform.localPosition = Vector3.zero;
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
