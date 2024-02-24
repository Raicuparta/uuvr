using BepInEx;

#if CPP
using BepInEx.IL2CPP;
using UnhollowerRuntimeLib;
#endif

namespace Uuvr;

[BepInPlugin("raicuparta.unityuniversalvr", "UUVR", "0.3.0")]
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
#if CPP
        ClassInjector.RegisterTypeInIl2Cpp<VrCamera>();
#endif

#if MONO
        gameObject.
#endif
        AddComponent<UuvrCore>();
        
    }
}
