#if MODERN && MONO
using System;
using Unity.XR.OpenVR;
using UnityEngine;
using UnityEngine.XR.Management;

namespace Uuvr.VrTogglers;

// This is for Unity versions that let you add VR support via "XR Plugins".
// Has the advantage of not requiring modifying any files like globalGameManagers.
// This does require adding some needed files (native plugins and other stuff), which is handled by Uuvr.Patcher.
// There is a separate project in Uuvr.XR.OpenVR that's just a copy of the code from that plugin.
// I haven't been able to get this working in IL2CPP games since usually too much stuff gets stripped,
// so this is currently limited to Mono.
// Should work on Mono Unity versions 2018.4 and later.
public class XrPluginOpenVrToggler: XrPluginToggler
{
    protected override XRLoader CreateLoader()
    {
        var xrLoader = ScriptableObject.CreateInstance<OpenVRLoader>();

        xrLoader.Initialize();

        var openVrSettings = OpenVRSettings.GetSettings();
        if (openVrSettings == null) throw new Exception("OpenVRSettings instance is null");
        openVrSettings.EditorAppKey = "uuvr";
        openVrSettings.InitializationType = OpenVRSettings.InitializationTypes.Scene;
        openVrSettings.StereoRenderingMode = OpenVRSettings.StereoRenderingModes.MultiPass;
        openVrSettings.SetMirrorViewMode(OpenVRSettings.MirrorViewModes.Right);

        return xrLoader;
    }
}
#endif