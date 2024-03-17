#if MODERN
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;

namespace Uuvr.VrTogglers;

public class XrPluginOpenXrToggler: XrPluginToggler
{
    protected override XRLoader CreateLoader()
    {
        OpenXRLoader xrLoader = ScriptableObject.CreateInstance<OpenXRLoader>();
        OpenXRSettings.Instance.renderMode = OpenXRSettings.RenderMode.MultiPass;
        return xrLoader;
    }
}
#endif