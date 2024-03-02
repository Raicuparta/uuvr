#if LEGACY
using System;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Uuvr;

public static class VrToggle
{
    public static bool IsVrEnabled {
        get {
            return _xrEnabledProperty != null && (bool)_xrEnabledProperty.GetValue(null, null);
        }
    }

    private static Type? _xrSettingsType;
    private static PropertyInfo? _xrEnabledProperty;
    private static bool _isSetUp;

    private static void SetUp()
    {
        if (_isSetUp) return;

        _xrSettingsType =
            Type.GetType("UnityEngine.XR.XRSettings, UnityEngine.XRModule") ??
            Type.GetType("UnityEngine.XR.XRSettings, UnityEngine.VRModule") ??
            Type.GetType("UnityEngine.VR.VRSettings, UnityEngine");
        
        _xrEnabledProperty = _xrSettingsType.GetProperty("enabled");

        _isSetUp = true;
    }
    
    public static void ToggleVr()
    {
        SetVrEnabled(!IsVrEnabled);
    }

    public static void SetVrEnabled(bool vrEnabled)
    {
        SetUp();
        
        Debug.Log($"Setting VR enabled to {vrEnabled}");

        _xrEnabledProperty.SetValue(null, vrEnabled, null);
    }
}
#endif