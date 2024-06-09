#if MODERN
using System;
using Unity.XR.OpenVR;
using UnityEngine;
using UnityEngine.XR.Management;

namespace Uuvr.VrTogglers;

public abstract class XrPluginToggler: VrToggler
{
    protected XRGeneralSettings _generalSettings;
    protected XRManagerSettings _managerSetings;
    
    protected override bool SetUp()
    {
        _generalSettings = ScriptableObject.CreateInstance<XRGeneralSettings>();
        _managerSetings = ScriptableObject.CreateInstance<XRManagerSettings>();
        _generalSettings.Manager = _managerSetings;
        
        #pragma warning disable CS0618
        /*
         * ManagerSettings.loaders is deprecated but very useful, allows me to add the xr loader without reflection.
         * Should be fine unless the game's Unity version gets majorly updated, in which case the whole mod will be
         * broken, so I'll have to update it anyway.
         */
        _managerSetings.loaders.Add(CreateLoader());
        #pragma warning restore CS0618
        
        _managerSetings.InitializeLoaderSync();
        if (_managerSetings.activeLoader == null) throw new Exception("Cannot initialize OpenXR Loader. Maybe The VR headset wasn't ready?");

        return true;
    }

    protected override bool EnableVr()
    {
        _managerSetings.StartSubsystems();
        var initializationSuccess = _managerSetings.activeLoader.Initialize();
        var startSuccess = _managerSetings.activeLoader.Start();
        return initializationSuccess && startSuccess;
    }

    protected override bool DisableVr()
    {
        var stopSuccess = _managerSetings.activeLoader.Stop();
        var deinitializationSuccess = _managerSetings.activeLoader.Deinitialize();
        return stopSuccess && deinitializationSuccess;
    }

    protected abstract XRLoader CreateLoader();
}
#endif