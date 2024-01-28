using BepInEx;
using BepInEx.IL2CPP;
using UnhollowerRuntimeLib;

namespace Uuvr;

[BepInPlugin("raicuparta.unityuniversalvr", "UUVR", "0.3.0")]
public class UuvrPlugin : BasePlugin
{
    public override void Load()
    {
        ClassInjector.RegisterTypeInIl2Cpp<VrCamera>();
        AddComponent<UuvrCore>();
    }
}
