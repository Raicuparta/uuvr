using System;
using System.Reflection;

namespace Uuvr.VrTogglers;

public class LegacyOpenVrToggler: VrToggler
{
    private static Type? _xrSettingsType;
    private static PropertyInfo? _xrEnabledProperty;

    protected override bool SetUp()
    {
        _xrSettingsType =
            Type.GetType("UnityEngine.XR.XRSettings, UnityEngine.XRModule") ??
            Type.GetType("UnityEngine.XR.XRSettings, UnityEngine.VRModule") ??
            Type.GetType("UnityEngine.VR.VRSettings, UnityEngine");
        
        _xrEnabledProperty = _xrSettingsType.GetProperty("enabled");

        return true;
    }

    protected override bool EnableVr()
    {
        _xrEnabledProperty.SetValue(null, true, null);
        return true;
    }

    protected override bool DisableVr()
    {
        _xrEnabledProperty.SetValue(null, false, null);
        return true;
    }
}
