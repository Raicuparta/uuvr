using System;
using System.Collections;
using Mono.CompilerServices.SymbolWriter;
using System.Reflection;
using UnityEngine;

public class VrCamera : MonoBehaviour
{
    private Transform _trackingSource;
    private Transform _transform;
    private Camera _camera;
    private Camera _trackingCamera;
    private Vector3 _previousPosition;
    private Quaternion _previousRotation;
    private bool _isRight = false;

    void Start()
    {
        _camera = GetComponent<Camera>();
        _trackingSource = new GameObject("VrCameraTracking").transform;
        _trackingSource.parent = transform;
        _trackingSource.localPosition = Vector3.zero;
        _trackingSource.rotation = Quaternion.identity;
        _trackingCamera = _trackingSource.gameObject.AddComponent<Camera>();
        _trackingCamera.CopyFrom(_camera);
        _trackingCamera.cullingMask = 0;
        _trackingCamera.clearFlags = CameraClearFlags.Nothing;
        _trackingCamera.depth = -100;
        _transform = transform;

        DisableTracking();
    }

    // When VR is enabled, Unity auto-enables HMD tracking for the cameras, overriding the game's
    // intended camera position. This is annoying, we want tracking relative to the intended position.
    private void DisableTracking()
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
            Component poseDriver = _camera.gameObject.AddComponent(poseDriverType);
            poseDriverType.GetProperty("enabled").SetValue(poseDriver, false, null);
        }
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
        Camera.StereoscopicEye eye = _camera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Left ? Camera.StereoscopicEye.Left : Camera.StereoscopicEye.Right;
        _camera.worldToCameraMatrix = _trackingCamera.GetStereoViewMatrix(eye);
    }
}
