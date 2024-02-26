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
    private MonoBehaviour _directTrackingPoseDriver;
    private bool _isDirectTrackingDisabled = false;

#if CPP
    public VrCamera(IntPtr pointer) : base(pointer)
    {
    }
#endif
    
    void Start()
    {
        _camera = GetComponent<Camera>();
        // TODO: setting for overriding camera depth.
        // TODO: setting for disabling post processing, antialiasing, etc.
        
        _trackingSource = new GameObject("VrCameraTracking").transform;
        _trackingSource.parent = transform;
        _trackingSource.localPosition = Vector3.zero;
        _trackingSource.rotation = Quaternion.identity;
        _trackingCamera = _trackingSource.gameObject.AddComponent<Camera>();
        // _trackingCamera.CopyFrom(_camera);
        _trackingCamera.cullingMask = 0;
        _trackingCamera.clearFlags = CameraClearFlags.Nothing;
        _trackingCamera.depth = -100;
        
        UuvrPoseDriver.Create(_trackingCamera);
    }

    private void SetUpDirectTracking()
    {
        if (_directTrackingPoseDriver != null) return;
        _directTrackingPoseDriver = UuvrPoseDriver.Create(_camera);
    }

    // When VR is enabled, Unity auto-enables HMD tracking for the cameras, overriding the game's
    // intended camera position. This is annoying, we want tracking relative to the intended position.
    private void DisableDirectTracking()
    {
        Debug.Log("Disabling Direct Tracking");

        SetUpDirectTracking();
        if (_directTrackingPoseDriver == null)
        {
            // Calling this method disables tracking for some reason.
            // But when I tested it in Aragami, it also messed up the shadows in the right eye.
            // Still, this method is useful for games where TrackedPoseDriver isn't available.
            _camera.SetStereoViewMatrix(Camera.StereoscopicEye.Left, Matrix4x4.zero);
        }
        else
        {
            _directTrackingPoseDriver.enabled = false;
        }

        _isDirectTrackingDisabled = true;
    }

    private void EnableDirectTracking()
    {
        Debug.Log("Enabling Direct Tracking");

        SetUpDirectTracking();

        if (_directTrackingPoseDriver == null)
        {
            Debug.LogWarning("Can't enable direct tracking, since pose driver is not defined");
            // TODO: if tracking was disabled via SetStereoViewMatrix,
            // I don't think it's possible to enabled it again with the same camera.
            // Might require a restart.
        }
        else
        {
            // Removing the disabled TrackedPoseDriver should let Unity go back to the auto-tracking it usually does.
            _directTrackingPoseDriver.enabled = true;
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

    private void LateUpdate()
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
