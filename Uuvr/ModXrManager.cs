#if MODERN

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;

namespace Uuvr;

public class ModXrManager : MonoBehaviour
{
    public static bool IsVrEnabled;
    private static OpenXRLoader _openXrLoader;
    private bool isXrSetUp;
    private static bool IsInitialized => _openXrLoader != null && _openXrLoader.GetValue<bool>("isInitialized");
    private readonly KeyboardKey _toggleKey = new (KeyboardKey.KeyCode.F2);

    private void Update()
    {
        if (_toggleKey.UpdateIsDown()) ToggleXr();
    }

    private void ToggleXr()
    {
        if (!isXrSetUp) SetUpXr();

        if (!IsVrEnabled)
        {
            XRGeneralSettings.Instance.Manager.StartSubsystems();
            XRGeneralSettings.Instance.Manager.activeLoader.Initialize();
            XRGeneralSettings.Instance.Manager.activeLoader.Start();
        }
        else
        {
            XRGeneralSettings.Instance.Manager.activeLoader.Stop();
            XRGeneralSettings.Instance.Manager.activeLoader.Deinitialize();
        }

        IsVrEnabled = IsInitialized;
    }

    private void SetUpXr()
    {

        var generalSettings = ScriptableObject.CreateInstance<XRGeneralSettings>();
        var managerSetings = ScriptableObject.CreateInstance<XRManagerSettings>();
        var features = new OpenXRInteractionFeature[]
        {
            ScriptableObject.CreateInstance<HTCViveControllerProfile>(),
            ScriptableObject.CreateInstance<OculusTouchControllerProfile>(),
            ScriptableObject.CreateInstance<MicrosoftMotionControllerProfile>(),
            ScriptableObject.CreateInstance<ValveIndexControllerProfile>()
        };
        _openXrLoader = ScriptableObject.CreateInstance<OpenXRLoader>();
        OpenXRSettings.Instance.renderMode = OpenXRSettings.RenderMode.MultiPass;
        OpenXRSettings.Instance.SetValue("features", features);
        foreach (var feature in features) feature.enabled = true;

        generalSettings.Manager = managerSetings;
        #pragma warning disable CS0618
        /*
         * ManagerSettings.loaders is deprecated but very useful, allows me to add the xr loader without reflection.
         * Should be fine unless the game's Unity version gets majorly updated, in which case the whole mod will be
         * broken, so I'll have to update it anyway.
         */
        managerSetings.loaders.Add(_openXrLoader);
        #pragma warning restore CS0618

        managerSetings.InitializeLoaderSync();
        if (managerSetings.activeLoader == null) throw new Exception("Cannot initialize OpenVR Loader");
        
        managerSetings.StartSubsystems();

        SetUpCameraTracking();

        isXrSetUp = true;
    }

    private void SetUpCameraTracking()
    {
        var mainCamera = Camera.main ?? Camera.current;
        
        Type spacialTrackingPoseDriverType = Type.GetType("UnityEngine.SpatialTracking.TrackedPoseDriver, UnityEngine.SpatialTracking");
        if (spacialTrackingPoseDriverType != null)
        {
            SetUpSpacialTrackingPoseDriver(spacialTrackingPoseDriverType, mainCamera);
            return;
        }
        
        Type inputSystemPoseDriverType = Type.GetType("UnityEngine.InputSystem.XR.TrackedPoseDriver, UnityEngine.InputSystem");
        if (inputSystemPoseDriverType != null)
        {
            SetUpInputSystemPoseDriver(spacialTrackingPoseDriverType, mainCamera);
            return;
        }
    }
    private void SetUpInputSystemPoseDriver(Type spacialTrackingPoseDriverType, Camera mainCamera)
    {
        TrackedPoseDriver poseDriver = mainCamera.gameObject.AddComponent(spacialTrackingPoseDriverType) as TrackedPoseDriver;
        poseDriver.trackingType = TrackedPoseDriver.TrackingType.RotationOnly;
        poseDriver.rotationAction = new InputAction("VrRotation", InputActionType.PassThrough, "HeadTrackingOpenXR/centereyerotation");
    }

    private void SetUpSpacialTrackingPoseDriver(Type spacialTrackingPoseDriverType, Camera mainCamera)
    {
        var poseDriver = mainCamera.gameObject.AddComponent(spacialTrackingPoseDriverType);
        poseDriver.SetValue("trackingType", 1); // rotation only.
    }


}

#endif