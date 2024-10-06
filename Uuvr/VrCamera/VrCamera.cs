#if CPP
using Il2CppSystem.Collections.Generic;
#else
using System.Collections.Generic;
#endif
using UnityEngine;

namespace Uuvr.VrCamera;

public class VrCamera : UuvrBehaviour
{
    public static readonly HashSet<Camera> VrCameras = new();
    public static readonly HashSet<Camera> IgnoredCameras = new();
    public static VrCamera? HighestDepthVrCamera { get; private set; }

#if MODERN
    private Quaternion _rotationBeforeRender;
#endif
    
    public Camera? ParentCamera { get; private set; }
    public Camera? CameraInUse {
        get {
            return ModConfiguration.Instance.CameraTracking.Value == ModConfiguration.CameraTrackingMode.Child ? _childCamera : ParentCamera;
        }
    }

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
        ParentCamera = GetComponent<Camera>();
        VrCameras.Add(ParentCamera);
    }

#if MODERN && MONO
    protected override void OnBeginFrameRendering()
    {
        base.OnBeginFrameRendering();

        if (ModConfiguration.Instance.CameraTracking.Value != ModConfiguration.CameraTrackingMode.RelativeTransform) return;
        
        _rotationBeforeRender = transform.rotation;
        transform.rotation = _childCamera.transform.rotation;
    }

    protected override void OnEndFrameRendering()
    {
        if (ModConfiguration.Instance.CameraTracking.Value != ModConfiguration.CameraTrackingMode.RelativeTransform) return;

        transform.rotation = _rotationBeforeRender;
    }
#endif

    private void OnDestroy()
    {
        VrCameras.Remove(ParentCamera);
    }

    private void Start()
    {
        // TODO: setting for disabling post processing, antialiasing, etc.

        var rotationNullifier = Create<VrCameraOffset>(transform);
        _parentCameraPoseDriver = ParentCamera.gameObject.AddComponent<UuvrPoseDriver>();
        
        _childCameraPoseDriver = Create<UuvrPoseDriver>(rotationNullifier.transform);
        _childCameraPoseDriver.name = "VrChildCamera";
        _childCamera = _childCameraPoseDriver.gameObject.AddComponent<Camera>();
        IgnoredCameras.Add(_childCamera);
        _childCamera.CopyFrom(ParentCamera);
        
        // TODO: add option for this.
        // SetUpForwardLine();
    }

    protected override void OnBeforeRender()
    {
        UpdateRelativeMatrix();
    }

    private void OnPreCull()
    {
        UpdateRelativeMatrix();
    }

    private void OnPreRender()
    {
        UpdateRelativeMatrix();
    }

    private void LateUpdate()
    {
        UpdateRelativeMatrix();
    }

    private void Update()
    {
        if (ModConfiguration.Instance.OverrideDepth.Value)
        {
            ParentCamera.depth = ModConfiguration.Instance.VrCameraDepth.Value;
        }
        
        var cameraTrackingMode = ModConfiguration.Instance.CameraTracking.Value;
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
            _childCamera.cullingMask = ParentCamera.cullingMask;
            _childCamera.clearFlags = ParentCamera.clearFlags;
            _childCamera.depth = ParentCamera.depth;
        }
        else
        {
            // _parentCamera.cullingMask = _originalCullingMask;
            // _originalCullingMask = -2;
            _childCamera.cullingMask = 0;
            _childCamera.clearFlags = CameraClearFlags.Nothing;
            _childCamera.depth = -100;
        }

        if (HighestDepthVrCamera == null || ParentCamera.depth > HighestDepthVrCamera.CameraInUse.depth)
        {
            HighestDepthVrCamera = this;
        }
    }

    private void UpdateRelativeMatrix()
    {
        if (ModConfiguration.Instance.CameraTracking.Value != ModConfiguration.CameraTrackingMode.RelativeMatrix) return;
        
        var eye = ParentCamera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Left ? Camera.StereoscopicEye.Left : Camera.StereoscopicEye.Right;
       
        // A bit confused by this.
        // worldToCameraMatrix by itself almost works perfectly, but it breaks culling.
        // I expected SetStereoViewMatrix by itself to be enough, but it was even more broken (although culling did work).
        // So I'm just doing both I guess.
        ParentCamera.worldToCameraMatrix = _childCamera.GetStereoViewMatrix(eye);

        if (ModConfiguration.Instance.RelativeCameraSetStereoView.Value)
        {
            // Some times setting worldToCameraMatrix is enough, some times not. I'm not sure why, need to learn more.
            // Some times it's actually better not to call SetStereoViewMatrix, since it messes up the shadows. Like in Aragami.
            ParentCamera.SetStereoViewMatrix(eye, ParentCamera.worldToCameraMatrix);
        }
        
        // TODO: reset camera matrices and everything else on disabling VR
    }

    // TODO: add option for rendering original camera forward line.
    // private void SetUpForwardLine()
    // {
    //     _forwardLine = new GameObject("VrCameraForwardLine").AddComponent<LineRenderer>();
    //     _forwardLine.transform.SetParent(transform, false);
    //     _forwardLine.useWorldSpace = false;
    //     _forwardLine.SetPositions(new []{ Vector3.forward * 2f, Vector3.forward * 10f });
    //     _forwardLine.startWidth = 0.1f;
    //     _forwardLine.endWidth = 0f;
    // }
}
