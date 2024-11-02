using System;
using System.Reflection;

namespace Uuvr.VrTogglers;

// This is for Unity versions that let you easily toggle VR by (mostly) just checking a box in the Editor.
// These versions came with batteries included (mostly), so the code to toggle VR is pretty simple.
// It does require patching before starting the game though, which happens in the Uuvr.Patcher project.
// Specifically, it requires adding devices to the enabledVRDevices array in globalGameMangers, under BuildSettings.
// This specific method seems to work on versions between 5.6 and 2019.something (not exactly sure).
// Earlier versions also had simple VR toggles but the APIs might differ too much.
public class NativeOpenVrToggler: VrToggler
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
