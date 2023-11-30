using System;
using System.Reflection;
using BepInEx;

namespace UnityUniversalVr;

[BepInPlugin("raicuparta.unityuniversalvr", "Unity Universal VR", "0.1.0")]
public class UnityUniversalVrMod : BaseUnityPlugin
{
    private void Awake()
    {
        Type inputTrackingType = Type.GetType("UnityEngine.XR.InputTracking, UnityEngine.XRModule");
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
    }

    // TODO Unusued for now
    private static void ToggleVr()
    {
        Console.WriteLine("Toggling VR...");
        
        Type xrSettingsType = Type.GetType("UnityEngine.XR.XRSettings, UnityEngine.XRModule");
        if (xrSettingsType != null)
        {
            MethodInfo loadDeviceByNameMethod = xrSettingsType.GetMethod("LoadDeviceByName");
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
}
