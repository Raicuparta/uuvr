using System;
using UnityEngine;
using Valve.VR;

    public class SteamVRTest : MonoBehaviour {
        
        #region Types
        public enum HmdState {
            Uninitialized,
            Initializing,
            Initialized,
            InitFailed,
        }
        #endregion


        #region Properties

        public Camera camera => Camera.main ?? Camera.current;

        /// <summary>
        /// Returns true if VR is currently running, i.e. tracking devices
        /// and rendering images to the headset.
        /// </summary>
        public static bool HmdIsRunning { get; private set; }

        /// <summary>
        /// Set to true to allow the VR images to be rendered
        /// to the game screen. False to disable.
        /// </summary>
        public static bool RenderHmdToScreen { get; set; } = true;

        #endregion


        #region Private Members

        // keep track of when the HMD is rendering images
        private static HmdState hmdState = HmdState.Uninitialized;
        private static bool hmdIsRunningPrev = false;
        private static DateTime hmdInitLastAttempt;

        // defines the bounds to texture bounds for rendering
        private static VRTextureBounds_t hmdTextureBounds;

        // these arrays each hold one object for the corresponding eye, where
        // index 0 = Left_Eye, index 1 = Right_Eye
        private static Texture_t[] hmdEyeTexture = new Texture_t[2];
        private static RenderTexture[] hmdEyeRenderTexture = new RenderTexture[2];

        // store the tracked device poses
        private static TrackedDevicePose_t[] devicePoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
        private static TrackedDevicePose_t[] gamePoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];

        #endregion


        /// <summary>
        /// Initialize the application GUI, singleton classes, and initialize OpenVR.
        /// </summary>
        private void Start() {

            InitializeHMD();

            // don't destroy this object when switching scenes
            DontDestroyOnLoad(this);
        }

        /// <summary>
        /// Overrides the OnDestroy method, called when plugin is destroyed.
        /// </summary>
        private void OnDestroy() {
            Debug.Log("VR shutting down...");
            CloseHMD();
        }

        
        public static float CalculatePredictedSecondsToPhotons() {
            float secondsSinceLastVsync = 0f;
            ulong frameCounter = 0;
            OpenVR.System.GetTimeSinceLastVsync(ref secondsSinceLastVsync, ref frameCounter);

            float displayFrequency = GetFloatTrackedDeviceProperty(ETrackedDeviceProperty.Prop_DisplayFrequency_Float);
            float vsyncToPhotons = GetFloatTrackedDeviceProperty(ETrackedDeviceProperty.Prop_SecondsFromVsyncToPhotons_Float);
            float frameDuration = 1f / displayFrequency;

            return frameDuration - secondsSinceLastVsync + vsyncToPhotons;
        }

        public static float GetFloatTrackedDeviceProperty(ETrackedDeviceProperty property, uint device = OpenVR.k_unTrackedDeviceIndex_Hmd) {
            ETrackedPropertyError propertyError = ETrackedPropertyError.TrackedProp_Success;
            float value = OpenVR.System.GetFloatTrackedDeviceProperty(device, property, ref propertyError);
            if (propertyError != ETrackedPropertyError.TrackedProp_Success) {
                throw new Exception("Failed to obtain tracked device property \"" +
                                    property + "\", error: (" + (int)propertyError + ") " + propertyError.ToString());
            }
            return value;
        }

        private void Update()
        {
            EVRCompositorError vrCompositorError = EVRCompositorError.None;
            vrCompositorError = OpenVR.Compositor.WaitGetPoses(devicePoses, gamePoses);

            if (vrCompositorError != EVRCompositorError.None) {
                throw new Exception("WaitGetPoses failed: (" + (int)vrCompositorError + ") " + vrCompositorError.ToString());
            }
        }

        /// <summary>
        /// On LateUpdate, dispatch OpenVR events, run the main HMD loop code.
        /// </summary>
        private void LateUpdate() {
            // dispatch any OpenVR events
            if (hmdState == HmdState.Initialized) {
                DispatchOpenVREvents();
            }

            // process the state of OpenVR
            ProcessHmdState();

            // check if we are running the HMD
            HmdIsRunning = hmdState == HmdState.Initialized;

            // perform regular updates if HMD is initialized
            if (HmdIsRunning) {

                // we've just started VR
                if (!hmdIsRunningPrev) {
                    Debug.Log("HMD is now on");
                    ResetInitialHmdPosition();
                }

                try {
                    HmdMatrix34_t vrLeftEyeTransform = OpenVR.System.GetEyeToHeadTransform(EVREye.Eye_Left);
                    HmdMatrix34_t vrRightEyeTransform = OpenVR.System.GetEyeToHeadTransform(EVREye.Eye_Right);

                    // convert SteamVR poses to Unity coordinates
                    var hmdTransform = new SteamVR_Utils.RigidTransform(devicePoses[OpenVR.k_unTrackedDeviceIndex_Hmd].mDeviceToAbsoluteTracking);
                    SteamVR_Utils.RigidTransform[] hmdEyeTransform = new SteamVR_Utils.RigidTransform[2];
                    hmdEyeTransform[0] = new SteamVR_Utils.RigidTransform(vrLeftEyeTransform);
                    hmdEyeTransform[1] = new SteamVR_Utils.RigidTransform(vrRightEyeTransform);

                    // don't highlight parts with the mouse
                    // Mouse.HoveredPart = null;

                    // render each eye
                    for (int i = 0; i < 2; i++) {
                        RenderHmdCameras(
                            (EVREye)i,
                            hmdTransform,
                            hmdEyeTransform[i],
                            ref hmdEyeRenderTexture[i],
                            hmdEyeTexture[i]);
                    }

                    // [insert dark magic here]
                    OpenVR.Compositor.PostPresentHandoff();

                    // render to the game screen
                    if (RenderHmdToScreen) {
                        Graphics.Blit(hmdEyeRenderTexture[0], null as RenderTexture);
                    }

                } catch (Exception e) {
                    // shut off VR when an error occurs
                    Debug.LogError($"steamvrtest error: {e}");
                    HmdIsRunning = false;
                }
            }

            // reset cameras when HMD is turned off
            if (!HmdIsRunning && hmdIsRunningPrev) {
                Debug.Log("HMD is now off, resetting cameras...");

                // TODO: figure out why we can no longer manipulate the IVA camera in the regular game
            }
            
            hmdIsRunningPrev = HmdIsRunning;
        }

        /// <summary>
        /// Dispatch other miscellaneous OpenVR-specific events.
        /// </summary>
        private void DispatchOpenVREvents() {
            // copied from SteamVR_Render
            var vrEvent = new VREvent_t();
            var size = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(VREvent_t));
            for (int i = 0; i < 64; i++) {
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
            switch (hmdState) {
                case HmdState.Uninitialized:
                    hmdState = HmdState.Initializing;
                    break;

                case HmdState.Initializing:
                    InitializeHMD();
                    break;

                case HmdState.InitFailed:
                    if (DateTime.Now.Subtract(hmdInitLastAttempt).TotalSeconds > 10) {
                        hmdState = HmdState.Uninitialized;
                    }
                    break;
            }
        }

        private void InitializeHMD() {
            hmdInitLastAttempt = DateTime.Now;
            try {
                InitializeOpenVR();
                hmdState = HmdState.Initialized;
                Debug.Log("Initialized OpenVR.");

            } catch (Exception e) {
                hmdState = HmdState.InitFailed;
                Debug.LogError("InitHMD failed: " + e);
            }
        }

        
        public static Matrix4x4 Matrix4x4_OpenVr2UnityFormat(ref HmdMatrix44_t mat44_openvr) {
            Matrix4x4 mat44_unity = Matrix4x4.identity;
            mat44_unity.m00 = mat44_openvr.m0;
            mat44_unity.m01 = mat44_openvr.m1;
            mat44_unity.m02 = mat44_openvr.m2;
            mat44_unity.m03 = mat44_openvr.m3;
            mat44_unity.m10 = mat44_openvr.m4;
            mat44_unity.m11 = mat44_openvr.m5;
            mat44_unity.m12 = mat44_openvr.m6;
            mat44_unity.m13 = mat44_openvr.m7;
            mat44_unity.m20 = mat44_openvr.m8;
            mat44_unity.m21 = mat44_openvr.m9;
            mat44_unity.m22 = mat44_openvr.m10;
            mat44_unity.m23 = mat44_openvr.m11;
            mat44_unity.m30 = mat44_openvr.m12;
            mat44_unity.m31 = mat44_openvr.m13;
            mat44_unity.m32 = mat44_openvr.m14;
            mat44_unity.m33 = mat44_openvr.m15;
            return mat44_unity;
        }
        
        /// <summary>
        /// Renders a set of cameras onto a RenderTexture, and submit the frame to the HMD.
        /// </summary>
        private void RenderHmdCameras(
            EVREye eye,
            SteamVR_Utils.RigidTransform hmdTransform,
            SteamVR_Utils.RigidTransform hmdEyeTransform,
            ref RenderTexture hmdEyeRenderTexture,
            Texture_t hmdEyeTexture) {

            /**
             * hmdEyeTransform is in a coordinate system that follows the headset, where
             * the origin is the headset device position. Therefore the eyes are at a constant
             * offset from the device. hmdEyeTransform does not change (per eye).
             *      hmdEyeTransform.x+  towards the right of the headset
             *      hmdEyeTransform.y+  towards the top the headset
             *      hmdEyeTransform.z+  towards the front of the headset
             *
             * hmdTransform is in a coordinate system set in physical space, where the
             * origin is the initial seated position. Or for room-scale, the physical origin of the room.
             *      hmdTransform.x+     towards the right
             *      hmdTransform.y+     upwards
             *      hmdTransform.z+     towards the front
             *
             *  Scene.InitialPosition and Scene.InitialRotation are the Unity world coordinates where
             *  we initialize the VR scene, i.e. the origin of a coordinate system that maps
             *  1-to-1 with physical space.
             *
             *  1. Calculate the position of the eye in the physical coordinate system.
             *  2. Transform the calculated position into Unity world coordinates, offset from
             *     InitialPosition and InitialRotation.
             */


            var prevCameraPosition = camera.transform.localPosition;
            camera.transform.localRotation = hmdTransform.rot * hmdEyeTransform.rot;
            camera.transform.localPosition = prevCameraPosition + hmdTransform.rot * hmdEyeTransform.pos;
            
            // set projection matrix
            
            HmdMatrix44_t projectionMatrix = OpenVR.System.GetProjectionMatrix(eye, camera.nearClipPlane, camera.farClipPlane);
            camera.projectionMatrix = Matrix4x4_OpenVr2UnityFormat(ref projectionMatrix);
            
            // set texture to render to, then render
            if (!hmdEyeRenderTexture)
            {
                uint renderTextureWidth = 0;
                uint renderTextureHeight = 0;
                OpenVR.System.GetRecommendedRenderTargetSize(ref renderTextureWidth, ref renderTextureHeight);
                hmdEyeRenderTexture = new RenderTexture((int)renderTextureWidth, (int)renderTextureHeight, 24, RenderTextureFormat.ARGB32);
            }

            camera.targetTexture = hmdEyeRenderTexture;
            camera.Render();

            camera.transform.localPosition = prevCameraPosition;

            hmdEyeTexture.handle = hmdEyeRenderTexture.GetNativeTexturePtr();

            // Submit frames to HMD
            EVRCompositorError vrCompositorError = OpenVR.Compositor.Submit(eye, ref hmdEyeTexture, ref hmdTextureBounds, EVRSubmitFlags.Submit_Default);
            if (vrCompositorError != EVRCompositorError.None) {
                throw new Exception("Submit (" + eye + ") failed: (" + (int)vrCompositorError + ") " + vrCompositorError.ToString());
            }
        }

        /// <summary>
        /// Initialize HMD using OpenVR API calls.
        /// </summary>
        /// <returns>True on successful initialization, false otherwise.</returns>
        private static void InitializeOpenVR() {

            // return if HMD has already been initialized
            if (hmdState == HmdState.Initialized) {
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
            EVRInitError hmdInitErrorCode = EVRInitError.None;
            OpenVR.Init(ref hmdInitErrorCode, EVRApplicationType.VRApplication_Scene);
            if (hmdInitErrorCode != EVRInitError.None) {
                throw new Exception("OpenVR error: " + OpenVR.GetStringForHmdError(hmdInitErrorCode));
            }

            // reset "seated position" and capture initial position. this means you should hold the HMD in
            // the position you would like to consider "seated", before running this code.
            ResetInitialHmdPosition();

            // get HMD render target size
            uint renderTextureWidth = 0;
            uint renderTextureHeight = 0;
            OpenVR.System.GetRecommendedRenderTargetSize(ref renderTextureWidth, ref renderTextureHeight);

            // at the moment, only Direct3D11 is working with Kerbal Space Program
            ETextureType textureType = ETextureType.DirectX;
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
            for (int i = 0; i < 2; i++) {
                hmdEyeRenderTexture[i] = new RenderTexture((int)renderTextureWidth, (int)renderTextureHeight, 24, RenderTextureFormat.ARGB32);
                hmdEyeRenderTexture[i].Create();
                hmdEyeTexture[i].handle = hmdEyeRenderTexture[i].GetNativeTexturePtr();
                hmdEyeTexture[i].eColorSpace = EColorSpace.Auto;
                hmdEyeTexture[i].eType = textureType;
            }

            // set rendering bounds on texture to render
            hmdTextureBounds.uMin = 0.0f;
            hmdTextureBounds.uMax = 1.0f;
            hmdTextureBounds.vMin = 1.0f; // flip the vertical coordinate for some reason
            hmdTextureBounds.vMax = 0.0f;
        }

        /// <summary>
        /// Sets the current real-world position of the HMD as the seated origin.
        /// </summary>
        public static void ResetInitialHmdPosition() {
            if (hmdState == HmdState.Initialized) {
                // OpenVR.System.ResetSeatedZeroPose();
            }
        }

        /// <summary>
        /// Shuts down the OpenVR API.
        /// </summary>
        private void CloseHMD() {
            OpenVR.Shutdown();
            hmdState = HmdState.Uninitialized;
        }

    }
