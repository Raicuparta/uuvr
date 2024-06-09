using UnityEngine;

namespace Uuvr.VrUi.PatchModes;

public abstract class VrUiPatchMode: UuvrBehaviour
{
    public abstract void SetUpTargetTexture(RenderTexture targetTexture);
}