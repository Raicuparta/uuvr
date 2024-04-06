using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Vector3 = UnityEngine.Vector3;

namespace Uuvr.VrUi;

public class VrUiManager : UuvrBehaviour
{
#if CPP
    public VrUiManager(System.IntPtr pointer) : base(pointer)
    {
    }
#endif

    private Camera? _uiCaptureCamera;
    private Camera? _uiSceneCamera;
    private RenderTexture? _uiTexture;
    private GameObject? _vrUiQuad;
    private GameObject? _uiContainer;
    private FollowTarget? _containerFollowTarget;

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
        Create<VrUiCursor>(transform);
    }

    protected override void OnSettingChanged()
    {
        base.OnSettingChanged();
        var uiLayer = LayerHelper.GetVrUiLayer();

        _uiCaptureCamera.cullingMask = 1 << uiLayer;
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
    }

    private void SetUpUi()
    {
        _uiTexture = new RenderTexture(Screen.width, Screen.height, 16, RenderTextureFormat.ARGB32);
        var uiTextureAspectRatio = (float)_uiTexture.height / _uiTexture.width;

        _uiCaptureCamera = new GameObject("VrUiCaptureCamera").AddComponent<Camera>();
        VrCamera.VrCamera.IgnoredCameras.Add(_uiCaptureCamera);
        _uiCaptureCamera.transform.parent = transform;
        _uiCaptureCamera.clearFlags = CameraClearFlags.SolidColor;
        _uiCaptureCamera.backgroundColor = Color.clear;
        _uiCaptureCamera.targetTexture = _uiTexture;
        _uiCaptureCamera.depth = 100;

        _uiContainer = new GameObject("VrUiContainer")
        {
            transform =
            {
                parent = transform
            }
        };

        _containerFollowTarget = _uiContainer.AddComponent<FollowTarget>();

        var flatScreenView = FlatScreenView.Create(_uiContainer.transform);
        flatScreenView.transform.localPosition = Vector3.forward * 2f;
        flatScreenView.transform.localRotation = Quaternion.identity;

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
    }

    private void Update()
    {
        if (_uiTexture == null) SetUpUi();

        // TODO: handle finding canvases that aren't here because they don't have anything inside them.
        // Game example: Smushi.
        foreach (var canvas in GraphicRegistry.instance.m_Graphics.Keys)
        {
            PatchCanvas(canvas);
        }

        if (
            VrCamera.VrCamera.HighestDepthVrCamera != null &&
            VrCamera.VrCamera.HighestDepthVrCamera.ParentCamera != null &&
            _uiContainer != null &&
            _uiContainer.transform.parent != VrCamera.VrCamera.HighestDepthVrCamera.ParentCamera.transform &&
            _containerFollowTarget != null)
        {
            _containerFollowTarget.Target = ModConfiguration.Instance.PreferredUiRenderMode.Value == ModConfiguration.UiRenderMode.InWorld
                ? VrCamera.VrCamera.HighestDepthVrCamera.ParentCamera.transform
                : null;
        }
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
