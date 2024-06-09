using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Uuvr.VrUi.PatchModes;

public class CanvasRedirectPatchMode : VrUiPatchMode
{
#if CPP
    public VrUiCanvasPatcher(System.IntPtr pointer) : base(pointer)
    {
    }
#endif

    private readonly List<string> _ignoredCanvases = new()
    {
        // Unity Explorer canvas, don't want it to be affected by VR.
        "unityexplorer",

        // Also Unity Explorer stuff, or anything else that depends on UniverseLib,
        // but really just Unity Explorer.
        "universelib",
    };
    
    private Camera? _uiCaptureCamera;

    protected override void OnSettingChanged()
    {
        base.OnSettingChanged();
        _uiCaptureCamera.cullingMask = 1 << LayerHelper.GetVrUiLayer();
    }

    private void Awake()
    {
        _uiCaptureCamera = new GameObject("VrUiCaptureCamera").AddComponent<Camera>();
        VrCamera.VrCamera.IgnoredCameras.Add(_uiCaptureCamera);
        _uiCaptureCamera.transform.parent = transform;
        _uiCaptureCamera.clearFlags = CameraClearFlags.SolidColor;
        _uiCaptureCamera.backgroundColor = Color.clear;
        _uiCaptureCamera.depth = 100;
    }

    public override void SetUpTargetTexture(RenderTexture targetTexture)
    {
        _uiCaptureCamera.targetTexture = targetTexture;
    }

    private void Update()
    {
        // TODO: handle finding canvases that aren't here because they don't have anything inside them.
        // Game example: Smushi.
        foreach (var canvas in GraphicRegistry.instance.m_Graphics.Keys)
        {
            PatchCanvas(canvas);
        }
    }

    private void PatchCanvas(Canvas canvas)
    {
        if (canvas == null || _uiCaptureCamera == null) return;

        // World space canvases probably already work as intended in VR.
        if (canvas.renderMode == RenderMode.WorldSpace) return;

        // No need to look at child canvases, just change the parents.
        // Also changing some properties of children affects the parents, which makes it harder for us to know what we're doing.
        if (!canvas.isRootCanvas)
        {
            // TODO: seems like this is inefficient, really need to find a better way to get all canvases.
            PatchCanvas(canvas.rootCanvas);
            return;
        }

        if (_ignoredCanvases.Any(ignoredCanvas => canvas.name.ToLower().Contains(ignoredCanvas.ToLower())))
        {
            return;
        }

        // Already patched;
        // TODO: might be smart to have a more efficient way to check if it's patched.
        if (canvas.GetComponent<CanvasRedirect>()) return;

        CanvasRedirect.Create(canvas, _uiCaptureCamera);
    }
}
