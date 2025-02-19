using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

#if MODERN && MONO
using Uuvr.VrCamera;
#endif

#if CPP
using BepInEx.Unity.IL2CPP.Utils;
#endif

namespace Uuvr.VrUi.PatchModes;

public class ScreenMirrorPatchMode : UuvrBehaviour, VrUiPatchMode
{
#if CPP
    public ScreenMirrorPatchMode(System.IntPtr pointer) : base(pointer)
    {
    }
#endif

    private CommandBuffer? _commandBuffer;
    private Camera? _clearCamera;
    private float _scale = 2f;
    private RenderTexture _targetTexture;
    private Coroutine _endOfFrameCoroutine;

    protected override void OnEnable()
    {
        base.OnEnable();
        SetXrMirror(false);
        _endOfFrameCoroutine = this.StartCoroutine(EndOfFrameCoroutine());
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        SetXrMirror(true);
        StopCoroutine(_endOfFrameCoroutine);
        Reset();
    }

    private void Awake()
    {
        Debug.Log("mirror mode Start");
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

#if MODERN && MONO
        var additionalData = AdditionalCameraData.Create(_clearCamera);
        if (additionalData != null)
        {
            additionalData.SetAllowXrRendering(false);
        }
#endif
    }

    public void SetUpTargetTexture(RenderTexture targetTexture)
    {
        _targetTexture = targetTexture;

        if (!enabled) return;
        _commandBuffer = CreateCommandBuffer();
        _commandBuffer.name = "UUVR UI";
        _commandBuffer.Blit(BuiltinRenderTextureType.CameraTarget, targetTexture);
    }
    
    private static CommandBuffer CreateCommandBuffer()
    {
        var commandBufferType = typeof(CommandBuffer);
        var constructor = commandBufferType.GetConstructor(Type.EmptyTypes);
        return (CommandBuffer)constructor.Invoke(null);
    }

    private void Reset()
    {
        if (_commandBuffer == null) return;
        _commandBuffer.Dispose();
        _commandBuffer = null;
    }

    private void SetXrMirror(bool mirror)
    {
        var xrSettingsType =
            Type.GetType("UnityEngine.XR.XRSettings, UnityEngine.XRModule") ??
            Type.GetType("UnityEngine.XR.XRSettings, UnityEngine.VRModule") ??
            Type.GetType("UnityEngine.VR.VRSettings, UnityEngine");
        
        // This method of projecting the UI onto a texture basically just copies what's currently on the flat screen.
        // We don't want the game itself to show up there, only the UI. So we disable mirroring the VR view to the flat screen.
        xrSettingsType.GetProperty("showDeviceView").SetValue(null, mirror, null);
    }

    private IEnumerator EndOfFrameCoroutine()
    {
        while (true)
        {
            if (_targetTexture != null && (_commandBuffer == null || Screen.width != _targetTexture.width || Screen.height != _targetTexture.height))
            {
                SetUpTargetTexture(_targetTexture);
            }
            yield return new WaitForEndOfFrame();

            if (_commandBuffer != null && _targetTexture != null)
            {
                Graphics.ExecuteCommandBuffer(_commandBuffer);
            }
        }
    }
}
