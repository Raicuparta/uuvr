using BepInEx;
using BepInEx.IL2CPP;
using UnhollowerRuntimeLib;
using UuvrPluginMono;

namespace UuvrPluginIl2Cpp;

[BepInPlugin("raicuparta.unityuniversalvr", "Unity Universal VR", "0.2.1")]
public class Il2CppPlugin : BasePlugin
{
    public override void Load()
    {
        AddComponent<UuvrIl2cppBehaviour>();
    }
}
