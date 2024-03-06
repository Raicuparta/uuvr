﻿using System;
using UnityEngine;

namespace Uuvr;

public class VrUiCanvas: UuvrBehaviour
{
#if CPP
    public VrUiCanvas(System.IntPtr pointer) : base(pointer)
    {
    }
#endif

    private Canvas _canvas;
    private Camera _uiCaptureCamera;
    private bool _isPatched;
    private RenderMode _originalRenderMode;
    private Camera _originalWorldCamera;
    private int _originalLayer;

    public static void Create(Canvas _canvas, Camera uiCaptureCamera)
    {
        VrUiCanvas instance = _canvas.gameObject.AddComponent<VrUiCanvas>();
        instance._canvas = _canvas;
        instance._uiCaptureCamera = uiCaptureCamera;
    }

    private void Start()
    {
        OnSettingChanged();
    }

    protected override void OnSettingChanged()
    {
        bool shouldPatch = ShouldPatchScreenSpaceCanvas() && ModConfiguration.Instance.PatchUi.Value;
        
        if (shouldPatch && !_isPatched)
        {
            Patch();
        }
        else if (!shouldPatch && _isPatched)
        {
            UndoPatch();
        }
    }

    private bool ShouldPatchScreenSpaceCanvas()
    {
        bool isScreenSpace = _originalRenderMode == RenderMode.ScreenSpaceCamera;

        return ModConfiguration.Instance.ScreenSpaceCanvasTypesToPatch.Value switch
        {
            ModConfiguration.ScreenSpaceCanvasType.None => !isScreenSpace,
            ModConfiguration.ScreenSpaceCanvasType.NotToTexture => !isScreenSpace || isScreenSpace && _canvas.worldCamera?.targetTexture == null,
            ModConfiguration.ScreenSpaceCanvasType.All => true,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private void Patch()
    {
        LayerHelper.SetLayerRecursive(transform, LayerHelper.GetVrUiLayer());
        
        _originalRenderMode = _canvas.renderMode;
        _originalWorldCamera = _canvas.worldCamera;
        _originalLayer = _canvas.gameObject.layer;
        
        _canvas.renderMode = RenderMode.ScreenSpaceCamera;
        _canvas.worldCamera = _uiCaptureCamera;
        _isPatched = true;
    }

    private void UndoPatch()
    {
        // This is making the assumption that all children of the canvas were in the same layer,
        // which isn't a very smart assumption. TODO: don't make an ass out of u and mption.
        // I guess I'll need to store a reference to every child so I can reset them afterwards :(
        LayerHelper.SetLayerRecursive(transform, _originalLayer);

        _canvas.renderMode = _originalRenderMode;
        _canvas.worldCamera = _originalWorldCamera;
        _isPatched = false;
    }
}
