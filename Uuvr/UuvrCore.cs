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
    private float _originalFixedDeltaTime = -1;
    
    private VrUi? _vrUi;
    private PropertyInfo? _refreshRateProperty;

    public static void Create()
    {
        new GameObject("UUVR").AddComponent<UuvrCore>();
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        gameObject.AddComponent<VrCameraManager>();
    }

    private void OnDestroy()
    {
        Debug.Log("UUVR has been destroyed. This shouldn't have happened. Recreating...");
        
        // TODO: make some (most?) stuff static so it survives recreation.
        Create();
    }

    private void Start()
    {
        Type? xrDeviceType = Type.GetType("UnityEngine.XR.XRDevice, UnityEngine.XRModule") ??
                             Type.GetType("UnityEngine.XR.XRDevice, UnityEngine.VRModule") ??
                             Type.GetType("UnityEngine.VR.VRDevice, UnityEngine.VRModule") ??
                             Type.GetType("UnityEngine.VR.VRDevice, UnityEngine");

        _refreshRateProperty = xrDeviceType.GetProperty("refreshRate");
        
        _vrUi = UuvrBehaviour.Create<VrUi>(transform);
        _vrUi.enabled = false;
        
        VrToggle.SetVrEnabled(false);
        SetPositionTrackingEnabled(false);
    }

    private void Update()
    {
        if (_toggleVrKey.UpdateIsDown()) VrToggle.ToggleVr();
        if (_reparentCameraKey.UpdateIsDown()) ReparentCamera();
        if (_vrUiKey.UpdateIsDown()) ToggleVrUi();
        UpdatePhysicsRate();
    }

    private void UpdatePhysicsRate()
    {
        if (_originalFixedDeltaTime == -1)
        {
            _originalFixedDeltaTime = Time.fixedDeltaTime;
        }

        if (_refreshRateProperty == null) return;

        float headsetRefreshRate = (float)_refreshRateProperty.GetValue(null, null);
        if (headsetRefreshRate <= 0) return;

        if (ModConfiguration.Instance.PhysicsMatchHeadsetRefreshRate.Value)
        {
            Time.fixedDeltaTime = 1f / (float) _refreshRateProperty.GetValue(null, null);
        }
        else
        {
            Time.fixedDeltaTime = _originalFixedDeltaTime;
        }
    }

    private void ToggleVrUi()
    {
        if (!VrToggle.IsVrEnabled)
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
    
    private void ReparentCamera() {
        Console.WriteLine("Reparenting Camera...");

        Camera mainCamera = Camera.main ?? Camera.current;
        mainCamera.enabled = false;

        GameObject vrCameraObject = new("VrCamera");
        Camera vrCamera = vrCameraObject.AddComponent<Camera>();
        VrCamera.IgnoredCameras.Add(vrCamera);
        vrCamera.tag = "MainCamera";
        vrCamera.transform.parent = mainCamera.transform;
        vrCamera.transform.localPosition = Vector3.zero;
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
