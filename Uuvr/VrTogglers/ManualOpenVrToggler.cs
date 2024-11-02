using System;
using System.Reflection;
using UnityEngine;
using Uuvr.OpenVR;

namespace Uuvr.VrTogglers;

// This one should work on the widest range of Unity versions, not sure which yet.
// Specifically made to handle modern IL2CPP games where it's difficult to inject the XR Plugins,
// but should also be useful for older games where there's poor or no native VR support.
public class ManualOpenVrToggler: VrToggler
{
    private static OpenVrManager? _openVrManager;

    protected override bool SetUp()
    {
        return true;
    }

    protected override bool EnableVr()
    {
        _openVrManager = OpenVrManager.Create();
        return true;
    }

    protected override bool DisableVr()
    {
        GameObject.Destroy(_openVrManager.gameObject);
        return true;
    }
}