using BepInEx;

#if CPP
using BepInEx.IL2CPP;
using UnhollowerRuntimeLib;
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
#endif

        UuvrCore.Create();

    }
}
