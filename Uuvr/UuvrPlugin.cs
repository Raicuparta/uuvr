using BepInEx;

#if CPP
using BepInEx.IL2CPP;
using UnhollowerRuntimeLib;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
#endif

namespace Uuvr;

[BepInPlugin(
#if LEGACY
    "raicuparta.uuvr-legacy",
#elif MODERN
    "raicuparta.uuvr-modern",
#endif
    "UUVR",
    "0.3.0")]
public class UuvrPlugin
#if CPP
: BasePlugin
#elif MONO
: BaseUnityPlugin
#endif
{
    
#if CPP
    public override void Load()
#elif MONO
    private void Awake()
#endif
    {
        new ModConfiguration(Config);
        
#if CPP
        ClassInjector.RegisterTypeInIl2Cpp<VrCamera>();
        ClassInjector.RegisterTypeInIl2Cpp<UuvrCore>();
        ClassInjector.RegisterTypeInIl2Cpp<UuvrPoseDriver>();
        ClassInjector.RegisterTypeInIl2Cpp<ModXrManager>();
        ClassInjector.RegisterTypeInIl2Cpp<OpenXRSettings>();
        ClassInjector.RegisterTypeInIl2Cpp<XRLoader>();
        // ClassInjector.RegisterTypeInIl2Cpp<XRLoaderHelper>();
        ClassInjector.RegisterTypeInIl2Cpp<OpenXRLoaderBase>();
        ClassInjector.RegisterTypeInIl2Cpp<OpenXRLoader>();
        ClassInjector.RegisterTypeInIl2Cpp<XRManagerSettings>();
        ClassInjector.RegisterTypeInIl2Cpp<XRGeneralSettings>();
        // ClassInjector.RegisterTypeInIl2Cpp<OpenXRFeature>();
        // ClassInjector.RegisterTypeInIl2Cpp<OpenXRInteractionFeature>();
        ClassInjector.RegisterTypeInIl2Cpp<OpenXRRestarter>();
#endif

        UuvrCore.Create();

    }
}
