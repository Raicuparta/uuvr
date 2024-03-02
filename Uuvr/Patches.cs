using HarmonyLib;
using UnityEngine;

namespace Uuvr;

[HarmonyPatch]
public static class Patches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Camera), "set_fieldOfView")]
    // Unity already prevents this, but it also nags you constantly about it.
    // Some games try to change the FOV every frame, and all those logs can reduce performance.
    private static bool PreventChangingFov()
    {
        return false;
    }
}
