using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Uuvr;

public class FlatScreenView: MonoBehaviour
{
    private CommandBuffer _commandBuffer;
    private RenderTexture _targetTexture;
    private Material _targetMaterial;
    private GameObject _quad;
    private Camera _clearCamera;
    private float _scale = 2f;

    public static FlatScreenView Create(Transform parent)
    {
        return new GameObject(nameof(FlatScreenView))
        {
            transform =
            {
                parent = parent
            }
        }.AddComponent<FlatScreenView>();
    }
    
    private void Start()
    {
        _targetMaterial = new Material(Canvas.GetDefaultCanvasMaterial());
        _quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(_quad.GetComponent<Collider>());
        _quad.transform.parent = transform;
        _quad.transform.localPosition = Vector3.zero;
        _quad.transform.localRotation = Quaternion.identity;
        _quad.GetComponent<Renderer>().material = _targetMaterial;
        
        // TODO: find a layer that's visible by the top camera.
        // _quad.layer = LayerHelper.GetVrUiLayer();

        // This camera is here only to clear the screen.
        _clearCamera = gameObject.AddComponent<Camera>();
        _clearCamera.stereoTargetEye = StereoTargetEyeMask.None;
        _clearCamera.depth = -100;
        _clearCamera.cullingMask = 0;
        // TODO: not sure if this works in all games, check Dredge.
        _clearCamera.clearFlags = CameraClearFlags.SolidColor;
        _clearCamera.backgroundColor = Color.clear;
        
        // HDR seems to prevent a proper transparent clear (would some times become opaque with this enabled).
        _clearCamera.allowHDR = false;
        // This I'm not sure if it helps but since the HDR thing was a problem, might as well.
        _clearCamera.allowMSAA = false;

#if MODERN
        var additionalData = AdditionalCameraData.Create(_clearCamera);
        additionalData.SetAllowXrRendering(false);
#endif
        
        var xrSettingsType =
            Type.GetType("UnityEngine.XR.XRSettings, UnityEngine.XRModule") ??
            Type.GetType("UnityEngine.XR.XRSettings, UnityEngine.VRModule") ??
            Type.GetType("UnityEngine.VR.VRSettings, UnityEngine");
        
        // This method of projecting the UI onto a texture basically just copies what's currently on the flat screen.
        // We don't want the game itself to show up there, only the UI. So we disable mirroring the VR view to the flat screen.
        xrSettingsType.GetProperty("showDeviceView").SetValue(null, false, null);
        
        StartCoroutine(EndOfFrameCoroutine());
    }

    private void SetUp()
    {
        _targetTexture = new RenderTexture(Screen.width, Screen.height, 1);
        _targetMaterial.mainTexture = _targetTexture;
        _commandBuffer = new CommandBuffer();
        _commandBuffer.name = "UUVR UI";
        _commandBuffer.Blit(BuiltinRenderTextureType.CameraTarget, _targetTexture);
        
        
        // TODO: I don't understand why I need to flip some of these values.
        // When I tried in the Unity Editor, I had to flip the widgh.
        // When I tried in Smushi, I had to flip the height.
        _quad.transform.localScale = new Vector3(1, (float) -Screen.height / Screen.width, 1) * _scale;
    }

    private IEnumerator EndOfFrameCoroutine()
    {
        while (true)
        {
            if (_commandBuffer == null || Screen.width != _targetTexture.width || Screen.height != _targetTexture.height)
            {
                SetUp();
            }
            yield return new WaitForEndOfFrame();
            Graphics.ExecuteCommandBuffer(_commandBuffer);
        }
    }
}
