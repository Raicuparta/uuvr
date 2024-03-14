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
    private float _originalFixedDeltaTime = -1;
    
    private VrUiManager? _vrUi;
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
        
        Create();
    }

    private void Start()
    {
        Type? xrDeviceType = Type.GetType("UnityEngine.XR.XRDevice, UnityEngine.XRModule") ??
                             Type.GetType("UnityEngine.XR.XRDevice, UnityEngine.VRModule") ??
                             Type.GetType("UnityEngine.VR.VRDevice, UnityEngine.VRModule") ??
                             Type.GetType("UnityEngine.VR.VRDevice, UnityEngine");

        _refreshRateProperty = xrDeviceType.GetProperty("refreshRate");
        
        _vrUi = UuvrBehaviour.Create<VrUiManager>(transform);
        
        VrToggle.SetVrEnabled(false);
        SetPositionTrackingEnabled(false);
    }

    private void Update()
    {
        if (_toggleVrKey.UpdateIsDown()) VrToggle.ToggleVr();
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

    private void SetPositionTrackingEnabled(bool enabled)
    {
        Type inputTrackingType = 
            Type.GetType("UnityEngine.XR.InputTracking, UnityEngine.XRModule") ??
            Type.GetType("UnityEngine.XR.InputTracking, UnityEngine.VRModule") ??
            Type.GetType("UnityEngine.VR.InputTracking, UnityEngine.VRModule") ??
            Type.GetType("UnityEngine.VR.InputTracking, UnityEngine");

        if (inputTrackingType != null)
        {
            PropertyInfo disablePositionalTrackingProperty = inputTrackingType.GetProperty("disablePositionalTracking");
            if (disablePositionalTrackingProperty != null)
            {
                disablePositionalTrackingProperty.SetValue(null, !enabled, null);
            }
            else
            {
                Debug.LogWarning("Failed to get property disablePositionalTracking");
            }
        }
        else
        {
            Debug.LogWarning("Failed to get type UnityEngine.XR.InputTracking");
        }
    }
}
