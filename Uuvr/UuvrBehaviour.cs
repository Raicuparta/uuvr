#if CPP
using System;
#endif

using UnityEngine;

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
        Application.onBeforeRender += OnBeforeRender;
#endif
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
        Application.onBeforeRender -= OnBeforeRender;
#endif
    }

    protected virtual void OnBeforeRender() {}
}
