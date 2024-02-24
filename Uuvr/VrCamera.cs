using System;
using UnityEngine;
using Uuvr;

public class VrCamera : MonoBehaviour
{
    private Transform _trackingSource;
    private Camera _camera;
    private Camera _trackingCamera;
    private Vector3 _previousPosition;
    private Quaternion _previousRotation;
    private bool _isRight = false;
    
    // This pose driver is used only for disabling the tracking that some Unity versions add to cameras automatically.
    private Component _directTrackingDisablerPoseDriver;
    private bool _isDirectTrackingDisabled = false;

#if CPP
    public VrCamera(IntPtr pointer) : base(pointer)
    {
    }
#endif
    
    void Start()
    {
        _camera = GetComponent<Camera>();
        _trackingSource = new GameObject("VrCameraTracking").transform;
        _trackingSource.parent = transform;
        _trackingSource.localPosition = Vector3.zero;
        _trackingSource.rotation = Quaternion.identity;
        _trackingCamera = _trackingSource.gameObject.AddComponent<Camera>();
        // _trackingCamera.CopyFrom(_camera);
        _trackingCamera.cullingMask = 0;
        _trackingCamera.clearFlags = CameraClearFlags.Nothing;
        _trackingCamera.depth = -100;
    }

    // When VR is enabled, Unity auto-enables HMD tracking for the cameras, overriding the game's
    // intended camera position. This is annoying, we want tracking relative to the intended position.
    private void DisableDirectTracking()
    {
        Type poseDriverType = Type.GetType("UnityEngine.SpatialTracking.TrackedPoseDriver, UnityEngine.SpatialTracking");
        if (poseDriverType == null)
        {
            // Calling this method disables tracking for some reason.
            // But when I tested it in Aragami, it also messed up the shadows in the right eye.
            // Still, this method is useful for games where TrackedPoseDriver isn't available.
            _camera.SetStereoViewMatrix(Camera.StereoscopicEye.Left, Matrix4x4.zero);
        }
        else
        {
            // Adding a TrackedPoseDriver component, and then disabling it, also disables auto-tracking.
            // This works in Aragami since the TrackedPoseDriver component exists,
            // and it doesn't have the same shadows bug that the SetStereoViewMatrix method caused.
            _directTrackingDisablerPoseDriver = _camera.gameObject.AddComponent(
#if CPP
                UnhollowerRuntimeLib.Il2CppType.From(poseDriverType)
#else
                poseDriverType
#endif
            );
            poseDriverType.GetProperty("enabled").SetValue(_directTrackingDisablerPoseDriver, false, null);

            _isDirectTrackingDisabled = true;
        }
    }

    private void EnableDirectTracking()
    {
        if (_directTrackingDisablerPoseDriver == null)
        {
            // TODO: if tracking was disabled via SetStereoViewMatrix,
            // I don't think it's possible to enabled it again with the same camera.
            // Might require a restart.
        }
        else
        {
            // Removing the disabled TrackedPoseDriver should let Unity go back to the auto-tracking it usually does.
            Destroy(_directTrackingDisablerPoseDriver);
        }

        _isDirectTrackingDisabled = false;
    }

    private void OnPreCull()
    {
        UpdateCamera();
    }

    private void OnPreRender()
    {
        UpdateCamera();
    }

    private void UpdateCamera()
    {
        bool isRelativeTracking = ModConfiguration.Instance.cameraTracking.Value == ModConfiguration.CameraTracking.Relative;

        if (isRelativeTracking && !_isDirectTrackingDisabled) DisableDirectTracking();
        else if (!isRelativeTracking && _isDirectTrackingDisabled) EnableDirectTracking();
        
        if (!isRelativeTracking) return;
        
        Camera.StereoscopicEye eye = _camera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Left ? Camera.StereoscopicEye.Left : Camera.StereoscopicEye.Right;
       
        // A bit confused by this.
        // worldToCameraMatrix by itself almost works perfectly, but it breaks culling.
        // I expected SetStereoViewMatrix by itself to be enough, but it was even more broken (although culling did work).
        // So I'm just doing both I guess.
        _camera.worldToCameraMatrix = _trackingCamera.GetStereoViewMatrix(eye);

        if (ModConfiguration.Instance.relativeCameraSetStereoView.Value)
        {
            // Some times setting worldToCameraMatrix is enough, some times not. I'm not sure why, need to learn more.
            // Some times it's actually better not to call SetStereoViewMatrix, since it messes up the shadows. Like in Aragami.
            _camera.SetStereoViewMatrix(eye, _camera.worldToCameraMatrix);
        }
    }
}
