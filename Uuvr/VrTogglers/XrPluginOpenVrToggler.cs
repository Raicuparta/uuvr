#if MODERN
using System;
using Unity.XR.OpenVR;
using UnityEngine;
using UnityEngine.XR.Management;

namespace Uuvr.VrTogglers;

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