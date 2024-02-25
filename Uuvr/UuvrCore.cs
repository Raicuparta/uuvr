using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;
using UnityEngine.UI.Collections;
using UnityEngine.XR;
using UnityEngine.XR.Management;

namespace Uuvr;

public class UuvrCore: MonoBehaviour
{
    private readonly KeyboardKey _toggleVrKey = new (KeyboardKey.KeyCode.F3);
    private readonly KeyboardKey _reparentCameraKey = new (KeyboardKey.KeyCode.F4);
    private readonly KeyboardKey _vrUiKey = new (KeyboardKey.KeyCode.F5);
    private readonly List<string> _ignoredCanvases = new()
    {
        // Unity Explorer canvas, don't want it to be affected by VR.
        "unityexplorer",
        "universelib",
    };
    
    private Type _xrSettingsType;
    private PropertyInfo _xrEnabledProperty;
    private bool _shouldPatchUi;
    private object _graphicRegistryGraphics;
    private PropertyInfo _graphicRegistryKeysProperty;
    private const string VR_UI_PARENT_NAME = "UUVR_UI_PARENT";
    private static Camera _uiCaptureCamera;
    private static Camera _uiSceneCamera;
    private RenderTexture _uiTexture;
    
#if CPP
    public UuvrCore(IntPtr pointer) : base(pointer)
    {
    }
#endif

    private void Start()
    {
        _xrSettingsType =
            Type.GetType("UnityEngine.XR.XRSettings, UnityEngine.XRModule") ??
            Type.GetType("UnityEngine.XR.XRSettings, UnityEngine.VRModule") ??
            Type.GetType("UnityEngine.VR.VRSettings, UnityEngine");
        
        _xrEnabledProperty = _xrSettingsType.GetProperty("enabled");

        SetXrEnabled(false);
        SetPositionTrackingEnabled(false);

        _graphicRegistryGraphics = GraphicRegistry.instance.GetValue<object>("m_Graphics");
        _graphicRegistryKeysProperty = _graphicRegistryGraphics.GetType().GetProperty("Keys");
        
#if MODERN
        gameObject.AddComponent<ModXrManager>();
#endif
    }

    private void Update()
    {
        if (_toggleVrKey.UpdateIsDown()) ToggleXr();
        if (_reparentCameraKey.UpdateIsDown()) ReparentCamera();
        if (_vrUiKey.UpdateIsDown()) ToggleXrUi();

        UpdateXrUi();
    }

    private void SetUpUi()
    {
        // In some cases it might be useful to base the render texture dimensions on HMD,
        // but I'm guessing that's rare.
        // _uiTexture = new RenderTexture(XRSettings.eyeTextureWidth, XRSettings.eyeTextureHeight, 16, RenderTextureFormat.ARGB32);
        _uiTexture = new RenderTexture(Screen.width, Screen.height, 16, RenderTextureFormat.ARGB32);
        float uiTextureAspectRatio =  (float) _uiTexture.height / _uiTexture.width;

        _uiCaptureCamera = new GameObject("VrUiCaptureCamera").AddComponent<Camera>();
        _uiCaptureCamera.transform.parent = transform;
        _uiCaptureCamera.clearFlags = CameraClearFlags.SolidColor;
        _uiCaptureCamera.backgroundColor = Color.clear;
        _uiCaptureCamera.targetTexture = _uiTexture;
        _uiCaptureCamera.depth = 100;
        
        // Dumb solution to avoid the scene camera from seeing the capture camera.
        // Should use layers instead but dunno maybe not.
        _uiCaptureCamera.transform.localPosition = Vector3.right * 1000;

        _uiSceneCamera = new GameObject("VrUiSceneCamera").AddComponent<Camera>();
        _uiSceneCamera.transform.parent = transform;
        _uiSceneCamera.clearFlags = CameraClearFlags.Depth;
        _uiSceneCamera.depth = 100;

        GameObject vrUiQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        vrUiQuad.name = "VrUiQuad";
        vrUiQuad.transform.parent = transform;
        vrUiQuad.transform.localPosition = Vector3.forward * 2f;
        float quadWidth = 1.8f;
        float quadHeight = quadWidth * uiTextureAspectRatio;
        vrUiQuad.transform.localScale = new Vector3(quadWidth, quadHeight, 1f);
        vrUiQuad.GetComponent<Renderer>().material = Canvas.GetDefaultCanvasMaterial();
        vrUiQuad.GetComponent<Renderer>().material.mainTexture = _uiTexture;
    }
    
    private void ToggleXrUi()
    {
        bool xrEnabled = (bool) _xrEnabledProperty.GetValue(null, null);
        if (!xrEnabled)
        {
            Debug.LogWarning("Can't toggle VR UI while VR is disabled");
            return;
        }
        
        if (_uiTexture == null) SetUpUi();
        
        _shouldPatchUi = !_shouldPatchUi;
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

    private void UpdateXrUi()
    {
        if (!_shouldPatchUi) return;

        List<Canvas> canvases = new();
        
        IEnumerable keys = (IEnumerable)_graphicRegistryKeysProperty.GetValue(_graphicRegistryGraphics);
        foreach (Canvas canvas in keys)
        {
            if (!canvas) continue;
            
            // World space canvases probably already work as intended in VR.
            if (canvas.renderMode == RenderMode.WorldSpace) continue;
        
            // Screen space canvases are probably already working as intended in VR.
            if (canvas.renderMode == RenderMode.ScreenSpaceCamera) continue;

            // No need to look at child canvases, just change the parents.
            if (!canvas.isRootCanvas) continue;

            if (_ignoredCanvases.Any(ignoredCanvas => canvas.name.ToLower().Contains(ignoredCanvas.ToLower())))
            {
                continue;
            }

            Debug.Log($"Found canvas to convert to VR: {canvas.name}");
            
            canvases.Add(canvas);
        }
        
        foreach (Canvas canvas in canvases)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = _uiCaptureCamera;
        }
    }
}
