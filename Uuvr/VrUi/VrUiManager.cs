using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Uuvr.VrUi.PatchModes;

namespace Uuvr.VrUi;

public class VrUiManager : UuvrBehaviour
{
#if CPP
    public VrUiManager(System.IntPtr pointer) : base(pointer)
    {
    }
#endif

    // Overlay camera that sees the UI quad where the captured UI is projected.
    private Camera? _uiSceneCamera;
    
    private RenderTexture? _uiTexture;
    private GameObject? _vrUiQuad;
    private GameObject? _uiContainer;
    private FollowTarget? _containerFollowTarget;
    private CanvasRedirectPatchMode? _canvasRedirectPatchMode;
    private ScreenMirrorPatchMode? _screenMirrorPatchMode;

    private void Start()
    {
        SetUpUi();
        OnSettingChanged();
        Create<VrUiCursor>(transform);
    }

    protected override void OnSettingChanged()
    {
        base.OnSettingChanged();
        var uiLayer = LayerHelper.GetVrUiLayer();

        _uiSceneCamera.cullingMask = 1 << uiLayer;
        _vrUiQuad.layer = uiLayer;

        switch(ModConfiguration.Instance.PreferredUiRenderMode.Value)
        {
            case ModConfiguration.UiRenderMode.InWorld:
                _uiSceneCamera.gameObject.SetActive(false);
                break;
            case ModConfiguration.UiRenderMode.OverlayCamera:
                _uiSceneCamera.gameObject.SetActive(true);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        _screenMirrorPatchMode.enabled = ModConfiguration.Instance.PreferredUiPatchMode.Value == ModConfiguration.UiPatchMode.Mirror;
        _canvasRedirectPatchMode.enabled = ModConfiguration.Instance.PreferredUiPatchMode.Value == ModConfiguration.UiPatchMode.CanvasRedirect;

        UpdateFollowTarget();
    }

    private void SetUpUi()
    {
        _uiTexture = new RenderTexture(Screen.width, Screen.height, 16, RenderTextureFormat.ARGB32);
        var uiTextureAspectRatio = (float)_uiTexture.height / _uiTexture.width;

        _uiContainer = new GameObject("VrUiContainer")
        {
            transform =
            {
                parent = transform
            }
        };

        _containerFollowTarget = _uiContainer.AddComponent<FollowTarget>();

        _vrUiQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(_vrUiQuad.GetComponent<Collider>());
        _vrUiQuad.name = "VrUiQuad";
        _vrUiQuad.transform.parent = _uiContainer.transform;
        _vrUiQuad.transform.localPosition = Vector3.forward * 2f;
        var quadWidth = 1.8f;
        var quadHeight = quadWidth * uiTextureAspectRatio;
        _vrUiQuad.transform.localScale = new Vector3(quadWidth, quadHeight, 1f);

        var renderer = _vrUiQuad.GetComponent<Renderer>();

        // TODO: not sure if this is visible in all games, check Aragami.
        renderer.material = Canvas.GetDefaultCanvasMaterial();
        renderer.material.mainTexture = _uiTexture;
        renderer.material.renderQueue = 5000;

        _uiSceneCamera = Create<UuvrPoseDriver>(_uiContainer.transform).gameObject.AddComponent<Camera>();
        VrCamera.VrCamera.IgnoredCameras.Add(_uiSceneCamera);
        _uiSceneCamera.clearFlags = CameraClearFlags.Depth;
        _uiSceneCamera.depth = 100;

        _canvasRedirectPatchMode = gameObject.AddComponent<CanvasRedirectPatchMode>();
        _canvasRedirectPatchMode.SetUpTargetTexture(_uiTexture);

        _screenMirrorPatchMode = gameObject.AddComponent<ScreenMirrorPatchMode>();
        _screenMirrorPatchMode.SetUpTargetTexture(_uiTexture);
    }

    private void Update()
    {
        if (_uiTexture == null) SetUpUi();
        
        if (
            VrCamera.VrCamera.HighestDepthVrCamera != null &&
            VrCamera.VrCamera.HighestDepthVrCamera.ParentCamera != null &&
            _uiContainer != null &&
            _uiContainer.transform.parent != VrCamera.VrCamera.HighestDepthVrCamera.ParentCamera.transform &&
            _containerFollowTarget != null)
        {
            UpdateFollowTarget();
        }
    }

    private void UpdateFollowTarget()
    {
        _containerFollowTarget.Target = ModConfiguration.Instance.PreferredUiRenderMode.Value == ModConfiguration.UiRenderMode.InWorld
            ? VrCamera.VrCamera.HighestDepthVrCamera.ParentCamera.transform
            : null;
    }
}
