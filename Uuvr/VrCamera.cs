#if CPP
using Il2CppSystem.Collections.Generic;
#else
using System.Collections.Generic;
#endif
using UnityEngine;

namespace Uuvr;

public class VrCamera : UuvrBehaviour
{
    public static readonly HashSet<Camera> VrCameras = new();
    public static readonly HashSet<Camera> IgnoredCameras = new();
    
    private Camera? _parentCamera;
    private UuvrPoseDriver? _parentCameraPoseDriver;
    private Camera? _childCamera;
    private UuvrPoseDriver? _childCameraPoseDriver;

#if CPP
    public VrCamera(System.IntPtr pointer) : base(pointer)
    {
    }
#endif

    protected override void Awake()
    {
        base.Awake();
        _parentCamera = GetComponent<Camera>();
        VrCameras.Add(_parentCamera);
    }

    private void OnDestroy()
    {
        VrCameras.Remove(_parentCamera);
    }

    private void Start()
    {
        // TODO: setting for disabling post processing, antialiasing, etc.

        UuvrRotationNullifier rotationNullifier = Create<UuvrRotationNullifier>(transform);
        _parentCameraPoseDriver = _parentCamera.gameObject.AddComponent<UuvrPoseDriver>();
        
        _childCameraPoseDriver = Create<UuvrPoseDriver>(rotationNullifier.transform);
        _childCameraPoseDriver.name = "VrChildCamera";
        _childCamera = _childCameraPoseDriver.gameObject.AddComponent<Camera>();
        IgnoredCameras.Add(_childCamera);
        _childCamera.CopyFrom(_parentCamera);
    }

    protected override void OnBeforeRender()
    {
        UpdateRelativeCamera();
    }

    private void OnPreCull()
    {
        UpdateRelativeCamera();
    }

    private void OnPreRender()
    {
        UpdateRelativeCamera();
    }

    private void LateUpdate()
    {
        UpdateRelativeCamera();
    }

    private void Update()
    {
        if (ModConfiguration.Instance.OverrideDepth.Value)
        {
            _parentCamera.depth = ModConfiguration.Instance.VrCameraDepth.Value;
        }
        
        ModConfiguration.CameraTrackingMode cameraTrackingMode = ModConfiguration.Instance.CameraTracking.Value;
        _parentCameraPoseDriver.enabled = cameraTrackingMode == ModConfiguration.CameraTrackingMode.Absolute;
        _childCameraPoseDriver.gameObject.SetActive(cameraTrackingMode != ModConfiguration.CameraTrackingMode.Absolute);

        if (cameraTrackingMode == ModConfiguration.CameraTrackingMode.Child)
        {
            _childCamera.cullingMask = _parentCamera.cullingMask;
            _childCamera.clearFlags = _parentCamera.clearFlags;
            _childCamera.depth = _parentCamera.depth;
        }
        else
        {
            _childCamera.cullingMask = 0;
            _childCamera.clearFlags = CameraClearFlags.Nothing;
            _childCamera.depth = -100;
        }
    }

    private void UpdateRelativeCamera()
    {
        if (ModConfiguration.Instance.CameraTracking.Value != ModConfiguration.CameraTrackingMode.Relative) return;
        
        Camera.StereoscopicEye eye = _parentCamera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Left ? Camera.StereoscopicEye.Left : Camera.StereoscopicEye.Right;
       
        // A bit confused by this.
        // worldToCameraMatrix by itself almost works perfectly, but it breaks culling.
        // I expected SetStereoViewMatrix by itself to be enough, but it was even more broken (although culling did work).
        // So I'm just doing both I guess.
        _parentCamera.worldToCameraMatrix = _childCamera.GetStereoViewMatrix(eye);

        if (ModConfiguration.Instance.RelativeCameraSetStereoView.Value)
        {
            // Some times setting worldToCameraMatrix is enough, some times not. I'm not sure why, need to learn more.
            // Some times it's actually better not to call SetStereoViewMatrix, since it messes up the shadows. Like in Aragami.
            _parentCamera.SetStereoViewMatrix(eye, _parentCamera.worldToCameraMatrix);
        }
        
        // TODO: reset camera matrices and everything else on disabling VR
    }
}
