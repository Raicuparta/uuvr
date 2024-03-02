#if CPP
using System;
#endif
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
    private int _uiLayer = -1;
    
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

        _uiLayer = FindFreeLayer();
    }

    // Unity only lets you define 32 layers.
    // This is annoying because it's useful for us to create layers for some VR-specific stuff.
    // We try to find a free layer (one without a name), but some games use all 32 layers.
    // In that case, we need to fall back to something else (TODO: fall back to user defined layer)
    private static int FindFreeLayer()
    {
        for (int layer = 31; layer >= 0; layer--)
        {
            if (LayerMask.LayerToName(layer).Length != 0) continue;

            Debug.Log($"Found free layer: {layer}");
            return layer;
        }

        Debug.LogWarning("Failed to find a free layer to use for VR UI. Falling back to last layer.");
        return 31;
    }

    private void SetUpUi()
    {
        // In some cases it might be useful to base the render texture dimensions on HMD,
        // but I'm guessing that's rare.
        // _uiTexture = new RenderTexture(XRSettings.eyeTextureWidth, XRSettings.eyeTextureHeight, 16, RenderTextureFormat.ARGB32);
        _uiTexture = new RenderTexture(Screen.width, Screen.height, 16, RenderTextureFormat.ARGB32);
        float uiTextureAspectRatio =  (float) _uiTexture.height / _uiTexture.width;

        _uiCaptureCamera = new GameObject("VrUiCaptureCamera").AddComponent<Camera>();
        VrCamera.IgnoredCameras.Add(_uiCaptureCamera);
        _uiCaptureCamera.transform.parent = transform;
        _uiCaptureCamera.clearFlags = CameraClearFlags.SolidColor;
        _uiCaptureCamera.backgroundColor = Color.clear;
        _uiCaptureCamera.targetTexture = _uiTexture;
        _uiCaptureCamera.depth = 100;
        _uiCaptureCamera.cullingMask = 1 << _uiLayer;
        
        // Dumb solution to avoid the scene camera from seeing the capture camera.
        // Should use layers instead but dunno maybe not.
        _uiCaptureCamera.transform.localPosition = Vector3.right * 1000;

        _uiSceneCamera = new GameObject("VrUiSceneCamera").AddComponent<Camera>();
        VrCamera.IgnoredCameras.Add(_uiSceneCamera);
        _uiSceneCamera.transform.parent = transform;
        _uiSceneCamera.clearFlags = CameraClearFlags.Nothing;
        _uiSceneCamera.depth = 100;
        _uiSceneCamera.cullingMask = 1 << _uiLayer;
        _uiSceneCamera.gameObject.AddComponent<UuvrPoseDriver>();

        GameObject vrUiQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        vrUiQuad.name = "VrUiQuad";
        vrUiQuad.layer = _uiLayer;
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
        
        IEnumerable keys = (IEnumerable)_graphicRegistryKeysProperty.GetValue(_graphicRegistryGraphics, null);
        foreach (Canvas canvas in keys)
        {
            if (!canvas) continue;
            
            // World space canvases probably already work as intended in VR.
            if (canvas.renderMode == RenderMode.WorldSpace) continue;
        
            // Screen space canvases are probably already working as intended in VR.
            if (canvas.renderMode == RenderMode.ScreenSpaceCamera) continue;
            
            // TODO: option to skip the above only if it's rendering to a texture, or only if not rendering to stereo.
            // if (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera?.targetTexture != null) continue;

            // No need to look at child canvases, just change the parents.
            if (!canvas.isRootCanvas) continue;

            if (_ignoredCanvases.Any(ignoredCanvas => canvas.name.ToLower().Contains(ignoredCanvas.ToLower())))
            {
                continue;
            }

            Debug.Log($"Found canvas to convert to VR: {canvas.name}");
            
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = _uiCaptureCamera;
            SetLayer(canvas.transform, _uiLayer);
        }
    }

    private void SetLayer(Transform parent, int layer)
    {
        foreach (Transform child in parent)
        {
            SetLayer(child, layer);
            parent.gameObject.layer = layer;
        }
    }
}
