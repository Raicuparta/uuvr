using System.Runtime.InteropServices;

namespace Uuvr.OpenVR;

public static class OpenVrApiExtra
{
    public const int k_nRenderEventID_WaitGetPoses = 201510020;
    public const int k_nRenderEventID_SubmitL = 201510021;
    public const int k_nRenderEventID_SubmitR = 201510022;
    public const int k_nRenderEventID_Flush = 201510023;
    public const int k_nRenderEventID_PostPresentHandoff = 201510024;

    [DllImport("openvr_api", EntryPoint = "UnityHooks_GetRenderEventFunc")]
    public static extern System.IntPtr GetRenderEventFunc();

    [DllImport("openvr_api", EntryPoint = "UnityHooks_SetSubmitParams")]
    public static extern void SetSubmitParams(VRTextureBounds_t boundsL, VRTextureBounds_t boundsR, EVRSubmitFlags nSubmitFlags);

    [DllImport("openvr_api", EntryPoint = "UnityHooks_SetColorSpace")]
    public static extern void SetColorSpace(EColorSpace eColorSpace);

    [DllImport("openvr_api", EntryPoint = "UnityHooks_EventWriteString")]
    public static extern void EventWriteString([In, MarshalAs(UnmanagedType.LPWStr)] string sEvent);
}