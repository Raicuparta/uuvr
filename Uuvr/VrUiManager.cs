using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Vector3 = UnityEngine.Vector3;

namespace Uuvr;

public class VrUiManager: UuvrBehaviour
{
#if CPP
    public VrUi(System.IntPtr pointer) : base(pointer)
    {
    }
#endif
    
    private static Camera? _uiCaptureCamera;
    private static Camera? _uiSceneCamera;
    private RenderTexture? _uiTexture;
    private GameObject? _vrUiQuad;
    
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
        SetUpUi();
        OnSettingChanged();
    }

    protected override void OnSettingChanged()
    {
        base.OnSettingChanged();
        int uiLayer = LayerHelper.GetVrUiLayer();
        
        _uiCaptureCamera.cullingMask = 1 << uiLayer;
        _uiSceneCamera.cullingMask = 1 << uiLayer;
        _vrUiQuad.layer = uiLayer;
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
        
        GameObject uiScene = new("VrUiScene")
        {
            transform =
            {
                parent = transform,

                // Dumb solution to avoid the scene camera from seeing the capture camera.
                // Without having to waste another layer.
                localPosition = Vector3.right * 1000
            }
        };

        // TODO: use Overlay camera type in URP and HDRP
        _uiSceneCamera = Create<UuvrPoseDriver>(uiScene.transform).gameObject.AddComponent<Camera>();
        VrCamera.IgnoredCameras.Add(_uiSceneCamera);
        _uiSceneCamera.clearFlags = CameraClearFlags.Depth;
        _uiSceneCamera.depth = 100;

        _vrUiQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        _vrUiQuad.name = "VrUiQuad";
        _vrUiQuad.transform.parent = uiScene.transform;
        _vrUiQuad.transform.localPosition = Vector3.forward * 2f;
        float quadWidth = 1.8f;
        float quadHeight = quadWidth * uiTextureAspectRatio;
        _vrUiQuad.transform.localScale = new Vector3(quadWidth, quadHeight, 1f);

        Renderer renderer = _vrUiQuad.GetComponent<Renderer>();
        renderer.material = Canvas.GetDefaultCanvasMaterial();
        renderer.material.mainTexture = _uiTexture;
    }

    private void Update()
    {
        if (_uiTexture == null) SetUpUi();
        
        foreach (Canvas canvas in GraphicRegistry.instance.m_Graphics.Keys)
        {
            PatchCanvas(canvas);
        }
    }

    private void PatchCanvas(Canvas canvas)
    {
        if (!canvas) return;

        // Already patched;
        if (canvas.worldCamera == _uiCaptureCamera) return;
        
        // World space canvases probably already work as intended in VR.
        if (canvas.renderMode == RenderMode.WorldSpace) return;
        
        // Screen space canvases are probably already working as intended in VR.
        // if (canvas.renderMode == RenderMode.ScreenSpaceCamera) return;
            
        // TODO: option to skip the above only if it's rendering to a texture, or only if not rendering to stereo.
        // if (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera?.targetTexture != null) return;

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

        Debug.Log($"Found canvas to convert to VR: {canvas.name}");

        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = _uiCaptureCamera;
        canvas.gameObject.AddComponent<VrUiCanvas>();
    }
}
