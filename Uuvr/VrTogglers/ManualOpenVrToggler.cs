using BepInEx.Configuration;
using UnityEngine;
using Uuvr.OpenVR;

namespace Uuvr.VrTogglers;

// This one should work on the widest range of Unity versions, not sure which yet.
// Specifically made to handle modern IL2CPP games where it's difficult to inject the XR Plugins,
// but should also be useful for older games where there's poor or no native VR support.
public class ManualOpenVrToggler: VrToggler
{
    private static OpenVrManager? _openVrManager;

    public ManualOpenVrToggler()
    {
        ModConfiguration.Instance.Config.SettingChanged += OnConfigChanged;
    }
    
    ~ManualOpenVrToggler()
    {
        ModConfiguration.Instance.Config.SettingChanged -= OnConfigChanged;
    }

    private static void OnConfigChanged(object? _, SettingChangedEventArgs __)
    {
        OnConfigChanged();
    }

    private static void OnConfigChanged()
    {

        // Smaller eye distance makes the world looks bigger.
        _openVrManager.eyeDistanceMultiplier = 1f / ModConfiguration.Instance.WorldScale.Value;
    }

    protected override bool SetUp()
    {
        return true;
    }

    protected override bool EnableVr()
    {
        _openVrManager = OpenVrManager.Create();
        OnConfigChanged();
        return _openVrManager != null;
    }

    protected override bool DisableVr()
    {
        // Disabling VR needs extra work, so for now once you enable it you can't go back (as God intended).
        // if (_openVrManager != null)
        // {
        //     GameObject.Destroy(_openVrManager.gameObject);
        // }
        
        // Calling config changed here just to make it easier to test for now.
        OnConfigChanged();
        return false;
    }
}