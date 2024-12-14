using System;
using System.Collections;
using BepInEx.Unity.IL2CPP.Utils;
using UnityEngine;

namespace Uuvr.OpenVR;

public class OpenVrManager : MonoBehaviour {
    public Camera vrCamera;
    public bool renderHmdToScreen;
    public float eyeDistanceMultiplier = 1.0f;

    private readonly TrackedDevicePose_t[] _devicePoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
    private readonly TrackedDevicePose_t[] _gamePoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];

    private RenderTexture _hmdEyeRenderTexture;
    private float _aspect;
    private float _fieldOfView;

    public static OpenVrManager Create()
    {
        var camera = Camera.main;
        if (camera == null) camera = Camera.current;
        if (camera == null)
        {
            Debug.LogWarning("Failed to find suitable camera for OpenVR, can't initialize.");
            return null;
        }
        var instanceObject = new GameObject(nameof(OpenVrManager));
        instanceObject.SetActive(false);
        var instance = instanceObject.AddComponent<OpenVrManager>();
        instance.vrCamera = camera;
        instanceObject.SetActive(true);

        return instance;
    }
    
    private void OnEnable()
    {
        SetUpCamera();
        InitializeOpenVR();
        this.StartCoroutine(RenderLoop());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private void OnDestroy()
    {
        OpenVR.Shutdown();
    }

    private void Update()
    {
        OpenVrUnityHooks.PostPresentHandoff();

        // Application.targetFrameRate = -1; // disabling for now since caused log spam in sons of the forest.
        Application.runInBackground = true; // don't require companion window focus.
        QualitySettings.maxQueuedFrames = -1;
        QualitySettings.vSyncCount = 0; // this applies to the companion window.
        // UpdatePoses();
    }

    private void FixedUpdate()
    {
        // We want to call this as soon after Present as possible.
        OpenVrUnityHooks.PostPresentHandoff();
    }

    private void UpdatePoses()
    {
        OpenVrUnityHooks.WaitGetPoses();

        // Hack to flush render event that was queued in Update (this ensures WaitGetPoses has returned before we grab the new values).
        _hmdEyeRenderTexture.GetNativeTexturePtr();

        OpenVR.Compositor.GetLastPoses(_devicePoses, _gamePoses);
    }

    private readonly WaitForEndOfFrame _waitingForEndOfFrame = new();

    private IEnumerator RenderLoop()
    {
        while (enabled)
        {
            yield return _waitingForEndOfFrame;
            
            try
            {
                Graphics.SetRenderTarget(_hmdEyeRenderTexture);

                var vrLeftEyeTransform = OpenVR.System.GetEyeToHeadTransform(EVREye.Eye_Left);
                var vrRightEyeTransform = OpenVR.System.GetEyeToHeadTransform(EVREye.Eye_Right);
                UpdatePoses();

                // convert SteamVR poses to Unity coordinates
                var hmdTransform =
                    new Utils.RigidTransform(_devicePoses[OpenVR.k_unTrackedDeviceIndex_Hmd]
                        .mDeviceToAbsoluteTracking);
                var hmdEyeTransform = new Utils.RigidTransform[2];
                hmdEyeTransform[0] = new Utils.RigidTransform(vrLeftEyeTransform);
                hmdEyeTransform[1] = new Utils.RigidTransform(vrRightEyeTransform);

                // render each eye
                for (var i = 0; i < 2; i++)
                    RenderEye(
                        (EVREye) i,
                        hmdTransform,
                        hmdEyeTransform[i]);

                // render to the game screen
                if (renderHmdToScreen) Graphics.Blit(_hmdEyeRenderTexture, null as RenderTexture);

                Graphics.SetRenderTarget(null);
            }
            catch (Exception e)
            {
                // shut off VR when an error occurs
                Debug.LogError(e.Message);
                OpenVR.Shutdown();
                break;
            }
        }
    }

    private void SetUpCamera()
    {
        vrCamera.fieldOfView = _fieldOfView;
        vrCamera.aspect = _aspect;
        vrCamera.enabled = false;
    }

    private static Matrix4x4 Matrix4x4_OpenVr2UnityFormat(ref HmdMatrix44_t mat44Openvr)
    {
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

    private void RenderEye(
        EVREye eye,
        Utils.RigidTransform hmdTransform,
        Utils.RigidTransform hmdEyeTransform)
    {
        if (vrCamera == null) return;
        
        var cameraTransform = vrCamera.transform;
        var prevCameraRotation = cameraTransform.localRotation;
        var prevCameraPosition = cameraTransform.localPosition;
        cameraTransform.localPosition = prevCameraPosition + hmdTransform.rot * hmdEyeTransform.pos * eyeDistanceMultiplier;
        cameraTransform.localRotation = hmdTransform.rot * hmdEyeTransform.rot;

        var projectionMatrix =
            OpenVR.System.GetProjectionMatrix(eye, vrCamera.nearClipPlane, vrCamera.farClipPlane,
                EGraphicsAPIConvention.API_DirectX);
        vrCamera.projectionMatrix = Matrix4x4_OpenVr2UnityFormat(ref projectionMatrix);

        vrCamera.targetTexture = _hmdEyeRenderTexture;
        vrCamera.Render();

        cameraTransform.localRotation = prevCameraRotation;
        cameraTransform.localPosition = prevCameraPosition;
        
        if (eye == EVREye.Eye_Left)
        {
            // Get gpu started on work early to avoid bubbles at the top of the frame.
            OpenVrUnityHooks.Flush();

            OpenVrUnityHooks.SubmitLeftEye();
        }
        else
        {
            OpenVrUnityHooks.SubmitRightEye();
        }
    }

    private void InitializeOpenVR()
    {
        // check if HMD is connected on the system
        if (!OpenVR.IsHmdPresent()) throw new InvalidOperationException("HMD not found on this system");

        // check if SteamVR runtime is installed
        if (!OpenVR.IsRuntimeInstalled())
            throw new InvalidOperationException("SteamVR runtime not found on this system");

        // initialize HMD
        var hmdInitErrorCode = EVRInitError.None;
        OpenVR.Init(ref hmdInitErrorCode);
        if (hmdInitErrorCode != EVRInitError.None)
            throw new Exception("OpenVR error: " + OpenVR.GetStringForHmdError(hmdInitErrorCode));

        SetUp();
    }

    private void SetUp()
    {
        uint w = 0, h = 0;
        OpenVR.System.GetRecommendedRenderTargetSize(ref w, ref h);

        float lLeft = 0.0f, lRight = 0.0f, lTop = 0.0f, lBottom = 0.0f;
        OpenVR.System.GetProjectionRaw(EVREye.Eye_Left, ref lLeft, ref lRight, ref lTop, ref lBottom);

        float rLeft = 0.0f, rRight = 0.0f, rTop = 0.0f, rBottom = 0.0f;
        OpenVR.System.GetProjectionRaw(EVREye.Eye_Right, ref rLeft, ref rRight, ref rTop, ref rBottom);

        var tanHalfFov = new Vector2(
            Mathf.Max(-lLeft, lRight, -rLeft, rRight),
            Mathf.Max(-lTop, lBottom, -rTop, rBottom));

        var hmdTextureBounds = new VRTextureBounds_t
        {
            uMin = 0.0f,
            uMax = 1.0f,
            vMin = 1.0f, // flip the vertical coordinate (should only be needed in DirectX)
            vMax = 0.0f
        };

        OpenVrUnityHooks.SetSubmitParams(hmdTextureBounds, hmdTextureBounds, EVRSubmitFlags.Submit_Default);

        var hdr = vrCamera.allowHDR;
        var aa = QualitySettings.antiAliasing == 0 ? 1 : QualitySettings.antiAliasing;
        var format = hdr ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32;
        _aspect = tanHalfFov.x / tanHalfFov.y;
        _fieldOfView = 2.0f * Mathf.Atan(tanHalfFov.y) * Mathf.Rad2Deg;

        // initialize render texture (for displaying on HMD)
        _hmdEyeRenderTexture = new RenderTexture((int) w, (int) h, 0, format)
        {
            antiAliasing = aa
        };

        var colorSpace = hdr && QualitySettings.activeColorSpace == ColorSpace.Gamma
            ? EColorSpace.Gamma
            : EColorSpace.Auto;
        OpenVrUnityHooks.SetColorSpace(colorSpace);
    }
}