using System;
using System.Reflection;
using UnityEngine;
using Uuvr.VrCamera;
using Uuvr.VrTogglers;
using Uuvr.VrUi;

namespace Uuvr;

public class UuvrCore: MonoBehaviour
{
#if CPP
    public UuvrCore(IntPtr pointer) : base(pointer)
    {
    }
#endif

    private readonly KeyboardKey _toggleVrKey = new (KeyboardKey.KeyCode.F3);
    private float _originalFixedDeltaTime;
    
    private VrUiManager? _vrUi;
    private ThingDisabler? _thingDisabler;
    private PropertyInfo? _refreshRateProperty;
    private VrTogglerManager? _vrTogglerManager;

    public static void Create()
    {
        new GameObject("UUVR").AddComponent<UuvrCore>();
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        gameObject.AddComponent<VrCameraManager>();
        
        // TODO: Emulate input.   
        // UuvrBehaviour.Create<UuvrInput>(transform);
    }

    private void OnDestroy()
    {
        Debug.Log("UUVR has been destroyed. This shouldn't have happened. Recreating...");
        
        Create();
    }

    private void Start()
    {
        var xrDeviceType = Type.GetType("UnityEngine.XR.XRDevice, UnityEngine.XRModule") ??
                           Type.GetType("UnityEngine.XR.XRDevice, UnityEngine.VRModule") ??
                           Type.GetType("UnityEngine.VR.VRDevice, UnityEngine.VRModule") ??
                           Type.GetType("UnityEngine.VR.VRDevice, UnityEngine");

        _refreshRateProperty = xrDeviceType?.GetProperty("refreshRate");
        
        _vrUi = UuvrBehaviour.Create<VrUiManager>(transform);
        _thingDisabler = UuvrBehaviour.Create<ThingDisabler>(transform);

        _vrTogglerManager = new VrTogglerManager();

        SetPositionTrackingEnabled(false);
    }

    private void Update()
    {
        if (_toggleVrKey.UpdateIsDown()) _vrTogglerManager?.ToggleVr();
        UpdatePhysicsRate();
    }

    private void UpdatePhysicsRate()
    {
        if (_originalFixedDeltaTime == 0)
        {
            _originalFixedDeltaTime = Time.fixedDeltaTime;
        }

        if (_refreshRateProperty == null) return;

        var headsetRefreshRate = (float)_refreshRateProperty.GetValue(null, null);
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

    private static void SetPositionTrackingEnabled(bool positionTrackingEnabled)
    {
        var inputTrackingType = 
            Type.GetType("UnityEngine.XR.InputTracking, UnityEngine.XRModule") ??
            Type.GetType("UnityEngine.XR.InputTracking, UnityEngine.VRModule") ??
            Type.GetType("UnityEngine.VR.InputTracking, UnityEngine.VRModule") ??
            Type.GetType("UnityEngine.VR.InputTracking, UnityEngine");

        if (inputTrackingType != null)
        {
            var disablePositionalTrackingProperty = inputTrackingType.GetProperty("disablePositionalTracking");
            if (disablePositionalTrackingProperty != null)
            {
                disablePositionalTrackingProperty.SetValue(null, !positionTrackingEnabled, null);
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
