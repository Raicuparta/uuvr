using System;
using System.Reflection;
using System.Runtime.InteropServices;
using BepInEx;

namespace UnityUniversalVr;

[BepInPlugin("raicuparta.unityuniversalvr", "Unity Universal VR", "0.1.0")]
public class UnityUniversalVrMod : BaseUnityPlugin
{
    private bool _vrEnabled;
    private bool _setUpDone;
    private bool _previousIsKeyPressed;
    private bool _isKeyPressed;
    private Type _xrSettingsType;
    private PropertyInfo _loadedDeviceNameProperty;

    private void Awake()
    {
        _xrSettingsType =
            Type.GetType("UnityEngine.XR.XRSettings, UnityEngine.XRModule") ??
            Type.GetType("UnityEngine.XR.XRSettings, UnityEngine.VRModule");

        _loadedDeviceNameProperty = _xrSettingsType.GetProperty("loadedDeviceName");
    }

    private void Update()
    {
        if (GetKeyDown())
        {
            if (!_vrEnabled)
            {
                if (!_setUpDone)
                {
                    SetUpVr();
                }
                else
                {
                    SetVrEnabled(true);
                }
            }
            else
            {
                SetVrEnabled(false);
            }
        }

        if (!_setUpDone && _loadedDeviceNameProperty.GetValue(null, null) != "")
        {
            FinishSetUp();
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
    private static extern short GetKeyState(int keyCode);

    private bool GetKeyDown()
    {
        _previousIsKeyPressed = _isKeyPressed;
        _isKeyPressed = (((ushort)GetKeyState(0x72)) & 0x8000) != 0;

        return !_previousIsKeyPressed && _isKeyPressed;
    }
    
    private void SetUpVr()
    {
        Console.WriteLine("Toggling VR...");

        if (_xrSettingsType != null)
        {
            MethodInfo loadDeviceByNameMethod = _xrSettingsType.GetMethod("LoadDeviceByName", new[] { typeof(string) });
            if (loadDeviceByNameMethod != null)
            {
                object[] parameters = { "OpenVR" };
                loadDeviceByNameMethod.Invoke(null, parameters);
            }
            else
            {
                Console.WriteLine("Failed to get method LoadDeviceByName");
            }
        }
        else
        {
            Console.WriteLine("Failed to get type UnityEngine.XR.XRSettings");
        }
    }

    private void FinishSetUp()
    {
        SetVrEnabled(true);
        
        Type inputTrackingType = 
            Type.GetType("UnityEngine.XR.InputTracking, UnityEngine.XRModule") ??
            Type.GetType("UnityEngine.XR.InputTracking, UnityEngine.VRModule");

        if (inputTrackingType != null)
        {
            PropertyInfo disablePositionalTrackingProperty = inputTrackingType.GetProperty("disablePositionalTracking");
            if (disablePositionalTrackingProperty != null)
            {
                disablePositionalTrackingProperty.SetValue(null, true, null);
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

        _setUpDone = true;
    }

    private void SetVrEnabled(bool enabled)
    {
        Console.WriteLine($"Setting VR enable status to {enabled}");
        PropertyInfo enableVr = _xrSettingsType.GetProperty("enabled");
        if (enableVr != null)
        {
            enableVr.SetValue(null, enabled, null);
        }
        else
        {
            Console.WriteLine("Failed to get property enabled");
        }

        _vrEnabled = enabled;
    }
}
