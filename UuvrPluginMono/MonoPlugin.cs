using System;
using System.Reflection;
using System.Runtime.InteropServices;
using BepInEx;

namespace UuvrPluginMono;

[BepInPlugin("raicuparta.unityuniversalvr", "Unity Universal VR", "0.1.0")]
public class MonoPlugin : BaseUnityPlugin
{
    private readonly KeyboardKey _toggleVrKey = new (KeyboardKey.KeyCode.F3);
    private readonly KeyboardKey _reparentCameraKey = new (KeyboardKey.KeyCode.F4);

    private bool _vrEnabled;
    private bool _setUpDone;
    private Type _xrSettingsType;
    private Type _cameraType;
    private Type _gameObjectType;
    private Type _transformType;
    private PropertyInfo _loadedDeviceNameProperty;
    private object _mainCameraRenderTexture;

    private void Awake()
    {
        _xrSettingsType =
            Type.GetType("UnityEngine.XR.XRSettings, UnityEngine.XRModule") ??
            Type.GetType("UnityEngine.XR.XRSettings, UnityEngine.VRModule") ??
            Type.GetType("UnityEngine.VR.VRSettings, UnityEngine");
        
        _cameraType = Type.GetType("UnityEngine.Camera, UnityEngine.CoreModule") ??
                      Type.GetType("UnityEngine.Camera, UnityEngine");

        _gameObjectType = Type.GetType("UnityEngine.GameObject, UnityEngine.CoreModule") ??
                          Type.GetType("UnityEngine.GameObject, UnityEngine");

        _loadedDeviceNameProperty = _xrSettingsType.GetProperty("loadedDeviceName");
        
        _transformType = Type.GetType("UnityEngine.Transform, UnityEngine.CoreModule") ??
                         Type.GetType("UnityEngine.Transform, UnityEngine");
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

        if (!_setUpDone && IsDeviceLoaded())
        {
            FinishSetUp();
        }
    }

    private bool IsDeviceLoaded()
    {
        return _loadedDeviceNameProperty?.GetValue(null, null) != null &&
               _loadedDeviceNameProperty.GetValue(null, null).ToString().Length > 0;
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
        try
        {
            object mainCamera = _cameraType.GetProperty("main").GetValue(null, null);

            if (mainCamera == null)
            {
                Console.WriteLine("Failed to find main camera");
                return;
            }

            _mainCameraRenderTexture = _cameraType.GetProperty("targetTexture").GetValue(mainCamera, null);

            _cameraType.GetProperty("targetTexture").SetValue(mainCamera, null, null);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to disable render texture: {e}");
        }
    }

    private void EnableRenderTexture()
    {
        try
        {
            object mainCamera = _cameraType.GetProperty("main").GetValue(null, null);

            if (mainCamera == null)
            {
                Console.WriteLine("Failed to find main camera");
                return;
            }

            _cameraType.GetProperty("targetTexture").SetValue(mainCamera, _mainCameraRenderTexture, null);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to enable render texture: {e}");
        }
    }
    
    private void ReparentCamera() {

        Console.WriteLine("Reparenting Camera...");

        object mainCamera = _cameraType.GetProperty("main").GetValue(null, null);
        _cameraType.GetProperty("enabled").SetValue(mainCamera, false, null);

        Console.WriteLine("Reparenting Camera 1");
        object vrCameraObject = Activator.CreateInstance(_gameObjectType);
        MethodInfo addComponentMethod = _gameObjectType.GetMethod("AddComponent", new[] { typeof(Type) });
        object vrCamera = addComponentMethod.Invoke(vrCameraObject, new[] { _cameraType });
        object mainCameraTransform = _cameraType.GetProperty("transform").GetValue(mainCamera, null);
        
        Console.WriteLine("Reparenting Camera 2");
        object vrCameraTransform = _cameraType.GetProperty("transform").GetValue(vrCamera, null);
        
        Console.WriteLine("Reparenting Camera 3");
        _transformType.GetProperty("parent").SetValue(vrCameraTransform, mainCameraTransform, null);
        _transformType.GetProperty("localPosition").SetValue(vrCameraTransform, null, null);
    }
}
