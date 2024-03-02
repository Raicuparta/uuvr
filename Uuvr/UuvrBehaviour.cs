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
    protected UuvrBehaviour(IntPtr pointer) : base(pointer)
    {
    }
#endif
    
    protected virtual void Awake()
    {
#if CPP
        _onBeforeRenderAction = OnBeforeRender;
#endif
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
