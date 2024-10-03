using UnityEngine;
using Valve.VR;

namespace Uuvr;

public class SteamVRTest
{
    public static EVRCompositorError compositorError;
    static public float sceneResolutionScale = 1.0f;
    static private RenderTexture _sceneTexture;
    private static VRTextureBounds_t[] textureBounds;

    private static Camera camera;
    
    static public RenderTexture GetSceneTexture(bool hdr)
    {
        EVRInitError peError = EVRInitError.None;
        OpenVR.Init(ref peError);
        
        var hmd = OpenVR.System;
        
        float l_left = 0.0f, l_right = 0.0f, l_top = 0.0f, l_bottom = 0.0f;
        hmd.GetProjectionRaw(EVREye.Eye_Left, ref l_left, ref l_right, ref l_top, ref l_bottom);

        float r_left = 0.0f, r_right = 0.0f, r_top = 0.0f, r_bottom = 0.0f;
        hmd.GetProjectionRaw(EVREye.Eye_Right, ref r_left, ref r_right, ref r_top, ref r_bottom);

        var tanHalfFov = new Vector2(
            Mathf.Max(-l_left, l_right, -r_left, r_right),
            Mathf.Max(-l_top, l_bottom, -r_top, r_bottom));
        
        textureBounds = new VRTextureBounds_t[2];

        textureBounds[0].uMin = 0.5f + 0.5f * l_left / tanHalfFov.x;
        textureBounds[0].uMax = 0.5f + 0.5f * l_right / tanHalfFov.x;
        textureBounds[0].vMin = 0.5f - 0.5f * l_bottom / tanHalfFov.y;
        textureBounds[0].vMax = 0.5f - 0.5f * l_top / tanHalfFov.y;

        textureBounds[1].uMin = 0.5f + 0.5f * r_left / tanHalfFov.x;
        textureBounds[1].uMax = 0.5f + 0.5f * r_right / tanHalfFov.x;
        textureBounds[1].vMin = 0.5f - 0.5f * r_bottom / tanHalfFov.y;
        textureBounds[1].vMax = 0.5f - 0.5f * r_top / tanHalfFov.y;
        
        uint pnWidth = 0, pnHeight = 0;
        hmd.GetRecommendedRenderTargetSize(ref pnWidth, ref pnHeight);
        var sceneWidth = (float)pnWidth / Mathf.Max(textureBounds[0].uMax - textureBounds[0].uMin, textureBounds[1].uMax - textureBounds[1].uMin);
        var sceneHeight = (float)pnHeight / Mathf.Max(textureBounds[0].vMax - textureBounds[0].vMin, textureBounds[1].vMax - textureBounds[1].vMin);

        var w = (int)(sceneWidth * sceneResolutionScale);
        var h = (int)(sceneHeight * sceneResolutionScale);
        var aa = QualitySettings.antiAliasing == 0 ? 1 : QualitySettings.antiAliasing;
        var format = hdr ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32;

        if (_sceneTexture != null)
        {
            if (_sceneTexture.width != w || _sceneTexture.height != h || _sceneTexture.antiAliasing != aa || _sceneTexture.format != format)
            {
                Debug.Log(string.Format("Recreating scene texture.. Old: {0}x{1} MSAA={2} [{3}] New: {4}x{5} MSAA={6} [{7}]",
                    _sceneTexture.width, _sceneTexture.height, _sceneTexture.antiAliasing, _sceneTexture.format, w, h, aa, format));
                Object.Destroy(_sceneTexture);
                _sceneTexture = null;
            }
        }

        if (_sceneTexture == null)
        {
            _sceneTexture = new RenderTexture(w, h, 0, format);
            _sceneTexture.antiAliasing = aa;

            // OpenVR assumes floating point render targets are linear unless otherwise specified.
            // var colorSpace = (hdr && QualitySettings.activeColorSpace == ColorSpace.Gamma) ? EColorSpace.Gamma : EColorSpace.Auto;
            // SteamVR.Unity.SetColorSpace(colorSpace);
        }
        // Debug.Log(res);

        return _sceneTexture;
    }

    private static readonly TrackedDevicePose_t[] DevicePoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
    private static readonly TrackedDevicePose_t[] GamePoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];

    
    public static void SetCameraTexture(Camera camera)
    {
        camera.targetTexture = GetSceneTexture(false);
    }

    public static void SetCameraTexture()
    {
        SetCameraTexture(Camera.main);
    }

    public static void Update()
    {
        if (vrCamera == null || vrCamera != Camera.main)
        {
            StartTest();
        }
        
        UpdateTest();
        
        // if (camera == null || camera != Camera.main)
        // {
        //     SetCameraTexture();
        // }
        //
        // var eye = EVREye.Eye_Left;
        // Texture_t t = new Texture_t() { eColorSpace = EColorSpace.Auto, eType = ETextureType.DirectX, handle = _sceneTexture.GetNativeTexturePtr() };
        // VRTextureBounds_t b = eye == EVREye.Eye_Left ? textureBounds[0] :textureBounds[1];
        //
        // OpenVR.Compositor.WaitGetPoses(DevicePoses, GamePoses);
        // compositorError = OpenVR.Compositor.Submit(eye, ref t, ref b, EVRSubmitFlags.Submit_Default);
    }
    
    public static Camera vrCamera;  // VR Camera
    private static Texture2D vrTexture;
    
    public static void StartTest()
    {
        vrCamera = Camera.main;
        
        // Initialize OpenVR
        EVRInitError peError = EVRInitError.None;
        OpenVR.Init(ref peError, EVRApplicationType.VRApplication_Scene);
        if (compositorError != EVRCompositorError.None)
        {
            Debug.LogError("Failed to initialize OpenVR: " + compositorError);
            return;
        }

        // Create a texture to render to (1280x720, 24-bit depth)
        RenderTexture renderTexture = new RenderTexture(1280, 720, 24);
        vrCamera.targetTexture = renderTexture;
    }
    
    private static void UpdateTest()
    {
        // Get the texture from the VR camera
        RenderTexture.active = vrCamera.targetTexture;
        vrTexture = new Texture2D(vrCamera.targetTexture.width, vrCamera.targetTexture.height, TextureFormat.RGB24, false);
        vrTexture.ReadPixels(new Rect(0, 0, vrCamera.targetTexture.width, vrCamera.targetTexture.height), 0, 0);
        vrTexture.Apply();
        
        // Submit the texture to the VR compositor
        SubmitToOpenVR(vrTexture);
    }
    
    private static void SubmitToOpenVR(Texture2D texture)
    {
        
        OpenVR.Compositor.WaitGetPoses(DevicePoses, GamePoses);
        
        // Submit the texture for both eyes (left and right)
        VRTextureBounds_t bounds = new VRTextureBounds_t { uMin = 0, uMax = 1, vMin = 0, vMax = 1 };
        
        // Submit left eye
        Texture_t leftEyeTexture = new Texture_t
        {
            handle = texture.GetNativeTexturePtr(),
            eType = ETextureType.DirectX,  // For OpenGL or DirectX, adjust accordingly
            eColorSpace = EColorSpace.Auto
        };
        compositorError = OpenVR.Compositor.Submit(EVREye.Eye_Left, ref leftEyeTexture, ref bounds, EVRSubmitFlags.Submit_Default);
        if (compositorError != EVRCompositorError.None) Debug.LogError("Failed to submit texture to left eye: " + compositorError);

        // Submit right eye
        Texture_t rightEyeTexture = new Texture_t
        {
            handle = texture.GetNativeTexturePtr(),
            eType = ETextureType.DirectX,
            eColorSpace = EColorSpace.Auto
        };
        compositorError = OpenVR.Compositor.Submit(EVREye.Eye_Right, ref rightEyeTexture, ref bounds, EVRSubmitFlags.Submit_Default);
        if (compositorError != EVRCompositorError.None) Debug.LogError("Failed to submit texture to right eye: " + compositorError);
    }
}