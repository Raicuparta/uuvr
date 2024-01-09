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

        var poseDriverType = Type.GetType("UnityEngine.SpatialTracking.TrackedPoseDriver, UnityEngine.SpatialTracking");
        var poseDriver = _camera.gameObject.AddComponent(poseDriverType);
        poseDriverType.GetProperty("enabled").SetValue(poseDriver, false, null);

    }

    // private void Update()
    // {
    //     UpdateCamera();
    // }
    //
    // private void LateUpdate()
    // {
    //     UpdateCamera();
    // }

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
        var eye = _camera.stereoActiveEye == Camera.MonoOrStereoscopicEye.Left ? Camera.StereoscopicEye.Left : Camera.StereoscopicEye.Right;
        _camera.worldToCameraMatrix = _trackingCamera.GetStereoViewMatrix(eye);
    }
}
