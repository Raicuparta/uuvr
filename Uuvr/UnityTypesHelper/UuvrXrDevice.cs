using System;
using System.Reflection;

namespace Uuvr.UnityTypesHelper;

public static class UuvrXrDevice
{
    public static readonly Type? XrDeviceType = Type.GetType("UnityEngine.XR.XRDevice, UnityEngine.XRModule") ??
                                                Type.GetType("UnityEngine.XR.XRDevice, UnityEngine.VRModule") ??
                                                Type.GetType("UnityEngine.VR.VRDevice, UnityEngine.VRModule") ??
                                                Type.GetType("UnityEngine.VR.VRDevice, UnityEngine");
    
    public static readonly PropertyInfo? RefreshRateProperty = XrDeviceType?.GetProperty("refreshRate");
}
