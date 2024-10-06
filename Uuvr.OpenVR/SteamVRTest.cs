using System;
using UnityEngine;

namespace Uuvr.OpenVR;

public class SteamVRTest : MonoBehaviour {
    private Camera _activeCamera;

    private VRTextureBounds_t _hmdTextureBounds = new()
    {
        uMin = 0.0f,
        uMax = 1.0f,
        vMin = 1.0f,
        vMax = 0.0f
    };
    
    private Texture_t _hmdEyeTexture;
    private RenderTexture _hmdEyeRenderTexture;
    
    private readonly TrackedDevicePose_t[] _devicePoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
    private readonly TrackedDevicePose_t[] _gamePoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
    
    private void Start()
    {
        SetUpCamera();
        
        // check if HMD is connected on the system
        if (!OpenVR.IsHmdPresent()) {
            throw new InvalidOperationException("HMD not found on this system");
        }

        // check if SteamVR runtime is installed
        if (!OpenVR.IsRuntimeInstalled()) {
            throw new InvalidOperationException("SteamVR runtime not found on this system");
        }

        // initialize HMD
        var hmdInitErrorCode = EVRInitError.None;
        OpenVR.Init(ref hmdInitErrorCode, EVRApplicationType.VRApplication_Scene);
        if (hmdInitErrorCode != EVRInitError.None) {
            throw new Exception("OpenVR error: " + OpenVR.GetStringForHmdError(hmdInitErrorCode));
        }

        SetUpRenderTexture();
    }

    private void SetUpRenderTexture()
    {
        // get HMD render target size
        uint renderTextureWidth = 0;
        uint renderTextureHeight = 0;
        OpenVR.System.GetRecommendedRenderTargetSize(ref renderTextureWidth, ref renderTextureHeight);
        
        var textureType = ETextureType.DirectX;
        switch (SystemInfo.graphicsDeviceType) {
            case UnityEngine.Rendering.GraphicsDeviceType.OpenGLCore:
            case UnityEngine.Rendering.GraphicsDeviceType.OpenGLES2:
            case UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3:
                textureType = ETextureType.OpenGL;
                throw new InvalidOperationException(SystemInfo.graphicsDeviceType + " does not support VR. You must use -force-d3d11");
            case UnityEngine.Rendering.GraphicsDeviceType.Direct3D11:
                textureType = ETextureType.DirectX;
                break;
            case UnityEngine.Rendering.GraphicsDeviceType.Direct3D12:
                textureType = ETextureType.DirectX;
                break;
            default:
                throw new InvalidOperationException(SystemInfo.graphicsDeviceType + " not supported");
        }
        
        _hmdEyeRenderTexture = new RenderTexture((int)renderTextureWidth, (int)renderTextureHeight, 24, RenderTextureFormat.ARGB32);
        _hmdEyeRenderTexture.Create();
        _hmdEyeTexture.handle = _hmdEyeRenderTexture.GetNativeTexturePtr();
        _hmdEyeTexture.eColorSpace = EColorSpace.Auto;
        _hmdEyeTexture.eType = textureType;
    }

    private void SetUpCamera()
    {
        if (_activeCamera != null)
        {
            Destroy(_activeCamera.gameObject);
        }
        
        Debug.Log("Setting up camera...");
        _activeCamera = new GameObject("VrCamera").AddComponent<Camera>();
        _activeCamera.enabled = false;
        var parentCamera = Camera.main;
        if (parentCamera == null)
        {
            parentCamera = Camera.current;
        }
        else if (parentCamera == null)
        {
            parentCamera = FindObjectOfType<Camera>();
        }

        if (parentCamera != null)
        {
            Debug.Log($"Using parent camera: {parentCamera.name}");
            _activeCamera.CopyFrom(parentCamera);
            _activeCamera.enabled = false;
        }

        _activeCamera.transform.parent = parentCamera == null ? null : parentCamera.transform;
        _activeCamera.transform.localPosition = Vector3.zero;
        _activeCamera.transform.localRotation = Quaternion.identity;
    }

    private void OnDestroy()
    {
        Debug.Log("VR shutting down...");
        Destroy(_activeCamera.gameObject);
        // OpenVR.Shutdown();
    }

    private void Update()
    {
        if (OpenVR.Compositor == null) return;
        
        var vrCompositorError = EVRCompositorError.None;
        vrCompositorError = OpenVR.Compositor.WaitGetPoses(_devicePoses, _gamePoses);

        if (vrCompositorError != EVRCompositorError.None) {
            throw new Exception("WaitGetPoses failed: (" + (int)vrCompositorError + ") " + vrCompositorError.ToString());
        }
    }

    private static bool IsFocused()
    {
        return OpenVR.System != null && Environment.ProcessId == OpenVR.Compositor.GetCurrentSceneFocusProcess();
    }

    private void LateUpdate()
    {
        if (!IsFocused() || !OpenVR.Compositor.CanRenderScene()) return;

        if (_activeCamera == null)
        {
            Debug.Log("Active camera was destroyed, recreating");
            SetUpCamera();
        }

        if (_activeCamera == null)
        {
            Debug.Log("Active camera couldn't be created, skipping");
            return;
        }
        
        try {
            Render(EVREye.Eye_Left);
            Render(EVREye.Eye_Right);
            OpenVR.Compositor.PostPresentHandoff();
        } catch (Exception e) {
            Debug.LogError($"steamvrtest error: {e}");
        }
    }

    private static Matrix4x4 Matrix4x4_OpenVr2UnityFormat(ref HmdMatrix44_t mat44Openvr) {
        var mat44Unity = Matrix4x4.identity;
        mat44Unity.m00 = mat44Openvr.m0;
        mat44Unity.m01 = mat44Openvr.m1;
        mat44Unity.m02 = mat44Openvr.m2;
        mat44Unity.m03 = mat44Openvr.m3;
        mat44Unity.m10 = mat44Openvr.m4;
        mat44Unity.m11 = mat44Openvr.m5;
        mat44Unity.m12 = mat44Openvr.m6;
        mat44Unity.m13 = mat44Openvr.m7;
        mat44Unity.m20 = mat44Openvr.m8;
        mat44Unity.m21 = mat44Openvr.m9;
        mat44Unity.m22 = mat44Openvr.m10;
        mat44Unity.m23 = mat44Openvr.m11;
        mat44Unity.m30 = mat44Openvr.m12;
        mat44Unity.m31 = mat44Openvr.m13;
        mat44Unity.m32 = mat44Openvr.m14;
        mat44Unity.m33 = mat44Openvr.m15;
        return mat44Unity;
    }
    
    private void Render(EVREye eye)
    {
        var prevCameraPosition = _activeCamera.transform.localPosition;

        // convert SteamVR poses to Unity coordinates
        var hmdMatrix = _devicePoses[OpenVR.k_unTrackedDeviceIndex_Hmd].mDeviceToAbsoluteTracking;
        var eyeMatrix = OpenVR.System.GetEyeToHeadTransform(eye);

        var hmdRotation = hmdMatrix.GetRotation();
        var eyeRotation = eyeMatrix.GetRotation();
        var eyePosition = eyeMatrix.GetPosition();
        
        _activeCamera.transform.localRotation = hmdRotation * eyeRotation;
        _activeCamera.transform.localPosition = prevCameraPosition + hmdRotation * eyePosition;
            
        var projectionMatrix = OpenVR.System.GetProjectionMatrix(eye, _activeCamera.nearClipPlane, _activeCamera.farClipPlane);
        _activeCamera.projectionMatrix = Matrix4x4_OpenVr2UnityFormat(ref projectionMatrix);
        
        // set texture to render to, then render
        if (_hmdEyeRenderTexture == null)
        {
            Debug.Log("missing render texture, recreating");
            SetUpRenderTexture();
        }

        _activeCamera.targetTexture = _hmdEyeRenderTexture;
        _activeCamera.Render();

        _activeCamera.transform.localPosition = prevCameraPosition;

        _hmdEyeTexture.handle = _hmdEyeRenderTexture.GetNativeTexturePtr();

        // Submit frames to HMD
        var vrCompositorError = OpenVR.Compositor.Submit(eye, ref _hmdEyeTexture, ref _hmdTextureBounds, EVRSubmitFlags.Submit_Default);
        if (vrCompositorError != EVRCompositorError.None) {
            throw new Exception("Submit (" + eye + ") failed: (" + (int)vrCompositorError + ") " + vrCompositorError);
        }
        
        _hmdEyeRenderTexture.Release();
    }
}