#if MODERN && MONO
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;

namespace Uuvr.VrTogglers;

// This is for Unity versions that let you add VR support via "XR Plugins".
// Has the advantage of not requiring modifying any files like globalGameManagers.
// This does require adding some needed files (native plugins and other stuff), which is handled by Uuvr.Patcher.
// There is a separate project in Uuvr.XR.OpenXR that's just a copy of the code from that plugin.
// I haven't been able to get this working in IL2CPP games since usually too much stuff gets stripped,
// so this is currently limited to Mono.
// Should work on Mono Unity versions 2018.4 and later.
public class XrPluginOpenXrToggler: XrPluginToggler
{
    protected override XRLoader CreateLoader()
    {
        var xrLoader = ScriptableObject.CreateInstance<OpenXRLoader>();
        OpenXRSettings.Instance.renderMode = OpenXRSettings.RenderMode.MultiPass;
        return xrLoader;
    }
}
#endif