using System;
using UnityEngine;

namespace Uuvr;

public class UuvrBehaviour: MonoBehaviour
{
    private Action? _onBeforeRenderAction;

#if CPP
    protected UuvrBehaviour(IntPtr pointer) : base(pointer)
    {
    }
#endif
    
    protected virtual void Awake()
    {
        _onBeforeRenderAction = OnBeforeRender;
    }

    protected virtual void OnEnable()
    {
#if CPP
        Application.add_onBeforeRender(_onBeforeRenderAction);
#else
        Application.onBeforeRender += OnBeforeRender;
#endif
    }

    protected virtual void OnDisable()
    {
#if CPP
        Application.remove_onBeforeRender(_onBeforeRenderAction);
#else
        Application.onBeforeRender -= OnBeforeRender;
#endif
    }

    protected virtual void OnBeforeRender() {}
}
