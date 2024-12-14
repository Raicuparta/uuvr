using System.IO;
using System.Reflection;
using BepInEx;

#if CPP
using Uuvr.OpenVR;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Injection;
#if LEGACY
using HarmonyLib;
using Uuvr.VrCamera;
using Uuvr.VrUi;
using Uuvr.VrUi.PatchModes;
#endif
#endif

namespace Uuvr;

[BepInPlugin(
#if LEGACY
    "raicuparta.uuvr-legacy",
#elif MODERN
    "raicuparta.uuvr-modern",
#endif
    "UUVR",
    "0.3.1")]
public class UuvrPlugin
#if CPP
: BasePlugin
#elif MONO
: BaseUnityPlugin
#endif
{
    private static UuvrPlugin _instance;
    public static string ModFolderPath { get; private set; }
    
#if CPP
    public override void Load()
#elif MONO
    private void Awake()
#endif
    {
        _instance = this;
        
        ModFolderPath = Path.GetDirectoryName(Assembly.GetAssembly(typeof(UuvrPlugin)).Location);
        
        new ModConfiguration(Config);
        
#if CPP
#if LEGACY
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());

        ClassInjector.RegisterTypeInIl2Cpp<VrCamera.VrCamera>();
        ClassInjector.RegisterTypeInIl2Cpp<VrCameraOffset>();
        ClassInjector.RegisterTypeInIl2Cpp<CanvasRedirect>();
        ClassInjector.RegisterTypeInIl2Cpp<UiOverlayRenderMode>();
        ClassInjector.RegisterTypeInIl2Cpp<VrUiCursor>();
        ClassInjector.RegisterTypeInIl2Cpp<VrUiManager>();
        ClassInjector.RegisterTypeInIl2Cpp<FollowTarget>();
        // ClassInjector.RegisterTypeInIl2Cpp<UuvrInput>();
        ClassInjector.RegisterTypeInIl2Cpp<UuvrPoseDriver>();
        ClassInjector.RegisterTypeInIl2Cpp<UuvrBehaviour>();
        // ClassInjector.RegisterTypeInIl2Cpp<AdditionalCameraData>();
       ClassInjector.RegisterTypeInIl2Cpp<VrCameraManager>();
       ClassInjector.RegisterTypeInIl2Cpp<CanvasRedirectPatchMode>();
       ClassInjector.RegisterTypeInIl2Cpp<ScreenMirrorPatchMode>();
#else
        ClassInjector.RegisterTypeInIl2Cpp<OpenVrManager>();
#endif
        ClassInjector.RegisterTypeInIl2Cpp<UuvrCore>();
#endif

        UuvrCore.Create();
    }
}
