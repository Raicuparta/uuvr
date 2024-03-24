using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Vector3 = UnityEngine.Vector3;

namespace Uuvr;

public class VrUiManager: UuvrBehaviour
{
#if CPP
    public VrUiManager(System.IntPtr pointer) : base(pointer)
    {
    }
#endif
    
    private static Camera? _uiCaptureCamera;
    private static Camera? _uiSceneCamera;
    private RenderTexture? _uiTexture;
    private GameObject? _vrUiQuad;
    private GameObject? _uiScene;
    
    private readonly List<string> _ignoredCanvases = new()
    {
        // Unity Explorer canvas, don't want it to be affected by VR.
        "unityexplorer",

        // Also Unity Explorer stuff, or anything else that depends on UniverseLib,
        // but really just Unity Explorer.
        "universelib",
    };
    private AdditionalCameraData _additionalSceneCameraData;

    private void Start()
    {
        SetUpUi();
        OnSettingChanged();
        VrUiCursor.Create(transform);
    }

    protected override void OnSettingChanged()
    {
        base.OnSettingChanged();
        var uiLayer = LayerHelper.GetVrUiLayer();
        
        _uiCaptureCamera.cullingMask = 1 << uiLayer;
        _uiSceneCamera.cullingMask = 1 << uiLayer;
        _vrUiQuad.layer = uiLayer;
    }

    private void SetUpUi()
    {
        _uiTexture = new RenderTexture(Screen.width, Screen.height, 16, RenderTextureFormat.ARGB32);
        var uiTextureAspectRatio =  (float) _uiTexture.height / _uiTexture.width;

        _uiCaptureCamera = new GameObject("VrUiCaptureCamera").AddComponent<Camera>();
        VrCamera.IgnoredCameras.Add(_uiCaptureCamera);
        _uiCaptureCamera.transform.parent = transform;
        _uiCaptureCamera.clearFlags = CameraClearFlags.SolidColor;
        _uiCaptureCamera.backgroundColor = Color.clear;
        _uiCaptureCamera.targetTexture = _uiTexture;
        _uiCaptureCamera.depth = 100;
        
        
        _uiScene = new("VrUiScene")
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
        _uiSceneCamera = Create<UuvrPoseDriver>(_uiScene.transform).gameObject.AddComponent<Camera>();
        VrCamera.IgnoredCameras.Add(_uiSceneCamera);
        _uiSceneCamera.clearFlags = CameraClearFlags.Depth;
        _uiSceneCamera.depth = 100;
        
        _additionalSceneCameraData = AdditionalCameraData.Create(_uiSceneCamera);
        _additionalSceneCameraData.SetRenderTypeOverlay();
        
        var flatScreenView = FlatScreenView.Create(_uiScene.transform);
        flatScreenView.transform.localPosition = Vector3.forward * 2f;
        flatScreenView.transform.localRotation = Quaternion.identity;
        
        _vrUiQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        _vrUiQuad.name = "VrUiQuad";
        _vrUiQuad.transform.parent = _uiScene.transform;
        _vrUiQuad.transform.localPosition = Vector3.forward * 2f;
        var quadWidth = 1.8f;
        var quadHeight = quadWidth * uiTextureAspectRatio;
        _vrUiQuad.transform.localScale = new Vector3(quadWidth, quadHeight, 1f);

        var renderer = _vrUiQuad.GetComponent<Renderer>();
        renderer.material = Canvas.GetDefaultCanvasMaterial();
        renderer.material.mainTexture = _uiTexture;
        
        // TODO setting for this.
        // UniversalRenderPipelineAsset? pipelineAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        // var data = pipelineAsset.GetValue<ForwardRendererData>("scriptableRendererData");
        // data.opaqueLayerMask = -1;
        // data.transparentLayerMask = -1;
    }

    private void Update()
    {
        if (_uiTexture == null) SetUpUi();
        
        foreach (var canvas in GraphicRegistry.instance.m_Graphics.Keys)
        {
            PatchCanvas(canvas);
        }

        SetUpAdditionalCameraData();

        if (VrCamera.HighestDepthVrCamera != null && VrCamera.HighestDepthVrCamera.ParentCamera != null)
        {
            _uiScene.transform.SetParent(VrCamera.HighestDepthVrCamera.ParentCamera.transform, false);
            _uiScene.transform.localPosition = Vector3.zero;
            _uiScene.transform.localRotation = Quaternion.identity;
        }
    }

    private void SetUpAdditionalCameraData()
    {
        if (VrCamera.HighestDepthVrCamera == null)
        {
            _additionalSceneCameraData.SetRenderTypeBase();
            _uiSceneCamera.clearFlags = CameraClearFlags.Skybox;
            return;
        }
    
        _additionalSceneCameraData.SetRenderTypeOverlay();
        _uiSceneCamera.clearFlags = CameraClearFlags.Depth;
    
        var additionalHighestDepthCameraData = AdditionalCameraData.Create(VrCamera.HighestDepthVrCamera.CameraInUse);
        if (additionalHighestDepthCameraData.GetCameraStack().Contains(_uiSceneCamera)) return;
        
        additionalHighestDepthCameraData.GetCameraStack().Add(_uiSceneCamera);
    }

    private void PatchCanvas(Canvas canvas)
    {
        if (!canvas) return;
        
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
        if (canvas.GetComponent<VrUiCanvas>()) return;
        
        VrUiCanvas.Create(canvas, _uiCaptureCamera);
    }
}
