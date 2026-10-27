using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using NiceHealthTab;

namespace NiceHealthTab_YAPEM_Patch
{
    public struct LimbSlot
    {
        public bool HasImplant;
        public string Tier;
        public Color Color;
    }

    public static class LimbTierUtility
    {
        // Colors dynamically read from mod settings with default fallbacks:
        public static Color ColorAdvancedBionic => YAPEMPatchMod.Settings?.ColorAdvancedBionic ?? YAPEMPatchSettings.DefaultColorAdvancedBionic;
        public static Color ColorUltratech      => YAPEMPatchMod.Settings?.ColorUltratech ?? YAPEMPatchSettings.DefaultColorUltratech;
        public static Color ColorArchotech      => YAPEMPatchMod.Settings?.ColorArchotech ?? YAPEMPatchSettings.DefaultColorArchotech;
        public static Color ColorBionic         => YAPEMPatchMod.Settings?.ColorBionic ?? YAPEMPatchSettings.DefaultColorBionic;
        public static Color ColorProsthetic     => YAPEMPatchMod.Settings?.ColorProsthetic ?? YAPEMPatchSettings.DefaultColorProsthetic;

        // DLC Category colors:
        public static Color ColorRoyalty        => YAPEMPatchMod.Settings?.ColorRoyalty ?? YAPEMPatchSettings.DefaultColorRoyalty;
        public static Color ColorBiotech        => YAPEMPatchMod.Settings?.ColorBiotech ?? YAPEMPatchSettings.DefaultColorBiotech;
        public static Color ColorAnomaly        => YAPEMPatchMod.Settings?.ColorAnomaly ?? YAPEMPatchSettings.DefaultColorAnomaly;
        public static Color ColorOdyssey        => YAPEMPatchMod.Settings?.ColorOdyssey ?? YAPEMPatchSettings.DefaultColorOdyssey;
        public static Color ColorIntegratedImplants => YAPEMPatchMod.Settings?.ColorIntegratedImplants ?? YAPEMPatchSettings.DefaultColorIntegratedImplants;
        public static bool UseDLCColors         => YAPEMPatchMod.Settings?.UseDLCColors ?? true;

        // DLC Def Collections:
        public static readonly HashSet<string> RoyaltyDefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "StoneskinGland", "ArmorskinGland", "ToughskinGland", "DrillArm", "FieldHand",
            "ElbowBlade", "KneeSpike", "HandTalon", "VenomFangs", "VenomTalon",
            "AestheticShaper", "AestheticNose", "LoveEnhancer", "GastroAnalyzer",
            "Immunoenhancer", "Coagulator", "HealingEnhancer", "CircadianRhythm",
            "CircadianHalfCycler", "LearningAssistant", "Neurocalculator",
            "NuclearStomach", "ReprocessorStomach", "SterilizingStomach"
        };

        public static readonly HashSet<string> BiotechDefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "DetoxifierLung", "DetoxifierKidney", "MechlinkImplant", "Mechlink", "RemoteRepairer"
        };

        public static readonly HashSet<string> AnomalyDefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "AdrenalHeart", "CorrosiveHeart", "MetalbloodHeart", "GhoulBarbs", "GhoulPlating", "Tentacle"
        };

        public static readonly HashSet<string> OdysseyDefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "PilotAssistant", "VacskinGland", "BloodWarmer"
        };

        public static bool TryGetTierColor(string tierName, out Color color)
        {
            switch (tierName)
            {
                case "Archotech":
                    color = ColorArchotech;
                    return true;
                case "AdvancedBionic":
                case "AdvBionic":
                    color = ColorAdvancedBionic;
                    return true;
                case "Ultratech":
                    color = ColorUltratech;
                    return true;
                case "Bionic":
                    color = ColorBionic;
                    return true;
                case "Prosthetic":
                    color = ColorProsthetic;
                    return true;
                case "Royalty":
                    color = ColorRoyalty;
                    return true;
                case "Biotech":
                    color = ColorBiotech;
                    return true;
                case "Anomaly":
                    color = ColorAnomaly;
                    return true;
                case "Odyssey":
                    color = ColorOdyssey;
                    return true;
                case "IntegratedImplants":
                case "Integrated":
                    color = ColorIntegratedImplants;
                    return true;
                default:
                    color = Color.white;
                    return false;
            }
        }

        public static string GetTierLabel(string tierName)
        {
            switch (tierName)
            {
                case "Archotech": return "NHT_YAPEM_TierArchotech".Translate();
                case "AdvancedBionic":
                case "AdvBionic": return "NHT_YAPEM_TierAdvBionic".Translate();
                case "Ultratech": return "NHT_YAPEM_TierUltratech".Translate();
                case "Bionic": return "NHT_YAPEM_TierBionic".Translate();
                case "Prosthetic": return "NHT_YAPEM_TierProsthetic".Translate();
                case "Royalty": return "NHT_YAPEM_TierRoyalty".Translate();
                case "Biotech": return "NHT_YAPEM_TierBiotech".Translate();
                case "Anomaly": return "NHT_YAPEM_TierAnomaly".Translate();
                case "Odyssey": return "NHT_YAPEM_TierOdyssey".Translate();
                case "IntegratedImplants":
                case "Integrated": return "NHT_YAPEM_TierIntegrated".Translate();
                case "None": return "NHT_YAPEM_TierNone".Translate();
                case "Auto": return "NHT_YAPEM_TierAuto".Translate();
                default: return tierName;
            }
        }

        public static bool CategorizeHediffTier(Hediff hediff, out string tier, out Color color)
        {
            tier = null;
            color = Color.white;
            if (hediff?.def == null) return false;

            // 0. User custom category override via Mod Settings (fine-tuning)
            if (YAPEMPatchMod.Settings?.CustomHediffTiers != null &&
                YAPEMPatchMod.Settings.CustomHediffTiers.TryGetValue(hediff.def.defName, out string overrideTier))
            {
                if (overrideTier == "None")
                {
                    tier = "None";
                    color = Color.white;
                    return false; // Visual recoloring disabled for this implant
                }
                if (overrideTier != "Auto" && TryGetTierColor(overrideTier, out Color overrideColor))
                {
                    tier = overrideTier;
                    color = overrideColor;
                    return true;
                }
            }

            return CategorizeAutoCached(hediff.def, out tier, out color);
        }

        public static bool CategorizeHediffDefTier(HediffDef def, out string tier, out Color color)
        {
            tier = null;
            color = Color.white;
            if (def == null) return false;

            if (YAPEMPatchMod.Settings?.CustomHediffTiers != null &&
                YAPEMPatchMod.Settings.CustomHediffTiers.TryGetValue(def.defName, out string overrideTier))
            {
                if (overrideTier == "None")
                {
                    tier = "None";
                    color = Color.white;
                    return false;
                }
                if (overrideTier != "Auto" && TryGetTierColor(overrideTier, out Color overrideColor))
                {
                    tier = overrideTier;
                    color = overrideColor;
                    return true;
                }
            }

            return CategorizeAutoCached(def, out tier, out color);
        }

        // Auto tier depends only on the def and UseDLCColors; colors are resolved live so palette edits need no invalidation.
        private static readonly Dictionary<HediffDef, string> autoTierCache = new Dictionary<HediffDef, string>();
        private static bool autoTierCacheUseDLC = true;

        private static bool CategorizeAutoCached(HediffDef def, out string tier, out Color color)
        {
            bool useDLC = UseDLCColors;
            if (useDLC != autoTierCacheUseDLC)
            {
                autoTierCache.Clear();
                autoTierCacheUseDLC = useDLC;
            }
            if (!autoTierCache.TryGetValue(def, out tier))
            {
                CategorizeDefInternal(def, out tier, out _);
                autoTierCache[def] = tier;
            }
            if (tier == null)
            {
                color = Color.white;
                return false;
            }
            return TryGetTierColor(tier, out color);
        }

        private static bool CategorizeDefInternal(HediffDef def, out string tier, out Color color)
        {
            tier = null;
            color = Color.white;
            if (def == null) return false;

            string defName = def.defName ?? "";
            string fullLabel = def.label ?? "";
            ThingDef spawnThing = def.spawnThingOnRemoved;
            TechLevel tech = spawnThing?.techLevel ?? TechLevel.Undefined;

            bool isIntegratedImplants = false;
            if (def.modContentPack != null)
            {
                string pkg = def.modContentPack.PackageIdPlayerFacing ?? "";
                string modName = def.modContentPack.Name ?? "";
                if (pkg.Equals("lts.i", StringComparison.OrdinalIgnoreCase) ||
                    modName.IndexOf("Integrated Implants", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    isIntegratedImplants = true;
                }
            }

            bool hasArchoCategory = false;
            bool hasUltraCategory = false;
            bool hasBionicCategory = false;
            bool hasProstheticCategory = false;

            if (spawnThing?.thingCategories != null)
            {
                foreach (var cat in spawnThing.thingCategories)
                {
                    if (cat == null) continue;
                    string catName = cat.defName ?? "";
                    if (catName.IndexOf("Archotech", StringComparison.OrdinalIgnoreCase) >= 0) hasArchoCategory = true;
                    if (catName.IndexOf("Ultra", StringComparison.OrdinalIgnoreCase) >= 0) hasUltraCategory = true;
                    if (catName.IndexOf("Bionic", StringComparison.OrdinalIgnoreCase) >= 0) hasBionicCategory = true;
                    if (catName.IndexOf("Prosthetic", StringComparison.OrdinalIgnoreCase) >= 0) hasProstheticCategory = true;
                }
            }

            bool isAddedOrImplant = def.countsAsAddedPartOrImplant ||
                                    def.addedPartProps != null ||
                                    (def.hediffClass != null && (typeof(Hediff_AddedPart).IsAssignableFrom(def.hediffClass) || typeof(Hediff_Implant).IsAssignableFrom(def.hediffClass))) ||
                                    spawnThing != null;

            string dlcPkg = def.modContentPack?.PackageIdPlayerFacing?.ToLowerInvariant() ?? "";

            // Dedicated DLC Categories (Royalty, Biotech, Anomaly, Odyssey)
            if (UseDLCColors && isAddedOrImplant)
            {
                if (dlcPkg == "ludeon.rimworld.royalty" || RoyaltyDefs.Contains(defName))
                {
                    tier = "Royalty";
                    color = ColorRoyalty;
                    return true;
                }

                if (dlcPkg == "ludeon.rimworld.biotech" || BiotechDefs.Contains(defName) || defName.StartsWith("ControlSublink", StringComparison.OrdinalIgnoreCase))
                {
                    tier = "Biotech";
                    color = ColorBiotech;
                    return true;
                }

                if (dlcPkg == "ludeon.rimworld.anomaly" || AnomalyDefs.Contains(defName) || defName.StartsWith("Fleshmass", StringComparison.OrdinalIgnoreCase))
                {
                    tier = "Anomaly";
                    color = ColorAnomaly;
                    return true;
                }

                if (dlcPkg == "ludeon.rimworld.odyssey" || OdysseyDefs.Contains(defName))
                {
                    tier = "Odyssey";
                    color = ColorOdyssey;
                    return true;
                }
            }

            // Dedicated Integrated Implants Category
            if (isIntegratedImplants && isAddedOrImplant)
            {
                tier = "IntegratedImplants";
                color = ColorIntegratedImplants;
                return true;
            }

            // 1. Archotech (Archotech Expanded, Vanilla, Royalty Royal implants, Biotech Mechlink, etc.)
            if (tech == TechLevel.Archotech ||
                hasArchoCategory ||
                defName.IndexOf("Archo", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fullLabel.IndexOf("archotech", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fullLabel.IndexOf("архотек", StringComparison.OrdinalIgnoreCase) >= 0 ||
                defName.Equals("MechlinkImplant", StringComparison.OrdinalIgnoreCase) ||
                defName.Equals("Mechlink", StringComparison.OrdinalIgnoreCase) ||
                (spawnThing?.techHediffsTags?.Contains("ImplantEmpireRoyal") == true))
            {
                tier = "Archotech";
                color = ColorArchotech;
                return true;
            }

            // 2. Advanced Bionic (YAPEM, EPOE-Forked и др.)
            if (defName.IndexOf("AdvancedBionic", StringComparison.OrdinalIgnoreCase) >= 0 ||
                defName.IndexOf("BIE_Advanced", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fullLabel.IndexOf("advanced bionic", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fullLabel.IndexOf("продвинут", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                tier = "AdvancedBionic";
                color = ColorAdvancedBionic;
                return true;
            }

            // 3. Ultratech (Ультратехнологические протезы и импланты, железы брони Royalty, саблинки Biotech, Vacskin Odyssey)
            if (defName.IndexOf("Ultra", StringComparison.OrdinalIgnoreCase) >= 0 ||
                hasUltraCategory ||
                fullLabel.IndexOf("ultratech", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fullLabel.IndexOf("ультратек", StringComparison.OrdinalIgnoreCase) >= 0 ||
                defName.Equals("StoneskinGland", StringComparison.OrdinalIgnoreCase) ||
                defName.Equals("ArmorskinGland", StringComparison.OrdinalIgnoreCase) ||
                defName.Equals("ToughskinGland", StringComparison.OrdinalIgnoreCase) ||
                defName.Equals("VacskinGland", StringComparison.OrdinalIgnoreCase) ||
                defName.StartsWith("ControlSublink", StringComparison.OrdinalIgnoreCase) ||
                defName.Equals("RemoteRepairer", StringComparison.OrdinalIgnoreCase) ||
                (tech == TechLevel.Ultra && defName.IndexOf("Bionic", StringComparison.OrdinalIgnoreCase) < 0 && fullLabel.IndexOf("bionic", StringComparison.OrdinalIgnoreCase) < 0))
            {
                tier = "Ultratech";
                color = ColorUltratech;
                return true;
            }

            // 4. Standard Bionic (Стандартная бионика, Royalty, Biotech, Anomaly, Odyssey, Integrated Implants)
            if (defName.IndexOf("Bionic", StringComparison.OrdinalIgnoreCase) >= 0 ||
                hasBionicCategory ||
                fullLabel.IndexOf("bionic", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fullLabel.IndexOf("бионич", StringComparison.OrdinalIgnoreCase) >= 0 ||
                RoyaltyDefs.Contains(defName) ||
                BiotechDefs.Contains(defName) ||
                AnomalyDefs.Contains(defName) ||
                OdysseyDefs.Contains(defName) ||
                // Integrated Implants & spacer implants:
                (isIntegratedImplants && isAddedOrImplant) ||
                (tech == TechLevel.Spacer && isAddedOrImplant) ||
                (spawnThing?.techHediffsTags?.Contains("ImplantEmpireCommon") == true) ||
                (spawnThing?.techHediffsTags?.Contains("Advanced") == true && isAddedOrImplant))
            {
                tier = "Bionic";
                color = ColorBionic;
                return true;
            }

            // 5. Simple Prosthetic (Простые протезы, Anomaly мутации)
            if (defName.IndexOf("Prosthetic", StringComparison.OrdinalIgnoreCase) >= 0 ||
                hasProstheticCategory ||
                fullLabel.IndexOf("prosthetic", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fullLabel.IndexOf("протез", StringComparison.OrdinalIgnoreCase) >= 0 ||
                defName.IndexOf("Wooden", StringComparison.OrdinalIgnoreCase) >= 0 ||
                defName.IndexOf("PegLeg", StringComparison.OrdinalIgnoreCase) >= 0 ||
                defName.IndexOf("Hook", StringComparison.OrdinalIgnoreCase) >= 0 ||
                defName.IndexOf("Tentacle", StringComparison.OrdinalIgnoreCase) >= 0 ||
                defName.IndexOf("Fleshmass", StringComparison.OrdinalIgnoreCase) >= 0 ||
                defName.IndexOf("Cochlear", StringComparison.OrdinalIgnoreCase) >= 0 ||
                (tech <= TechLevel.Industrial && isAddedOrImplant))
            {
                tier = "Prosthetic";
                color = ColorProsthetic;
                return true;
            }

            return false;
        }

        public static LimbSlot GetLimb(Pawn pawn, bool isArm, bool isLeft)
        {
            if (pawn?.health?.hediffSet == null) return default;

            foreach (var h in pawn.health.hediffSet.hediffs)
            {
                if (h.Part == null) continue;
                if (!(h is Hediff_AddedPart || h.def.countsAsAddedPartOrImplant)) continue;

                var part = h.Part;
                bool isTargetPart = isArm
                    ? (part.def == BodyPartDefOf.Shoulder || part.def == BodyPartDefOf.Arm || part.def == BodyPartDefOf.Hand)
                    : (part.def == BodyPartDefOf.Leg || part.def.defName == "Foot");

                if (isTargetPart)
                {
                    string u = part.untranslatedCustomLabel ?? "";
                    string l = part.Label ?? "";
                    bool matchSide = isLeft ? (ContainsIgnoreCase(u, "left") || ContainsIgnoreCase(l, "left") || ContainsIgnoreCase(l, "лев"))
                                            : (ContainsIgnoreCase(u, "right") || ContainsIgnoreCase(l, "right") || ContainsIgnoreCase(l, "прав"));
                    if (matchSide)
                    {
                        if (CategorizeHediffTier(h, out string tier, out Color col))
                        {
                            return new LimbSlot { HasImplant = true, Tier = tier, Color = col };
                        }
                    }
                }
            }

            return default;
        }

        private static bool ContainsIgnoreCase(string s, string value) => s.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;

        // Limbs of one pawn, computed once per frame: every doll part asks for them during the same OnGUI pass.
        // Order: upper left, upper right, lower left, lower right.
        private static readonly LimbSlot[] cachedLimbs = new LimbSlot[4];
        private static Pawn cachedLimbsPawn;
        private static int cachedLimbsFrame = -1;

        private static LimbSlot[] GetLimbs(Pawn pawn)
        {
            if (pawn != cachedLimbsPawn || Time.frameCount != cachedLimbsFrame)
            {
                cachedLimbs[0] = GetLimb(pawn, isArm: true, isLeft: true);
                cachedLimbs[1] = GetLimb(pawn, isArm: true, isLeft: false);
                cachedLimbs[2] = GetLimb(pawn, isArm: false, isLeft: true);
                cachedLimbs[3] = GetLimb(pawn, isArm: false, isLeft: false);
                cachedLimbsPawn = pawn;
                cachedLimbsFrame = Time.frameCount;
            }
            return cachedLimbs;
        }

        public static LimbSlot GetUpperLimb(Pawn pawn, bool isLeft) => GetLimbs(pawn)[isLeft ? 0 : 1];
        public static LimbSlot GetLowerLimb(Pawn pawn, bool isLeft) => GetLimbs(pawn)[isLeft ? 2 : 3];

        public static bool TryGetDominantTierColor(Pawn pawn, out Color dominantColor)
        {
            dominantColor = Color.white;
            if (YAPEMPatchMod.Settings != null && !YAPEMPatchMod.Settings.EnableBodyTinting)
            {
                return false;
            }

            int reqCount = YAPEMPatchMod.Settings?.RequiredLimbCount ?? 3;
            LimbSlot[] limbs = GetLimbs(pawn);

            // First tier (in limb order) reaching the threshold wins, same as the old first-seen dictionary order.
            for (int i = 0; i < limbs.Length; i++)
            {
                if (!limbs[i].HasImplant || string.IsNullOrEmpty(limbs[i].Tier)) continue;
                int count = 0;
                for (int j = 0; j < limbs.Length; j++)
                {
                    if (limbs[j].HasImplant && limbs[j].Tier == limbs[i].Tier) count++;
                }
                if (count >= reqCount)
                {
                    dominantColor = limbs[i].Color;
                    return true;
                }
            }

            return false;
        }

        public struct PartKind
        {
            public bool IsSpine, IsNeck, IsBone, IsOrgan, IsEye;
        }

        // Part kind depends only on defs, so it is computed once per (body part, doll part, rendered part) combination.
        private static readonly Dictionary<(BodyPartDef, DollBodyPartDef, DollBodyPartDef), PartKind> partKindCache =
            new Dictionary<(BodyPartDef, DollBodyPartDef, DollBodyPartDef), PartKind>();

        public static PartKind ClassifyPart(BodyPartDef bodyDef, DollBodyPartDef part, DollBodyPartDef effectivePart)
        {
            var key = (bodyDef, part, effectivePart);
            if (!partKindCache.TryGetValue(key, out PartKind kind))
            {
                kind = ComputePartKind(bodyDef, part, effectivePart);
                partKindCache[key] = kind;
            }
            return kind;
        }

        private static PartKind ComputePartKind(BodyPartDef bodyDef, DollBodyPartDef part, DollBodyPartDef effectivePart)
        {
            string bodyName = bodyDef?.defName ?? "";
            string partName = part?.defName ?? "";
            string effectiveName = effectivePart?.defName ?? "";
            var tags = bodyDef?.tags;

            bool isSpine = (tags != null && tags.Contains(BodyPartTagDefOf.Spine))
                || ContainsIgnoreCase(bodyName, "Spine")
                || ContainsIgnoreCase(partName, "Spine")
                || ContainsIgnoreCase(effectiveName, "Spine");

            // Neck is part of the body silhouette (like Torso or Head). It is NOT an organ or bone.
            bool isNeck = bodyDef == BodyPartDefOf.Neck
                || ContainsIgnoreCase(bodyName, "Neck")
                || ContainsIgnoreCase(partName, "Neck");

            bool isBone = part is DollBone
                || effectivePart is DollBone
                || isSpine
                || (tags != null && tags.Contains(BodyPartTagDefOf.Pelvis))
                || ContainsIgnoreCase(bodyName, "Bone")
                || ContainsIgnoreCase(bodyName, "Clavicle")
                || ContainsIgnoreCase(bodyName, "Ribcage")
                || ContainsIgnoreCase(bodyName, "Pelvis")
                || ContainsIgnoreCase(bodyName, "Sternum")
                || ContainsIgnoreCase(bodyName, "Skull")
                || ContainsIgnoreCase(partName, "Bone")
                || ContainsIgnoreCase(partName, "Ribcage")
                || ContainsIgnoreCase(partName, "Pelvis");

            // Organs strictly exclude bones and the neck
            bool isOrgan = !isBone && !isNeck && (
                part is DollOrgan
                || effectivePart is DollOrgan
                || bodyDef == BodyPartDefOf.Heart
                || bodyDef == BodyPartDefOf.Lung
                || (tags != null && (
                    tags.Contains(BodyPartTagDefOf.BloodFiltrationSource) ||
                    tags.Contains(BodyPartTagDefOf.BloodFiltrationLiver) ||
                    tags.Contains(BodyPartTagDefOf.BloodFiltrationKidney) ||
                    tags.Contains(BodyPartTagDefOf.BloodPumpingSource) ||
                    tags.Contains(BodyPartTagDefOf.BreathingSource) ||
                    tags.Contains(BodyPartTagDefOf.ConsciousnessSource) ||
                    tags.Contains(BodyPartTagDefOf.MetabolismSource)))
                || IsOrganName(bodyName)
                || ContainsIgnoreCase(partName, "Organ")
                || IsOrganName(partName));

            return new PartKind
            {
                IsSpine = isSpine,
                IsNeck = isNeck,
                IsBone = isBone,
                IsOrgan = isOrgan,
                IsEye = part is DollEye || effectivePart is DollEye
            };
        }

        private static bool IsOrganName(string name) =>
            ContainsIgnoreCase(name, "Stomach") ||
            ContainsIgnoreCase(name, "Heart") ||
            ContainsIgnoreCase(name, "Liver") ||
            ContainsIgnoreCase(name, "Kidney") ||
            ContainsIgnoreCase(name, "Lung") ||
            ContainsIgnoreCase(name, "Brain") ||
            ContainsIgnoreCase(name, "Nose");

        public static bool TryGetDollLimbColor(Pawn pawn, Doll doll, DollBodyPartDef partDef, out Color limbColor)
        {
            limbColor = Color.white;
            if (pawn == null || doll == null || partDef == null) return false;

            if (YAPEMPatchMod.Settings != null && !YAPEMPatchMod.Settings.ColorBottomIcons)
            {
                return false;
            }

            string name = partDef.defName ?? "";
            bool isLeft = name.EndsWith("Left") || name.EndsWith("LeftToe");
            bool isRight = name.EndsWith("Right") || name.EndsWith("RightToe");

            if (!isLeft && !isRight) return false;

            if (doll is HandDoll)
            {
                var slot = GetUpperLimb(pawn, isLeft);
                if (slot.HasImplant)
                {
                    limbColor = slot.Color;
                    return true;
                }
            }
            else if (doll is FeetDoll)
            {
                var slot = GetLowerLimb(pawn, isLeft);
                if (slot.HasImplant)
                {
                    limbColor = slot.Color;
                    return true;
                }
            }

            return false;
        }

        public static bool IsBodySubstratePart(DollBodyPartDef partDef)
        {
            if (partDef == null) return false;
            // Must NOT be an organ, bone, eye, or prosthesis
            if (partDef is DollOrgan || partDef is DollBone || partDef is DollEye || partDef is DollPartProsthesis)
            {
                return false;
            }
            return true;
        }
    }
}
