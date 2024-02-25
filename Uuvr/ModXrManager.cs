#if MODERN

using System;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

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

        SetUpCameraTracking();

        isXrSetUp = true;
    }

    private void SetUpCameraTracking()
    {
        Camera? mainCamera = Camera.main ?? Camera.current;
        mainCamera.gameObject.AddComponent<VrCamera>();
    }
}

#endif