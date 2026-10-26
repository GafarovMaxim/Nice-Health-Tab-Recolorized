using System;
using HarmonyLib;
using Verse;

namespace NiceHealthTab_YAPEM_Patch
{
    [StaticConstructorOnStartup]
    public static class ModInit
    {
        static ModInit()
        {
            var harmony = new Harmony("rimworld.nicehealthtab.recolorized");
            harmony.PatchAll();
            Log.Message("[NiceHealthTab: Recolorized] Initialized Harmony patches successfully.");
        }
    }
}
