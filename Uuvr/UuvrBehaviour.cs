#if CPP
using System;
#endif

using BepInEx.Configuration;
using UnityEngine;
#if MODERN && MONO
using UnityEngine.Rendering;
#endif

namespace Uuvr;

public class UuvrBehaviour: MonoBehaviour
{
#if CPP
    private Action? _onBeforeRenderAction;
#endif

#if CPP
    public UuvrBehaviour(IntPtr pointer) : base(pointer)
    {
    }
#endif
    
    public static T Create<T>(Transform parent) where T: UuvrBehaviour
    {
        return new GameObject(typeof(T).Name)
        {
            transform =
            {
                parent = parent,
                localPosition = Vector3.zero,
                localRotation = Quaternion.identity
            }
        }.AddComponent<T>();
    }
    
    protected virtual void Awake()
    {
#if CPP
        _onBeforeRenderAction = OnBeforeRender;
#endif
    }

    protected virtual void OnEnable()
    {
#if CPP
        try
        {
            Application.add_onBeforeRender(_onBeforeRenderAction);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Failed to listen to BeforeRender: {exception}");
        }
#else
        // TODO: This doesn't exist for unity <2017
        Application.onBeforeRender += OnBeforeRender;
#endif

#if MODERN && MONO
        RenderPipelineManager.beginFrameRendering += OnBeginFrameRendering;
        RenderPipelineManager.endFrameRendering += OnEndFrameRendering;
#endif

        ModConfiguration.Instance.Config.SettingChanged += ConfigOnSettingChanged;
    }

    protected virtual void OnDisable()
    {
#if CPP
        try
        {
            Application.remove_onBeforeRender(_onBeforeRenderAction);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Failed to unlisten from BeforeRender: {exception}");
        }
#else
        // TODO: This might not exist?
        Application.onBeforeRender -= OnBeforeRender;
#endif
        
#if MODERN && MONO
        // TODO: This might not exist? maybe ok for modern though.
        RenderPipelineManager.beginFrameRendering -= OnBeginFrameRendering;
        RenderPipelineManager.endFrameRendering -= OnEndFrameRendering;
#endif
        
        ModConfiguration.Instance.Config.SettingChanged -= ConfigOnSettingChanged;
    }

    private void ConfigOnSettingChanged(object? sender, SettingChangedEventArgs e)
    {
        OnSettingChanged();
    }

    protected virtual void OnBeforeRender() {}

    protected virtual void OnSettingChanged() {}

#if MODERN && MONO
    private void OnBeginFrameRendering(ScriptableRenderContext arg1, Camera[] arg2)
    {
        OnBeginFrameRendering();
    }

    private void OnEndFrameRendering(ScriptableRenderContext arg1, Camera[] arg2)
    {
        OnEndFrameRendering();
    }
    
    protected virtual void OnBeginFrameRendering() {}
    
    protected virtual void OnEndFrameRendering() {}
#endif
}
