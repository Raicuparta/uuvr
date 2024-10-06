using System;
using UnityEngine;

namespace Uuvr.OpenVR;

public class SteamVRTest : MonoBehaviour {
    #region Types

    private enum HmdState {
        Uninitialized,
        Initializing,
        Initialized,
        InitFailed,
    }
    #endregion


    #region Properties

    private static Camera ActiveCamera => Camera.main ?? Camera.current;

    /// <summary>
    /// Returns true if VR is currently running, i.e. tracking devices
    /// and rendering images to the headset.
    /// </summary>
    private bool _hmdIsRunning;

    #endregion


    #region Private Members

    // keep track of when the HMD is rendering images
    private HmdState _hmdState = HmdState.Uninitialized;
    private bool _hmdIsRunningPrev = false;
    private DateTime _hmdInitLastAttempt;

    // defines the bounds to texture bounds for rendering
    private VRTextureBounds_t _hmdTextureBounds;

    // these arrays each hold one object for the corresponding eye, where
    // index 0 = Left_Eye, index 1 = Right_Eye
    private readonly Texture_t[] _hmdEyeTextures = new Texture_t[2];
    private readonly RenderTexture[] _hmdEyeRenderTexture = new RenderTexture[2];

    // store the tracked device poses
    private readonly TrackedDevicePose_t[] _devicePoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
    private readonly TrackedDevicePose_t[] _gamePoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];

    #endregion

    /// <summary>
    /// Initialize the application GUI, singleton classes, and initialize OpenVR.
    /// </summary>
    private void Start() {

        InitializeHmd();

        // don't destroy this object when switching scenes
        DontDestroyOnLoad(this);
    }

    /// <summary>
    /// Overrides the OnDestroy method, called when plugin is destroyed.
    /// </summary>
    private void OnDestroy() {
        Debug.Log("VR shutting down...");
        CloseHmd();
    }

    private void Update()
    {
        var vrCompositorError = EVRCompositorError.None;
        vrCompositorError = OpenVR.Compositor.WaitGetPoses(_devicePoses, _gamePoses);

        if (vrCompositorError != EVRCompositorError.None) {
            throw new Exception("WaitGetPoses failed: (" + (int)vrCompositorError + ") " + vrCompositorError.ToString());
        }
    }

    /// <summary>
    /// On LateUpdate, dispatch OpenVR events, run the main HMD loop code.
    /// </summary>
    private void LateUpdate() {
        // dispatch any OpenVR events
        if (_hmdState == HmdState.Initialized) {
            DispatchOpenVREvents();
        }

        // process the state of OpenVR
        ProcessHmdState();

        // check if we are running the HMD
        _hmdIsRunning = _hmdState == HmdState.Initialized;

        // perform regular updates if HMD is initialized
        if (_hmdIsRunning) {
            // we've just started VR
            if (!_hmdIsRunningPrev) {
                Debug.Log("HMD is now on");
            }

            try {

                // don't highlight parts with the mouse
                // Mouse.HoveredPart = null;

                // render each eye
                RenderHmdCameras(EVREye.Eye_Left);
                RenderHmdCameras(EVREye.Eye_Right);

                // [insert dark magic here]
                // OpenVR.Compositor.PostPresentHandoff();

                // render to the game screen
                // if (RenderHmdToScreen) {
                //     Graphics.Blit(_hmdEyeRenderTexture[0], null as RenderTexture);
                // }

            } catch (Exception e) {
                // shut off VR when an error occurs
                Debug.LogError($"steamvrtest error: {e}");
                _hmdIsRunning = false;
            }
        }

        // reset cameras when HMD is turned off
        if (!_hmdIsRunning && _hmdIsRunningPrev) {
            Debug.Log("HMD is now off, resetting cameras...");

            // TODO: figure out why we can no longer manipulate the IVA camera in the regular game
        }
            
        _hmdIsRunningPrev = _hmdIsRunning;
    }

    /// <summary>
    /// Dispatch other miscellaneous OpenVR-specific events.
    /// </summary>
    private void DispatchOpenVREvents() {
        // copied from SteamVR_Render
        var vrEvent = new VREvent_t();
        var size = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(VREvent_t));
        for (var i = 0; i < 64; i++) {
            if (!OpenVR.System.PollNextEvent(ref vrEvent, size))
                break;

            // switch ((EVREventType)vrEvent.eventType) {
            //     case EVREventType.VREvent_InputFocusCaptured: // another app has taken focus (likely dashboard)
            //         if (vrEvent.data.process.oldPid == 0) {
            //             SteamVR_Events.InputFocus.Send(false);
            //         }
            //         break;
            //     case EVREventType.VREvent_InputFocusReleased: // that app has released input focus
            //         if (vrEvent.data.process.pid == 0) {
            //             SteamVR_Events.InputFocus.Send(true);
            //         }
            //         break;
            //     case EVREventType.VREvent_ShowRenderModels:
            //         SteamVR_Events.HideRenderModels.Send(false);
            //         break;
            //     case EVREventType.VREvent_HideRenderModels:
            //         SteamVR_Events.HideRenderModels.Send(true);
            //         break;
            //     default:
            //         SteamVR_Events.System((EVREventType)vrEvent.eventType).Send(vrEvent);
            //         break;
            // }
        }
    }

    private void ProcessHmdState() {
        switch (_hmdState) {
            case HmdState.Uninitialized:
                _hmdState = HmdState.Initializing;
                break;

            case HmdState.Initializing:
                InitializeHmd();
                break;

            case HmdState.InitFailed:
                if (DateTime.Now.Subtract(_hmdInitLastAttempt).TotalSeconds > 10) {
                    _hmdState = HmdState.Uninitialized;
                }
                break;
        }
    }

    private void InitializeHmd() {
        _hmdInitLastAttempt = DateTime.Now;
        try {
            InitializeOpenVR();
            _hmdState = HmdState.Initialized;
            Debug.Log("Initialized OpenVR.");

        } catch (Exception e) {
            _hmdState = HmdState.InitFailed;
            Debug.LogError("InitHMD failed: " + e);
        }
    }

        
    public static Matrix4x4 Matrix4x4_OpenVr2UnityFormat(ref HmdMatrix44_t mat44Openvr) {
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
        
    /// <summary>
    /// Renders a set of cameras onto a RenderTexture, and submit the frame to the HMD.
    /// </summary>
    private void RenderHmdCameras(EVREye eye)
    {
        var prevCameraPosition = ActiveCamera.transform.localPosition;

        // convert SteamVR poses to Unity coordinates
        var hmdTransform = new SteamVR_Utils.RigidTransform(_devicePoses[OpenVR.k_unTrackedDeviceIndex_Hmd].mDeviceToAbsoluteTracking);
        var hmdEyeTransform = new SteamVR_Utils.RigidTransform(OpenVR.System.GetEyeToHeadTransform(eye));

        ActiveCamera.transform.localRotation = hmdTransform.rot * hmdEyeTransform.rot;
        ActiveCamera.transform.localPosition = prevCameraPosition + hmdTransform.rot * hmdEyeTransform.pos;
            
        var projectionMatrix = OpenVR.System.GetProjectionMatrix(eye, ActiveCamera.nearClipPlane, ActiveCamera.farClipPlane);
        ActiveCamera.projectionMatrix = Matrix4x4_OpenVr2UnityFormat(ref projectionMatrix);
        
        // set texture to render to, then render
        var hmdEyeRenderTexture = _hmdEyeRenderTexture[(int)eye];
        if (!hmdEyeRenderTexture)
        {
            Debug.Log("missing render texture, recreating");
            uint renderTextureWidth = 0;
            uint renderTextureHeight = 0;
            OpenVR.System.GetRecommendedRenderTargetSize(ref renderTextureWidth, ref renderTextureHeight);
            hmdEyeRenderTexture = _hmdEyeRenderTexture[(int)eye] = new RenderTexture((int)renderTextureWidth, (int)renderTextureHeight, 24, RenderTextureFormat.ARGB32);
        }

        ActiveCamera.targetTexture = hmdEyeRenderTexture;
        ActiveCamera.Render();

        ActiveCamera.transform.localPosition = prevCameraPosition;

        var hmdEyeTexture = _hmdEyeTextures[(int) eye];
        hmdEyeTexture.handle = hmdEyeRenderTexture.GetNativeTexturePtr();

        // Submit frames to HMD
        var vrCompositorError = OpenVR.Compositor.Submit(eye, ref hmdEyeTexture, ref _hmdTextureBounds, EVRSubmitFlags.Submit_Default);
        if (vrCompositorError != EVRCompositorError.None) {
            throw new Exception("Submit (" + eye + ") failed: (" + (int)vrCompositorError + ") " + vrCompositorError.ToString());
        }
    }
    
    private void InitializeOpenVR() {

        // return if HMD has already been initialized
        if (_hmdState == HmdState.Initialized) {
            return;
        }

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

        // get HMD render target size
        uint renderTextureWidth = 0;
        uint renderTextureHeight = 0;
        OpenVR.System.GetRecommendedRenderTargetSize(ref renderTextureWidth, ref renderTextureHeight);

        // at the moment, only Direct3D11 is working with Kerbal Space Program
        var textureType = ETextureType.DirectX;
        switch (SystemInfo.graphicsDeviceType) {
            case UnityEngine.Rendering.GraphicsDeviceType.OpenGLCore:
            case UnityEngine.Rendering.GraphicsDeviceType.OpenGLES2:
            case UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3:
                textureType = ETextureType.OpenGL;
                throw new InvalidOperationException(SystemInfo.graphicsDeviceType.ToString() + " does not support VR. You must use -force-d3d11");
            case UnityEngine.Rendering.GraphicsDeviceType.Direct3D11:
                textureType = ETextureType.DirectX;
                break;
            case UnityEngine.Rendering.GraphicsDeviceType.Direct3D12:
                textureType = ETextureType.DirectX;
                break;
            default:
                throw new InvalidOperationException(SystemInfo.graphicsDeviceType.ToString() + " not supported");
        }

        // initialize render textures (for displaying on HMD)
        for (var i = 0; i < 2; i++) {
            _hmdEyeRenderTexture[i] = new RenderTexture((int)renderTextureWidth, (int)renderTextureHeight, 24, RenderTextureFormat.ARGB32);
            _hmdEyeRenderTexture[i].Create();
            _hmdEyeTextures[i].handle = _hmdEyeRenderTexture[i].GetNativeTexturePtr();
            _hmdEyeTextures[i].eColorSpace = EColorSpace.Auto;
            _hmdEyeTextures[i].eType = textureType;
        }

        // set rendering bounds on texture to render
        _hmdTextureBounds.uMin = 0.0f;
        _hmdTextureBounds.uMax = 1.0f;
        _hmdTextureBounds.vMin = 1.0f; // flip the vertical coordinate for some reason
        _hmdTextureBounds.vMax = 0.0f;
    }

    private void CloseHmd() {
        OpenVR.Shutdown();
        _hmdState = HmdState.Uninitialized;
    }

}