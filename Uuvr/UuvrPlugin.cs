using System.Reflection;
using BepInEx;
using HarmonyLib;

#if CPP
using BepInEx.IL2CPP;
using UnhollowerRuntimeLib;
using UnityEngine;
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
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        
#if CPP
        ClassInjector.RegisterTypeInIl2Cpp<UuvrBehaviour>();
        ClassInjector.RegisterTypeInIl2Cpp<VrCamera>();
        ClassInjector.RegisterTypeInIl2Cpp<UuvrCore>();
        ClassInjector.RegisterTypeInIl2Cpp<VrCameraOffset>();
        ClassInjector.RegisterTypeInIl2Cpp<VrCameraManager>();
        ClassInjector.RegisterTypeInIl2Cpp<VrUiManager>();
        ClassInjector.RegisterTypeInIl2Cpp<VrUiCanvas>();
        ClassInjector.RegisterTypeInIl2Cpp<UuvrPoseDriver>();
#endif

        UuvrCore.Create();

    }
}
