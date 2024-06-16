using System;
using UnityEngine;

namespace Uuvr.VrUi.PatchModes;

public class CanvasRedirect: UuvrBehaviour
{
#if CPP
    public CanvasRedirect(System.IntPtr pointer) : base(pointer)
    {
    }
#endif

    private Canvas _canvas;
    private Camera _uiCaptureCamera;
    private bool _isPatched;
    private RenderMode _originalRenderMode;
    private Camera _originalWorldCamera;
    private float _originalPlaneDistance;
    private int _originalLayer;

    public static void Create(Canvas _canvas, Camera uiCaptureCamera)
    {
        var instance = _canvas.gameObject.AddComponent<CanvasRedirect>();
        instance._canvas = _canvas;
        instance._uiCaptureCamera = uiCaptureCamera;
    }

    private void Start()
    {
        OnSettingChanged();
    }

    protected override void OnSettingChanged()
    {
        var shouldPatch = ShouldPatchCanvas();
        
        if (shouldPatch && !_isPatched)
        {
            Patch();
        }
        else if (!shouldPatch && _isPatched)
        {
            UndoPatch();
        }
    }

    private bool ShouldPatchCanvas()
    {
        if (ModConfiguration.Instance.PreferredUiPatchMode.Value != ModConfiguration.UiPatchMode.CanvasRedirect)
        {
            return false;
        }

        var isScreenSpace = _originalRenderMode == RenderMode.ScreenSpaceCamera;

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
        _originalPlaneDistance = _canvas.planeDistance;
        _originalLayer = _canvas.gameObject.layer;
        
        _canvas.renderMode = RenderMode.ScreenSpaceCamera;
        _canvas.worldCamera = _uiCaptureCamera;

        if (_originalRenderMode == RenderMode.ScreenSpaceCamera)
        {
            // If the canvas was rendering to another camera,
            // the original plane distance might not fit inside our UI camera frustum.
            // I'd rather not change the plane distance since it might be important for sorting.
            // So I'll change our UI camera near/far to make sure it can see all these canvases.
            if (_originalPlaneDistance < _uiCaptureCamera.nearClipPlane)
            {
                _uiCaptureCamera.nearClipPlane = Mathf.Max(0.01f, _originalPlaneDistance - 0.1f);
            }
            if (_originalPlaneDistance > _uiCaptureCamera.farClipPlane)
            {
                _uiCaptureCamera.farClipPlane = _originalPlaneDistance + 0.1f;
            }
        }
        else
        {
            // Doesn't really make sense to enter this,
            // but if we do, change the distance to something.
            _canvas.planeDistance = 1f;
        }

        _isPatched = true;
        
        // TODO: if using alternative ui patching for overlays, have option to resize camera mode canvases.
    }

    private void UndoPatch()
    {
        // This is making the assumption that all children of the canvas were in the same layer,
        // which isn't a very smart assumption. TODO: don't make an ass out of u and mption.
        // I guess I'll need to store a reference to every child so I can reset them afterwards :(
        LayerHelper.SetLayerRecursive(transform, _originalLayer);

        _canvas.renderMode = _originalRenderMode;
        _canvas.worldCamera = _originalWorldCamera;
        _canvas.planeDistance = _originalPlaneDistance;
        _isPatched = false;
    }
}
