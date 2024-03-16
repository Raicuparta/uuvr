#if MODERN

using System;
using Unity.XR.OpenVR;
using UnityEngine;
using UnityEngine.XR.Management;
using Valve.VR;

namespace Uuvr;

public static class VrToggle
{
    public static bool IsVrEnabled { get; private set; }

    private static OpenVRLoader _xrLoader;
    private static bool _isXrSetUp;

    public static void ToggleVr()
    {
        if (!_isXrSetUp) SetUp();

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

        IsVrEnabled = !IsVrEnabled;
    }

    public static void SetVrEnabled(bool vrEnabled)
    {
        SetUp();

        if (vrEnabled)
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

        IsVrEnabled = vrEnabled;
    }

    private static void SetUp()
    {
        if (_isXrSetUp) return;

        EVRInitError openVrError = EVRInitError.None;
        OpenVR.Init(ref openVrError);
        Debug.LogWarning($"OpenVR Error: {openVrError}");
        
        XRGeneralSettings? generalSettings = ScriptableObject.CreateInstance<XRGeneralSettings>();
        XRManagerSettings? managerSetings = ScriptableObject.CreateInstance<XRManagerSettings>();
        _xrLoader = ScriptableObject.CreateInstance<OpenVRLoader>();

        OpenVRSettings? openVrSettings = OpenVRSettings.GetSettings();
        if (openVrSettings == null) throw new Exception("OpenVRSettings instance is null");
        openVrSettings.EditorAppKey = "uuvr";
        openVrSettings.InitializationType = OpenVRSettings.InitializationTypes.Scene;
        openVrSettings.StereoRenderingMode = OpenVRSettings.StereoRenderingModes.MultiPass;
        openVrSettings.SetMirrorViewMode(OpenVRSettings.MirrorViewModes.Right);
        
        generalSettings.Manager = managerSetings;
        #pragma warning disable CS0618
        /*
         * ManagerSettings.loaders is deprecated but very useful, allows me to add the xr loader without reflection.
         * Should be fine unless the game's Unity version gets majorly updated, in which case the whole mod will be
         * broken, so I'll have to update it anyway.
         */
        managerSetings.loaders.Add(_xrLoader);
        #pragma warning restore CS0618

        managerSetings.InitializeLoaderSync();
        if (managerSetings.activeLoader == null) throw new Exception("Cannot initialize OpenXR Loader. Maybe The VR headset wasn't ready?");
        
        managerSetings.StartSubsystems();

        _isXrSetUp = true;
    }
}

#endif