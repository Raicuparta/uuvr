#if MODERN

using System;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;

namespace Uuvr;

public static class VrToggle
{
    public static bool IsVrEnabled { get; private set; }

    private static OpenXRLoader _openXrLoader;
    private static bool _isXrSetUp;
    private static bool IsInitialized {
        get {
            return _openXrLoader != null && _openXrLoader.IsInitialized;
        }
    }

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

        IsVrEnabled = IsInitialized;
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

        IsVrEnabled = IsInitialized;
    }

    private static void SetUp()
    {
        if (_isXrSetUp) return;

        XRGeneralSettings? generalSettings = ScriptableObject.CreateInstance<XRGeneralSettings>();
        XRManagerSettings? managerSetings = ScriptableObject.CreateInstance<XRManagerSettings>();
        _openXrLoader = ScriptableObject.CreateInstance<OpenXRLoader>();
        OpenXRSettings.Instance.renderMode = OpenXRSettings.RenderMode.MultiPass;

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
        if (managerSetings.activeLoader == null) throw new Exception("Cannot initialize OpenXR Loader. Maybe The VR headset wasn't ready?");
        
        managerSetings.StartSubsystems();

        _isXrSetUp = true;
    }
}

#endif