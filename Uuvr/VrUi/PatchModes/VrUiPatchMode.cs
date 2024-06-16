using UnityEngine;

namespace Uuvr.VrUi.PatchModes;

public abstract class VrUiPatchMode: UuvrBehaviour
{
#if CPP
    protected VrUiPatchMode(System.IntPtr pointer) : base(pointer)
    {
    }
#endif

    public abstract void SetUpTargetTexture(RenderTexture targetTexture);
}