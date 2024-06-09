#if MODERN
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Uuvr.VrCamera;

// Helper behaviour for handling URP's Additional Camera Data without needing an actual dependency.
// Not using this for HDRP for now, since it isn't much help.
// HDRP does't seem to support camera stacks, and that's the main thing I'm using this for.
public class AdditionalCameraData: MonoBehaviour
{
#if CPP
    public AdditionalCameraData(System.IntPtr pointer) : base(pointer)
    {
    }
#endif

    private const int RenderTypeBase = 0;
    private const int RenderTypeOverlay = 1;

    private static Type? _additionalCameraDataType;
    private static PropertyInfo? _renderTypeProperty;
    private static PropertyInfo? _cameraStackProperty;
    private static PropertyInfo? _allowXrRenderingProperty;

    private object? _additionalCameraData;

    public static AdditionalCameraData Create(Camera camera)
    {
        if (_additionalCameraDataType == null)
        {
            _additionalCameraDataType = Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
            _renderTypeProperty = _additionalCameraDataType?.GetProperty("renderType");
            _cameraStackProperty = _additionalCameraDataType?.GetProperty("cameraStack");
            _allowXrRenderingProperty = _additionalCameraDataType?.GetProperty("allowXRRendering");
        }

        if (_additionalCameraDataType == null) return null;

        return camera.gameObject.GetComponent<AdditionalCameraData>() ?? camera.gameObject.AddComponent<AdditionalCameraData>();
    }
    
    private void Awake()
    {
        _additionalCameraData = gameObject.GetComponent(_additionalCameraDataType) ?? gameObject.AddComponent(_additionalCameraDataType);
    }

    public void SetRenderTypeBase()
    {
        _renderTypeProperty?.SetValue(_additionalCameraData, RenderTypeBase);
    }

    public void SetRenderTypeOverlay()
    {
        _renderTypeProperty?.SetValue(_additionalCameraData, RenderTypeOverlay);
    }

    public bool IsOverlay()
    {
        return (int) _renderTypeProperty.GetValue(_additionalCameraData) == RenderTypeOverlay;
    }

    public List<Camera> GetCameraStack()
    {
        return (List<Camera>) _cameraStackProperty.GetValue(_additionalCameraData);
    }

    public void SetAllowXrRendering(bool allowXrRendering)
    {
        _allowXrRenderingProperty.SetValue(_additionalCameraData, allowXrRendering);    
    }
}
#endif