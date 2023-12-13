using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Collections;

namespace UuvrPluginMono;

[BepInPlugin("raicuparta.unityuniversalvr", "Unity Universal VR", "0.2.0")]
public class MonoPlugin : BaseUnityPlugin
{
    private readonly KeyboardKey _toggleVrKey = new (KeyboardKey.KeyCode.F3);
    private readonly KeyboardKey _reparentCameraKey = new (KeyboardKey.KeyCode.F4);
    private readonly KeyboardKey _vrUiKey = new (KeyboardKey.KeyCode.F5);

    private bool _vrEnabled;
    private bool _setUpDone;
    private Type _xrSettingsType;
    private Type _cameraType;
    private Type _gameObjectType;
    private Type _transformType;
    private PropertyInfo _xrEnabledProperty;
    private object _mainCameraRenderTexture;

    private const string VR_UI_PARENT_NAME = "UUVR_UI_PARENT";

    private void Start()
    {
        _xrSettingsType =
            Type.GetType("UnityEngine.XR.XRSettings, UnityEngine.XRModule") ??
            Type.GetType("UnityEngine.XR.XRSettings, UnityEngine.VRModule") ??
            Type.GetType("UnityEngine.VR.VRSettings, UnityEngine");
        
        
        _xrEnabledProperty = _xrSettingsType.GetProperty("enabled");
        
        _cameraType = Type.GetType("UnityEngine.Camera, UnityEngine.CoreModule") ??
                      Type.GetType("UnityEngine.Camera, UnityEngine");

        _gameObjectType = Type.GetType("UnityEngine.GameObject, UnityEngine.CoreModule") ??
                          Type.GetType("UnityEngine.GameObject, UnityEngine");

        _transformType = Type.GetType("UnityEngine.Transform, UnityEngine.CoreModule") ??
                         Type.GetType("UnityEngine.Transform, UnityEngine");
        
        SetXrEnabled(false);
        SetPositionTrackingEnabled(false);
    }

    private void Update()
    {
        if (_toggleVrKey.UpdateIsDown()) ToggleXr();
        if (_reparentCameraKey.UpdateIsDown()) ReparentCamera();
        if (_vrUiKey.UpdateIsDown()) SetUpUi();
    }

    private void ToggleXr()
    {
        bool xrEnabled = (bool) _xrEnabledProperty.GetValue(null, null);
        SetXrEnabled(!xrEnabled);
    }

    private void DisableRenderTexture()
    {
        try
        {
            object mainCamera = _cameraType.GetProperty("main").GetValue(null, null);

            if (mainCamera == null)
            {
                Console.WriteLine("Failed to find main camera, so not doing anything about render textures. This might be OK.");
                return;
            }

            _mainCameraRenderTexture = _cameraType.GetProperty("targetTexture").GetValue(mainCamera, null);

            _cameraType.GetProperty("targetTexture").SetValue(mainCamera, null, null);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to disable render texture: {e}");
        }
    }

    private void EnableRenderTexture()
    {
        try
        {
            object mainCamera = _cameraType.GetProperty("main").GetValue(null, null);

            if (mainCamera == null)
            {
                Console.WriteLine("Failed to find main camera");
                return;
            }

            _cameraType.GetProperty("targetTexture").SetValue(mainCamera, _mainCameraRenderTexture, null);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to enable render texture: {e}");
        }
    }
    
    private void ReparentCamera() {

        Console.WriteLine("Reparenting Camera...");

        object mainCamera = _cameraType.GetProperty("main").GetValue(null, null);
        _cameraType.GetProperty("enabled").SetValue(mainCamera, false, null);

        Console.WriteLine("Reparenting Camera 1");
        object vrCameraObject = Activator.CreateInstance(_gameObjectType);
        MethodInfo addComponentMethod = _gameObjectType.GetMethod("AddComponent", new[] { typeof(Type) });
        object vrCamera = addComponentMethod.Invoke(vrCameraObject, new[] { _cameraType });
        object mainCameraTransform = _cameraType.GetProperty("transform").GetValue(mainCamera, null);
        
        Console.WriteLine("Reparenting Camera 2");
        object vrCameraTransform = _cameraType.GetProperty("transform").GetValue(vrCamera, null);
        
        Console.WriteLine("Reparenting Camera 3");
        _transformType.GetProperty("parent").SetValue(vrCameraTransform, mainCameraTransform, null);
        _transformType.GetProperty("localPosition").SetValue(vrCameraTransform, null, null);
    }

    private void SetXrEnabled(bool enabled)
    {
        Console.WriteLine($"Setting XR enabled to {enabled}");
        _xrEnabledProperty.SetValue(null, enabled, null);
        
        if (enabled)
        {
            DisableRenderTexture();
        }
        else
        {
            EnableRenderTexture();
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

    private static void SetUpUi()
    {
        Console.WriteLine("Setting Up VR UI...");

        List<Canvas> canvases = GraphicRegistry.instance.m_Graphics.Keys.ToList();
        
        Console.WriteLine($"Found {canvases.Count}");
        
        foreach (Canvas canvas in canvases)
        {
            if (canvas.renderMode == RenderMode.WorldSpace) continue;

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
