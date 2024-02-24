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
        var xrLoader = ScriptableObject.CreateInstance<OpenXRLoader>();
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
        managerSetings.loaders.Add(xrLoader);
        #pragma warning restore CS0618

        managerSetings.InitializeLoaderSync();
        if (managerSetings.activeLoader == null) throw new Exception("Cannot initialize OpenVR Loader");
        
        managerSetings.StartSubsystems();

        var mainCamera = Camera.main ?? Camera.current;
        
        
        var poseDriver = mainCamera.gameObject.AddComponent<TrackedPoseDriver>();
        poseDriver.trackingType = TrackedPoseDriver.TrackingType.RotationOnly;
        poseDriver.rotationAction = new InputAction("VrRotation", InputActionType.PassThrough, "HeadTrackingOpenXR/centereyerotation");

        isXrSetUp = true;
    }
}

#endif