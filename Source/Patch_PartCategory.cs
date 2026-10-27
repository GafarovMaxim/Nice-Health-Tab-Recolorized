using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using NiceHealthTab;

namespace NiceHealthTab_YAPEM_Patch
{
    // The part list next to the doll uses DollBodyPartCategory.GetParts. The original treats a part as a bone ("solid")
    // when its added part is solid, so replaced organs (bionic kidney, stomach, lung) landed in the bones list.
    // Here: bones/organs are classified by the natural part, and the bones view shows only core bones plus damaged or replaced ones.
    [HarmonyPatch(typeof(DollBodyPartCategory), nameof(DollBodyPartCategory.GetParts))]
    public static class Patch_PartCategory
    {
        private static readonly Func<BodyPartRecord, DollFilterMode, List<Hediff>, bool> IsInCategory =
            AccessTools.MethodDelegate<Func<BodyPartRecord, DollFilterMode, List<Hediff>, bool>>(
                AccessTools.Method(typeof(DollBodyPartCategory), "IsInCategory"));

        private static readonly List<Hediff> NoHediffs = new List<Hediff>();

        public static bool Prefix(Pawn pawn, DollFilterMode mode, ref HashSet<BodyPartRecord> __result)
        {
            __result = new HashSet<BodyPartRecord>();
            if ((mode != DollFilterMode.Bones && mode != DollFilterMode.Organs) || pawn?.RaceProps?.body == null || pawn.health?.hediffSet == null)
            {
                return false;
            }

            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            foreach (BodyPartRecord part in pawn.RaceProps.body.AllParts)
            {
                if (!IsInCategory(part, mode, NoHediffs)) continue;
                if (mode == DollFilterMode.Bones && !BoneViewUtil.IsShown(part, hediffs)) continue;
                __result.Add(part);
            }
            return false;
        }
    }

    public static class BoneViewUtil
    {
        private static bool Has(string name, string what) => name != null && name.IndexOf(what, StringComparison.OrdinalIgnoreCase) >= 0;

        // Spine, pelvis and ribs are always listed; any other bone only when it has a hediff other than "missing" (injury, fracture, implant).
        // A missing bone (or one under a missing limb) is not interesting.
        public static bool IsShown(BodyPartRecord part, IEnumerable<Hediff> hediffs)
        {
            BodyPartDef def = part.def;
            if (def.tags != null && (def.tags.Contains(BodyPartTagDefOf.Spine) || def.tags.Contains(BodyPartTagDefOf.Pelvis))) return true;
            if (Has(def.defName, "Spine") || Has(def.defName, "Pelvis") || Has(def.defName, "Rib")) return true;
            if (hediffs == null) return false;
            foreach (Hediff h in hediffs)
            {
                if (h != null && !(h is Hediff_MissingPart) && h.Part == part) return true;
            }
            return false;
        }
    }
}
