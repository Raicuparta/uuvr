using UnityEngine;

namespace Uuvr.VrUi;

public class UiOverlayRenderMode: UuvrBehaviour
{
#if CPP
    public UiOverlayRenderMode(System.IntPtr pointer) : base(pointer)
    {
    }
#endif

    // Overlay camera that sees the UI quad where the captured UI is projected.
    private Camera? _uiSceneCamera;
    
    protected override void OnSettingChanged()
    {
        base.OnSettingChanged();
        _uiSceneCamera.cullingMask = 1 << LayerHelper.GetVrUiLayer();
    }

    private void Awake()
    {
        _uiSceneCamera = Create<UuvrPoseDriver>(transform).gameObject.AddComponent<Camera>();
        VrCamera.VrCamera.IgnoredCameras.Add(_uiSceneCamera);
        _uiSceneCamera.clearFlags = CameraClearFlags.Depth;
        _uiSceneCamera.depth = 100;
        _uiSceneCamera.cullingMask = 1 << LayerHelper.GetVrUiLayer();
    }
}