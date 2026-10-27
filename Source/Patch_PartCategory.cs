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
                // Same def-based kind the doll renderer uses, so the list never has a part the doll cannot draw in that view.
                LimbTierUtility.PartKind kind = LimbTierUtility.ClassifyPart(part.def, null, null);
                bool solid = IsInCategory(part, DollFilterMode.Bones, NoHediffs);
                bool show = mode == DollFilterMode.Bones
                    ? solid && kind.IsBone && BoneViewUtil.IsShown(part, hediffs)
                    : IsInCategory(part, DollFilterMode.Organs, NoHediffs) || (solid && (kind.IsOrgan || part.def.defName == "Jaw"));
                if (show) __result.Add(part);
            }
            return false;
        }
    }

    public static class BoneViewUtil
    {
        private static bool Has(string name, string what) => name != null && name.IndexOf(what, StringComparison.OrdinalIgnoreCase) >= 0;

        private static readonly Dictionary<BodyPartDef, bool> coreCache = new Dictionary<BodyPartDef, bool>();

        // Def-only check, cached: the doll renderer asks for every bone on every frame.
        private static bool IsCore(BodyPartDef def)
        {
            if (!coreCache.TryGetValue(def, out bool core))
            {
                core = (def.tags != null && (def.tags.Contains(BodyPartTagDefOf.Spine) || def.tags.Contains(BodyPartTagDefOf.Pelvis)))
                    || Has(def.defName, "Spine") || Has(def.defName, "Pelvis") || Has(def.defName, "Rib");
                coreCache[def] = core;
            }
            return core;
        }

        // Spine, pelvis and ribs are always listed; any other bone only when it has a hediff other than "missing" (injury, fracture, implant).
        // A missing bone (or one under a missing limb) is not interesting.
        public static bool IsShown(BodyPartRecord part, IEnumerable<Hediff> hediffs)
        {
            if (IsCore(part.def)) return true;
            if (hediffs == null) return false;
            foreach (Hediff h in hediffs)
            {
                if (h != null && !(h is Hediff_MissingPart) && h.Part == part) return true;
            }
            return false;
        }
    }
}
