using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace Uuvr;

public class VrUi: UuvrBehaviour
{
#if CPP
    public VrUi(System.IntPtr pointer) : base(pointer)
    {
    }
#endif
    
    private object? _graphicRegistryGraphics;
    private PropertyInfo? _graphicRegistryKeysProperty;
    private static Camera? _uiCaptureCamera;
    private static Camera? _uiSceneCamera;
    private RenderTexture? _uiTexture;
    private int _uiSceneLayer = -1;
    
    private readonly KeyboardKey _vrUiKey = new (KeyboardKey.KeyCode.F5);
    private readonly List<string> _ignoredCanvases = new()
    {
        // Unity Explorer canvas, don't want it to be affected by VR.
        "unityexplorer",

        // Also Unity Explorer stuff, or anything else that depends on UniverseLib,
        // but really just Unity Explorer.
        "universelib",
    };

    private void Start()
    {
        _graphicRegistryGraphics = GraphicRegistry.instance.GetValue<object>("m_Graphics");
        _graphicRegistryKeysProperty = _graphicRegistryGraphics.GetType().GetProperty("Keys");

        SetUpLayer();
    }

    private void SetUpLayer()
    {
        for (int layer = 0; layer < 32; layer++)
        {
            if (LayerMask.LayerToName(layer).Length == 0)
            {
                Debug.Log($"Found free layer for VR UI: {layer}");
                _uiSceneLayer = layer;
            }
        }

        if (_uiSceneLayer != -1) return;

        Debug.LogWarning("Failed to find a free layer to use for VR UI. Falling back to last layer.");
        _uiSceneLayer = 31;
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
        _uiSceneCamera.cullingMask = 1 << _uiSceneLayer;

        GameObject vrUiQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        vrUiQuad.name = "VrUiQuad";
        vrUiQuad.layer = _uiSceneLayer;
        vrUiQuad.transform.parent = transform;
        vrUiQuad.transform.localPosition = Vector3.forward * 2f;
        float quadWidth = 1.8f;
        float quadHeight = quadWidth * uiTextureAspectRatio;
        vrUiQuad.transform.localScale = new Vector3(quadWidth, quadHeight, 1f);
        vrUiQuad.GetComponent<Renderer>().material = Canvas.GetDefaultCanvasMaterial();
        vrUiQuad.GetComponent<Renderer>().material.mainTexture = _uiTexture;
    }

    private void Update()
    {
        if (_uiTexture == null) SetUpUi();

        List<Canvas> canvases = new();
        
        IEnumerable keys = (IEnumerable)_graphicRegistryKeysProperty.GetValue(_graphicRegistryGraphics, null);
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
