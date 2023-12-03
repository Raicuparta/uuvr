using System;
using System.Reflection;
using System.Runtime.InteropServices;
using BepInEx;

namespace UnityUniversalVr;

[BepInPlugin("raicuparta.unityuniversalvr", "Unity Universal VR", "0.1.0")]
public class UnityUniversalVrMod : BaseUnityPlugin
{
    private readonly KeyboardKey _toggleVrKey = new (KeyboardKey.KeyCode.F3);
    private readonly KeyboardKey _reparentCameraKey = new (KeyboardKey.KeyCode.F4);

    private bool _vrEnabled;
    private bool _setUpDone;
    private Type _xrSettingsType;
    private PropertyInfo _loadedDeviceNameProperty;
    private object _mainCameraRenderTexture;

    private void Awake()
    {
        _xrSettingsType =
            Type.GetType("UnityEngine.XR.XRSettings, UnityEngine.XRModule") ??
            Type.GetType("UnityEngine.XR.XRSettings, UnityEngine.VRModule");

        _loadedDeviceNameProperty = _xrSettingsType.GetProperty("loadedDeviceName");
    }

    private void Update()
    {
        if (_toggleVrKey.UpdateIsDown())
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

        if (_reparentCameraKey.UpdateIsDown())
        {
            ReparentCamera();
        }

        if (!_setUpDone && _loadedDeviceNameProperty.GetValue(null, null) != "")
        {
            FinishSetUp();
        }
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

        if (enabled)
        {
            DisableRenderTexture();
        }
        else
        {
            EnableRenderTexture();
        }

        _vrEnabled = enabled;
    }

    private void DisableRenderTexture()
    {
        Type cameraType = Type.GetType("UnityEngine.Camera, UnityEngine.CoreModule");
        object mainCamera = cameraType.GetProperty("main").GetValue(null, null);

        if (mainCamera == null)
        {
            Console.WriteLine("Failed to find main camera");
            return;
        }

        _mainCameraRenderTexture = cameraType.GetProperty("targetTexture").GetValue(mainCamera, null);
        
        cameraType.GetProperty("targetTexture").SetValue(mainCamera, null, null);
    }

    private void EnableRenderTexture()
    {
        Type cameraType = Type.GetType("UnityEngine.Camera, UnityEngine.CoreModule");
        object mainCamera = cameraType.GetProperty("main").GetValue(null, null);

        if (mainCamera == null)
        {
            Console.WriteLine("Failed to find main camera");
            return;
        }
        
        cameraType.GetProperty("targetTexture").SetValue(mainCamera, _mainCameraRenderTexture, null);
    }
    
    private static void ReparentCamera() {

        Console.WriteLine("Reparenting Camera...");

        Type cameraType = Type.GetType("UnityEngine.Camera, UnityEngine.CoreModule");
        object mainCamera = cameraType.GetProperty("main").GetValue(null, null);
        cameraType.GetProperty("enabled").SetValue(mainCamera, false, null);

        Type gameObjectType = Type.GetType("UnityEngine.GameObject, UnityEngine.CoreModule");
        object vrCameraObject = Activator.CreateInstance(gameObjectType);
        MethodInfo addComponentMethod = gameObjectType.GetMethod("AddComponent", new[] { typeof(Type) });
        object vrCamera = addComponentMethod.Invoke(vrCameraObject, new[] { cameraType });
        object mainCameraTransform = cameraType.GetProperty("transform").GetValue(mainCamera, null);
        object vrCameraTransform = cameraType.GetProperty("transform").GetValue(vrCamera, null);
        Type transformType = Type.GetType("UnityEngine.Transform, UnityEngine.CoreModule");
        
        transformType.GetProperty("parent").SetValue(vrCameraTransform, mainCameraTransform, null);
        transformType.GetProperty("localPosition").SetValue(vrCameraTransform, null, null);
    }
}
