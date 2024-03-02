#if CPP
using System;
#endif
using System.Collections.Generic;
using UnityEngine;

namespace Uuvr;

public class VrCamera : UuvrBehaviour
{
    public static readonly HashSet<Camera> VrCameras = new();
    public static readonly HashSet<Camera> IgnoredCameras = new();
    
    private UuvrPoseDriver? _childCameraPoseDriver;
    private Camera? _camera;
    private Camera? _childCamera;
    private UuvrPoseDriver? _parentCameraPoseDriver;

#if CPP
    public VrCamera(System.IntPtr pointer) : base(pointer)
    {
    }
#endif

    protected override void Awake()
    {
        base.Awake();
        _camera = GetComponent<Camera>();
        VrCameras.Add(_camera);
    }

    private void OnDestroy()
    {
        VrCameras.Remove(_camera);
    }

    private void Start()
    {
        // TODO: setting for disabling post processing, antialiasing, etc.

        UuvrRotationNullifier rotationNullifier = Create<UuvrRotationNullifier>(transform);
        _parentCameraPoseDriver = _camera.gameObject.AddComponent<UuvrPoseDriver>();
        
        _childCameraPoseDriver = Create<UuvrPoseDriver>(rotationNullifier.transform);
        _childCamera = _childCameraPoseDriver.gameObject.AddComponent<Camera>();
        IgnoredCameras.Add(_childCamera);
        // _childCamera.CopyFrom(_camera);
        _childCamera.cullingMask = 0;
        _childCamera.clearFlags = CameraClearFlags.Nothing;
        _childCamera.depth = -100;
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
            _camera.depth = ModConfiguration.Instance.VrCameraDepth.Value;
        }
        
        ModConfiguration.CameraTrackingMode cameraTrackingMode = ModConfiguration.Instance.CameraTracking.Value;
        _parentCameraPoseDriver.enabled = cameraTrackingMode == ModConfiguration.CameraTrackingMode.Absolute;
        _childCameraPoseDriver.gameObject.SetActive(cameraTrackingMode == ModConfiguration.CameraTrackingMode.Relative);
    }

    private void UpdateRelativeCamera()
    {
        if (ModConfiguration.Instance.CameraTracking.Value != ModConfiguration.CameraTrackingMode.Relative) return;
        
        Camera.StereoscopicEye eye = _camera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Left ? Camera.StereoscopicEye.Left : Camera.StereoscopicEye.Right;
       
        // A bit confused by this.
        // worldToCameraMatrix by itself almost works perfectly, but it breaks culling.
        // I expected SetStereoViewMatrix by itself to be enough, but it was even more broken (although culling did work).
        // So I'm just doing both I guess.
        _camera.worldToCameraMatrix = _childCamera.GetStereoViewMatrix(eye);

        if (ModConfiguration.Instance.RelativeCameraSetStereoView.Value)
        {
            // Some times setting worldToCameraMatrix is enough, some times not. I'm not sure why, need to learn more.
            // Some times it's actually better not to call SetStereoViewMatrix, since it messes up the shadows. Like in Aragami.
            _camera.SetStereoViewMatrix(eye, _camera.worldToCameraMatrix);
        }
    }
}
