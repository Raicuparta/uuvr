using System;
using System.Reflection;
using UnityEngine;

namespace Uuvr;

public class UuvrCore: MonoBehaviour
{
#if CPP
    public UuvrCore(IntPtr pointer) : base(pointer)
    {
    }
#endif

    private readonly KeyboardKey _toggleVrKey = new (KeyboardKey.KeyCode.F3);
    private readonly KeyboardKey _reparentCameraKey = new (KeyboardKey.KeyCode.F4);
    private readonly KeyboardKey _vrUiKey = new (KeyboardKey.KeyCode.F5);
    
    private Type? _xrSettingsType;
    private PropertyInfo? _xrEnabledProperty;
    private VrUi? _vrUi;
    
    public static void Create()
    {
        new GameObject("UUVR").AddComponent<UuvrCore>();
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        Debug.Log("UUVR has been destroyed. This shouldn't have happened. Recreating...");
        
        // TODO: make some (most?) stuff static so it survives recreation.
        Create();
    }

    private void Start()
    {
        _xrSettingsType =
            Type.GetType("UnityEngine.XR.XRSettings, UnityEngine.XRModule") ??
            Type.GetType("UnityEngine.XR.XRSettings, UnityEngine.VRModule") ??
            Type.GetType("UnityEngine.VR.VRSettings, UnityEngine");
        
        _xrEnabledProperty = _xrSettingsType.GetProperty("enabled");

        _vrUi = UuvrBehaviour.Create<VrUi>(transform);
        
        SetXrEnabled(false);
        SetPositionTrackingEnabled(false);
        
#if MODERN
        gameObject.AddComponent<ModXrManager>();
#endif
    }

    private void Update()
    {
        if (_toggleVrKey.UpdateIsDown()) ToggleXr();
        if (_reparentCameraKey.UpdateIsDown()) ReparentCamera();
        if (_vrUiKey.UpdateIsDown()) ToggleXrUi();
    }
    
    private void ToggleXrUi()
    {
        bool xrEnabled = (bool) _xrEnabledProperty.GetValue(null, null);
        if (!xrEnabled)
        {
            Debug.LogWarning("Can't toggle VR UI while VR is disabled.");
            return;
        }

        if (!_vrUi)
        {
            Debug.LogWarning("Can't toggle VR UI because VR UI component doesn't exist.");
            return;
        }
        
        _vrUi.enabled = !_vrUi.enabled;
    }

    private void ToggleXr()
    {
        bool xrEnabled = (bool) _xrEnabledProperty.GetValue(null, null);
        SetXrEnabled(!xrEnabled);
    }
    
    private void ReparentCamera() {
        Console.WriteLine("Reparenting Camera...");

        Camera mainCamera = Camera.main ?? Camera.current;
        mainCamera.enabled = false;

        GameObject vrCameraObject = new("VrCamera");
        Camera vrCamera = vrCameraObject.AddComponent<Camera>();
        vrCamera.tag = "MainCamera";
        vrCamera.transform.parent = mainCamera.transform;
        vrCamera.transform.localPosition = Vector3.zero;
    }

    private void SetXrEnabled(bool enabled)
    {
        Console.WriteLine($"Setting XR enabled to {enabled}");

        _xrEnabledProperty.SetValue(null, enabled, null);
        
        // TODO verify if exists etc.
        try
        {

            if (enabled)
            {
                Camera.main.gameObject.AddComponent<VrCamera>();
            }
            else
            {
                Destroy(Camera.main.gameObject.GetComponent<VrCamera>());
            }
        } catch
        {
            
        }
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
