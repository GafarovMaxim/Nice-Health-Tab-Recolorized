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
            ApplyTierColor(hediffs, __result, ref col);
        }

        // Plain loop instead of FirstOrDefault: avoids a closure allocation on every doll part draw.
        internal static void ApplyTierColor(List<Hediff> hediffs, string defName, ref Color col)
        {
            for (int i = 0; i < hediffs.Count; i++)
            {
                Hediff h = hediffs[i];
                if (h.def?.defName != defName) continue;
                if (LimbTierUtility.CategorizeHediffTier(h, out _, out Color tierColor))
                {
                    col = tierColor;
                }
                return;
            }
        }
    }

    [HarmonyPatch(typeof(DollDrawer), "FindAddedAndImplantedParts")]
    public static class Patch_DollDrawer_FindAddedAndImplantedParts
    {
        [HarmonyPostfix]
        public static void Postfix(List<Hediff> AllPartHediffs, ref string __result, ref Color col)
        {
            if (string.IsNullOrEmpty(__result) || AllPartHediffs == null) return;
            Patch_DollRenderContext_FindImplantDefName.ApplyTierColor(AllPartHediffs, __result, ref col);
        }
    }
}
