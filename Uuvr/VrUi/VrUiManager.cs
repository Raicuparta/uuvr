using System.Collections.Generic;
using System.Linq;
using BepInEx;
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

    private RenderTexture? _uiTexture;
    private GameObject? _vrUiQuad;
    private Renderer? _vrUiRenderer;
    private GameObject? _uiContainer;
    private CanvasRedirectPatchMode? _canvasRedirectPatchMode;
    private ScreenMirrorPatchMode? _screenMirrorPatchMode;
    private FollowTarget? _worldRenderModeFollowTarget;
    private UiOverlayRenderMode? _uiOverlayRenderMode;

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

        _vrUiQuad.layer = uiLayer;

        _uiOverlayRenderMode.gameObject.SetActive(ModConfiguration.Instance.PreferredUiRenderMode.Value ==
                                                  ModConfiguration.UiRenderMode.OverlayCamera);
        _worldRenderModeFollowTarget.enabled = ModConfiguration.Instance.PreferredUiRenderMode.Value ==
                                               ModConfiguration.UiRenderMode.InWorld;
        if (ModConfiguration.Instance.PreferredUiRenderMode.Value != ModConfiguration.UiRenderMode.InWorld)
        {
            _worldRenderModeFollowTarget.transform.localPosition = Vector3.zero;
            _worldRenderModeFollowTarget.transform.localRotation = Quaternion.identity;
        }

        _screenMirrorPatchMode.enabled = ModConfiguration.Instance.PreferredUiPatchMode.Value ==
                                         ModConfiguration.UiPatchMode.Mirror;
        _canvasRedirectPatchMode.enabled = ModConfiguration.Instance.PreferredUiPatchMode.Value ==
                                           ModConfiguration.UiPatchMode.CanvasRedirect;

        var uiTextureAspectRatio = (float) _uiTexture.height / _uiTexture.width;
        var quadWidth = 1.8f * ModConfiguration.Instance.VrUiScale.Value;
        var quadHeight = quadWidth * uiTextureAspectRatio;
        _vrUiQuad.transform.localScale = new Vector3(quadWidth, quadHeight, 1f);

        // For some reason I need to flip the UI upside down when using mirror mode.
        var yScale = Mathf.Abs(_vrUiQuad.transform.localScale.y);
        if (ModConfiguration.Instance.PreferredUiPatchMode.Value == ModConfiguration.UiPatchMode.Mirror)
        {
            yScale *= -1;
        }

        _vrUiQuad.transform.localScale =
            new Vector3(_vrUiQuad.transform.localScale.x, yScale, _vrUiQuad.transform.localScale.z);
        _vrUiQuad.transform.localPosition = ModConfiguration.Instance.VrUiPosition.Value;


        if (_vrUiRenderer != null)
        {
            _vrUiRenderer.material.shader = GetVrUiShader();
            _vrUiRenderer.material.renderQueue = ModConfiguration.Instance.VrUiRenderQueue.Value;
        }

        UpdateFollowTarget();
    }

    private static Shader GetVrUiShader()
    {
        if (ModConfiguration.Instance.VrUiShader.Value.IsNullOrWhiteSpace())
            return Canvas.GetDefaultCanvasMaterial().shader;

        var shader = Shader.Find(ModConfiguration.Instance.VrUiShader.Value);

        return shader != null ? shader : Canvas.GetDefaultCanvasMaterial().shader;
    }

    private void SetUpUi()
    {
        _uiTexture = new RenderTexture(Screen.width, Screen.height, 16, RenderTextureFormat.ARGB32);

        _uiContainer = new GameObject("VrUiContainer")
        {
            transform =
            {
                parent = transform
            }
        };

        _vrUiQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(_vrUiQuad.GetComponent("Collider"));
        _vrUiQuad.name = "VrUiQuad";
        _vrUiQuad.transform.parent = _uiContainer.transform;

        _vrUiRenderer = _vrUiQuad.GetComponent<Renderer>();
        _vrUiRenderer.material = new Material(Canvas.GetDefaultCanvasMaterial())
        {
            mainTexture = _uiTexture
        };

        _canvasRedirectPatchMode = gameObject.AddComponent<CanvasRedirectPatchMode>();
        _canvasRedirectPatchMode.SetUpTargetTexture(_uiTexture);

        _screenMirrorPatchMode = gameObject.AddComponent<ScreenMirrorPatchMode>();
        _screenMirrorPatchMode.SetUpTargetTexture(_uiTexture);

        _uiOverlayRenderMode = Create<UiOverlayRenderMode>(transform);
        _worldRenderModeFollowTarget = _uiContainer.AddComponent<FollowTarget>();
    }

    private void Update()
    {
        if (_uiTexture == null) SetUpUi();

        if (
            VrCamera.VrCamera.HighestDepthVrCamera != null &&
            VrCamera.VrCamera.HighestDepthVrCamera.ParentCamera != null &&
            _uiContainer != null &&
            _uiContainer.transform.parent != VrCamera.VrCamera.HighestDepthVrCamera.ParentCamera.transform &&
            _worldRenderModeFollowTarget != null)
        {
            UpdateFollowTarget();
        }
    }

    private void UpdateFollowTarget()
    {
        if (VrCamera.VrCamera.HighestDepthVrCamera == null ||
            VrCamera.VrCamera.HighestDepthVrCamera.ParentCamera == null) return;

        _worldRenderModeFollowTarget.Target = ModConfiguration.Instance.PreferredUiRenderMode.Value ==
                                              ModConfiguration.UiRenderMode.InWorld
            ? VrCamera.VrCamera.HighestDepthVrCamera.ParentCamera.transform
            : null;
    }
}