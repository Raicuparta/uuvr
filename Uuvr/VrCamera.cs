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
    public static Camera? HighestDepthVrCamera { get; private set; }
    
    private Camera? _parentCamera;
    private UuvrPoseDriver? _parentCameraPoseDriver;
    private Camera? _childCamera;
    private UuvrPoseDriver? _childCameraPoseDriver;
    private LineRenderer _forwardLine;
    // private int _originalCullingMask = -2;

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

        VrCameraOffset rotationNullifier = Create<VrCameraOffset>(transform);
        _parentCameraPoseDriver = _parentCamera.gameObject.AddComponent<UuvrPoseDriver>();
        
        _childCameraPoseDriver = Create<UuvrPoseDriver>(rotationNullifier.transform);
        _childCameraPoseDriver.name = "VrChildCamera";
        _childCamera = _childCameraPoseDriver.gameObject.AddComponent<Camera>();
        IgnoredCameras.Add(_childCamera);
        _childCamera.CopyFrom(_parentCamera);
        
        // TODO: add option for this.
        // SetUpForwardLine();
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
            // TODO: culling mask stuff is useful, but was breaking in Smushi.
            // if (_originalCullingMask == -2)
            // {
            //     _originalCullingMask = _parentCamera.cullingMask;
            // }
            // Kind of hacky way to reduce the performance penalty of having two cameras enabled.
            // This way I don't have to mess with the culling mask of the original camera.
            // Actually disabling the camera could cause issues with game scripts that read that value.
            // _parentCamera.cullingMask = 0;
            
            
            // TODO: also disable parent camera's rendering, somehow without affecting the game.
            // Disabling the camera itself is not a good idea, but changing the culling mask could work.
            // I've noticed that changing the target display also seems to work without having to mess with the mask.
            // _childCamera.cullingMask = _originalCullingMask;
            _childCamera.cullingMask = _parentCamera.cullingMask;
            _childCamera.clearFlags = _parentCamera.clearFlags;
            _childCamera.depth = _parentCamera.depth;
        }
        else
        {
            // _parentCamera.cullingMask = _originalCullingMask;
            // _originalCullingMask = -2;
            _childCamera.cullingMask = 0;
            _childCamera.clearFlags = CameraClearFlags.Nothing;
            _childCamera.depth = -100;
        }

        Camera cameraForHighestDepth = cameraTrackingMode == ModConfiguration.CameraTrackingMode.Child ? _childCamera : _parentCamera;
        if (HighestDepthVrCamera == null || cameraForHighestDepth.depth > HighestDepthVrCamera.depth)
        {
            HighestDepthVrCamera = cameraForHighestDepth;
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

    private void SetUpForwardLine()
    {
        _forwardLine = new GameObject("VrCameraForwardLine").AddComponent<LineRenderer>();
        _forwardLine.transform.SetParent(transform, false);
        _forwardLine.useWorldSpace = false;
        _forwardLine.SetPositions(new []{ Vector3.forward * 2f, Vector3.forward * 10f });
        _forwardLine.startWidth = 0.1f;
        _forwardLine.endWidth = 0f;
    }
}
