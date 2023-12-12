using System;
using System.Reflection;
using UnityEngine;
using UuvrPluginMono;

namespace UuvrPluginIl2Cpp;

public class UuvrIl2cppBehaviour: MonoBehaviour
{
    private readonly KeyboardKey _toggleVrKey = new (KeyboardKey.KeyCode.F3);
    
    private Type _xrSettingsType;
    private PropertyInfo _xrEnabledProperty;
    
    public UuvrIl2cppBehaviour(IntPtr pointer) : base(pointer)
    {
    }

    private void Start()
    {
        _xrSettingsType =
            Type.GetType("UnityEngine.XR.XRSettings, UnityEngine.XRModule") ??
            Type.GetType("UnityEngine.XR.XRSettings, UnityEngine.VRModule") ??
            Type.GetType("UnityEngine.VR.VRSettings, UnityEngine");
        
        _xrEnabledProperty = _xrSettingsType.GetProperty("enabled");

        SetXrEnabled(false);
        SetPositionTrackingEnabled(false);
    }

    private void Update()
    {
        if (_toggleVrKey.UpdateIsDown()) ToggleXr();
    }

    private void ToggleXr()
    {
        bool xrEnabled = (bool) _xrEnabledProperty.GetValue(null);
        SetXrEnabled(!xrEnabled);
    }

    private void SetXrEnabled(bool enabled)
    {
        Console.WriteLine($"Setting XR enabled to {enabled}");

        _xrEnabledProperty.SetValue(null, enabled, null);
    }

    private void SetPositionTrackingEnabled(bool enabled)
    {
        Type inputTrackingType = 
            Type.GetType("UnityEngine.XR.InputTracking, UnityEngine.XRModule") ??
            Type.GetType("UnityEngine.XR.InputTracking, UnityEngine.VRModule");

        if (inputTrackingType != null)
        {
            PropertyInfo disablePositionalTrackingProperty = inputTrackingType.GetProperty("disablePositionalTracking");
            if (disablePositionalTrackingProperty != null)
            {
                disablePositionalTrackingProperty.SetValue(null, !enabled, null);
            }
            else
            {
                Console.WriteLine("Failed to get property disablePositionalTracking");
            }
        }
        else
        {
            Console.WriteLine("Failed to get type UnityEngine.XR.InputTracking");
        }
    }
}
