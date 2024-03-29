﻿using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace Uuvr;

public class UuvrCore: MonoBehaviour
{
    private readonly KeyboardKey _toggleVrKey = new (KeyboardKey.KeyCode.F3);
    private readonly KeyboardKey _reparentCameraKey = new (KeyboardKey.KeyCode.F4);
    private readonly KeyboardKey _vrUiKey = new (KeyboardKey.KeyCode.F5);
    
    private Type _xrSettingsType;
    private PropertyInfo _xrEnabledProperty;
    private bool _shouldPatchUi;
    private const string VR_UI_PARENT_NAME = "UUVR_UI_PARENT";
    
#if CPP
    public UuvrCore(IntPtr pointer) : base(pointer)
    {
    }
#endif

    private void Start()
    {
        _xrSettingsType =
            Type.GetType("UnityEngine.XR.XRSettings, UnityEngine.XRModule") ??
            Type.GetType("UnityEngine.XR.XRSettings, UnityEngine.VRModule") ??
            Type.GetType("UnityEngine.VR.VRSettings, UnityEngine");
        
        _xrEnabledProperty = _xrSettingsType.GetProperty("enabled");

        SetXrEnabled(false);
        SetPositionTrackingEnabled(false);
    }

    private void Update()
    {
        if (_toggleVrKey.UpdateIsDown()) ToggleXr();
        if (_reparentCameraKey.UpdateIsDown()) ReparentCamera();
        if (_vrUiKey.UpdateIsDown()) ToggleXrUi();

        UpdateXrUi();
    }
    
    private void ToggleXrUi()
    {
        _shouldPatchUi = !_shouldPatchUi;
    }

    private void ToggleXr()
    {
        bool xrEnabled = (bool) _xrEnabledProperty.GetValue(null, null);
        SetXrEnabled(!xrEnabled);
    }
    
    private void ReparentCamera() {
        Console.WriteLine("Reparenting Camera...");

        Camera mainCamera = Camera.main ?? Camera.current;
        mainCamera.enabled = false;

        GameObject vrCameraObject = new("VrCamera");
        Camera vrCamera = vrCameraObject.AddComponent<Camera>();
        vrCamera.tag = "MainCamera";
        vrCamera.transform.parent = mainCamera.transform;
        vrCamera.transform.localPosition = Vector3.zero;
    }

    private void SetXrEnabled(bool enabled)
    {
        Console.WriteLine($"Setting XR enabled to {enabled}");

        _xrEnabledProperty.SetValue(null, enabled, null);
        
        // TODO verify if exists etc.
        try
        {

            if (enabled)
            {
                Camera.main.gameObject.AddComponent<VrCamera>();
            }
            else
            {
                Destroy(Camera.main.gameObject.GetComponent<VrCamera>());
            }
        } catch
        {
            
        }
    }

    private void SetPositionTrackingEnabled(bool enabled)
    {
        Type inputTrackingType = 
            Type.GetType("UnityEngine.XR.InputTracking, UnityEngine.XRModule") ??
            Type.GetType("UnityEngine.XR.InputTracking, UnityEngine.VRModule");

        if (inputTrackingType != null)
        {
            PropertyInfo disablePositionalTrackingProperty = inputTrackingType.GetProperty("disablePositionalTracking");
            if (disablePositionalTrackingProperty != null)
            {
                disablePositionalTrackingProperty.SetValue(null, !enabled, null);
            }
            else
            {
                Console.WriteLine("Failed to get property disablePositionalTracking");
            }
        }
        else
        {
            Console.WriteLine("Failed to get type UnityEngine.XR.InputTracking");
        }
    }

    private void UpdateXrUi()
    {
        if (!_shouldPatchUi) return;

        List<Canvas> canvases = new();
        foreach (Canvas canvas in GraphicRegistry.instance.m_Graphics.Keys)
        {
            if (!canvas) continue;
            
            // World space canvases probably already work as intended in VR.
            if (canvas.renderMode == RenderMode.WorldSpace) continue;

            // Screen space canvases being rendered to textures are probably already working as intended in VR.
            if (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera?.targetTexture != null) continue;
            
            canvases.Add(canvas);
        }
        
        foreach (Canvas canvas in canvases)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main ?? Camera.current;
            canvas.planeDistance = 1;
            canvas.sortingOrder = int.MaxValue;

            Transform originalParent = canvas.transform.parent;
            if (originalParent?.name == VR_UI_PARENT_NAME) continue;
            
            Transform vrUiParent = new GameObject(VR_UI_PARENT_NAME).transform;
            vrUiParent.parent = originalParent;
            vrUiParent.localPosition = Vector3.zero;
            vrUiParent.localRotation = Quaternion.identity;
            canvas.transform.parent = vrUiParent;

            vrUiParent.transform.localScale = Vector3.one * 0.3f;

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler)
            {
                scaler.scaleFactor = 2;
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            }
        }
    }
}
