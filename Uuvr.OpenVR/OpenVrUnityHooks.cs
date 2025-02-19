using System.Runtime.InteropServices;
using UnityEngine;

namespace Uuvr.OpenVR;

internal static class OpenVrUnityHooks
{
    private const int eventWaitGetPoses = 201510020;
    private const int eventSubmitLeftEye = 201510021;
    private const int eventSubmitRightEye = 201510022;
    private const int eventFlush = 201510023;
    private const int eventPostPresentHandoff = 201510024;
    
    private static System.IntPtr? _renderEventFunc;

    [DllImport("openvr_api", EntryPoint = "UnityHooks_GetRenderEventFunc")]
    private static extern System.IntPtr GetRenderEventFunc();

    [DllImport("openvr_api", EntryPoint = "UnityHooks_SetSubmitParams")]
    public static extern void SetSubmitParams(VRTextureBounds_t boundsL, VRTextureBounds_t boundsR, EVRSubmitFlags nSubmitFlags);

    [DllImport("openvr_api", EntryPoint = "UnityHooks_SetColorSpace")]
    public static extern void SetColorSpace(EColorSpace eColorSpace);
    
    private static void QueueEventOnRenderThread(int eventID)
    {
        _renderEventFunc ??= GetRenderEventFunc();
        GL.IssuePluginEvent(_renderEventFunc.Value, eventID);
    }

    public static void WaitGetPoses()
    {
        QueueEventOnRenderThread(eventWaitGetPoses);
    }
    
    public static void SubmitLeftEye()
    {
        QueueEventOnRenderThread(eventSubmitLeftEye);
    }
    
    public static void SubmitRightEye()
    {
        QueueEventOnRenderThread(eventSubmitRightEye);
    }
    
    public static void Flush()
    {
        QueueEventOnRenderThread(eventFlush);
    }
    
    public static void PostPresentHandoff()
    {
        QueueEventOnRenderThread(eventPostPresentHandoff);
    }
}