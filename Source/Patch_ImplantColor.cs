using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using NiceHealthTab;

namespace NiceHealthTab_YAPEM_Patch
{
    [HarmonyPatch(typeof(Hediff), "get_LabelColor")]
    public static class Patch_Hediff_LabelColor
    {
        [HarmonyPostfix]
        public static void Postfix(Hediff __instance, ref Color __result)
        {
            if (__instance != null && LimbTierUtility.CategorizeHediffTier(__instance, out _, out Color tierColor))
            {
                __result = tierColor;
            }
        }
    }

    [HarmonyPatch(typeof(DollRenderContext), "FindImplantDefName")]
    public static class Patch_DollRenderContext_FindImplantDefName
    {
        [HarmonyPostfix]
        public static void Postfix(List<Hediff> hediffs, ref string __result, ref Color col)
        {
            if (string.IsNullOrEmpty(__result) || hediffs == null) return;
            string targetDef = __result;
            var h = hediffs.FirstOrDefault(x => x.def?.defName == targetDef);
            if (h != null && LimbTierUtility.CategorizeHediffTier(h, out _, out Color tierColor))
            {
                col = tierColor;
            }
        }
    }

    [HarmonyPatch(typeof(DollDrawer), "FindAddedAndImplantedParts")]
    public static class Patch_DollDrawer_FindAddedAndImplantedParts
    {
        [HarmonyPostfix]
        public static void Postfix(List<Hediff> hediffs, ref string __result, ref Color col)
        {
            if (string.IsNullOrEmpty(__result) || hediffs == null) return;
            string targetDef = __result;
            var h = hediffs.FirstOrDefault(x => x.def?.defName == targetDef);
            if (h != null && LimbTierUtility.CategorizeHediffTier(h, out _, out Color tierColor))
            {
                col = tierColor;
            }
        }
    }
}
