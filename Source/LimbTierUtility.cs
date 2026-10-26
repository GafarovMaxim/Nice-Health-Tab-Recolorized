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
        public static bool UseDLCColors         => YAPEMPatchMod.Settings?.UseDLCColors ?? true;

        // DLC Def Collections:
        private static readonly HashSet<string> RoyaltyDefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "StoneskinGland", "ArmorskinGland", "ToughskinGland", "DrillArm", "FieldHand",
            "ElbowBlade", "KneeSpike", "HandTalon", "VenomFangs", "VenomTalon",
            "AestheticShaper", "AestheticNose", "LoveEnhancer", "GastroAnalyzer",
            "Immunoenhancer", "Coagulator", "HealingEnhancer", "CircadianRhythm",
            "CircadianHalfCycler", "LearningAssistant", "Neurocalculator",
            "NuclearStomach", "ReprocessorStomach", "SterilizingStomach"
        };

        private static readonly HashSet<string> BiotechDefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "DetoxifierLung", "DetoxifierKidney", "MechlinkImplant", "Mechlink", "RemoteRepairer"
        };

        private static readonly HashSet<string> AnomalyDefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "AdrenalHeart", "CorrosiveHeart", "MetalbloodHeart", "GhoulBarbs", "GhoulPlating", "Tentacle"
        };

        private static readonly HashSet<string> OdysseyDefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "PilotAssistant", "VacskinGland", "BloodWarmer"
        };

        public static bool CategorizeHediffTier(Hediff hediff, out string tier, out Color color)
        {
            tier = null;
            color = Color.white;
            if (hediff?.def == null) return false;

            string defName = hediff.def.defName ?? "";
            string defLabel = hediff.def.label ?? "";
            string customLabel = hediff.Label ?? "";
            string fullLabel = defLabel + " " + customLabel;
            ThingDef spawnThing = hediff.def.spawnThingOnRemoved;
            TechLevel tech = spawnThing?.techLevel ?? TechLevel.Undefined;

            bool isIntegratedImplants = false;
            if (hediff.def.modContentPack != null)
            {
                string pkg = hediff.def.modContentPack.PackageIdPlayerFacing ?? "";
                string modName = hediff.def.modContentPack.Name ?? "";
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

            bool isAddedOrImplant = hediff is Hediff_AddedPart ||
                                    hediff is Hediff_Implant ||
                                    hediff.def.addedPartProps != null ||
                                    hediff.def.hediffClass == typeof(Hediff_AddedPart) ||
                                    hediff.def.hediffClass == typeof(Hediff_Implant) ||
                                    spawnThing != null;

            string dlcPkg = hediff.def.modContentPack?.PackageIdPlayerFacing?.ToLowerInvariant() ?? "";

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
                    string u = part.untranslatedCustomLabel?.ToLowerInvariant() ?? "";
                    string l = part.Label?.ToLowerInvariant() ?? "";
                    bool matchSide = isLeft ? (u.Contains("left") || l.Contains("left") || l.Contains("лев"))
                                            : (u.Contains("right") || l.Contains("right") || l.Contains("прав"));
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

        public static LimbSlot GetUpperLimb(Pawn pawn, bool isLeft) => GetLimb(pawn, isArm: true, isLeft);
        public static LimbSlot GetLowerLimb(Pawn pawn, bool isLeft) => GetLimb(pawn, isArm: false, isLeft);

        public static bool TryGetDominantTierColor(Pawn pawn, out Color dominantColor)
        {
            dominantColor = Color.white;
            if (pawn?.health?.hediffSet == null) return false;

            // Check if body tinting is enabled in settings
            if (YAPEMPatchMod.Settings != null && !YAPEMPatchMod.Settings.EnableBodyTinting)
            {
                return false;
            }

            int requiredCount = YAPEMPatchMod.Settings != null ? YAPEMPatchMod.Settings.RequiredLimbCount : 3;

            // Exactly 4 limbs: Left Arm, Right Arm, Left Leg, Right Leg
            var slots = new LimbSlot[4]
            {
                GetUpperLimb(pawn, isLeft: true),
                GetUpperLimb(pawn, isLeft: false),
                GetLowerLimb(pawn, isLeft: true),
                GetLowerLimb(pawn, isLeft: false)
            };

            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var colors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);

            foreach (var slot in slots)
            {
                if (slot.HasImplant && !string.IsNullOrEmpty(slot.Tier))
                {
                    if (!counts.ContainsKey(slot.Tier))
                    {
                        counts[slot.Tier] = 0;
                        colors[slot.Tier] = slot.Color;
                    }
                    counts[slot.Tier]++;
                }
            }

            // Rule: requiredCount out of 4 limbs (e.g. >= 3)
            // If multiple tiers meet threshold, choose the one with the highest count
            string bestTier = null;
            int bestCount = 0;

            foreach (var kvp in counts)
            {
                if (kvp.Value >= requiredCount && kvp.Value > bestCount)
                {
                    bestCount = kvp.Value;
                    bestTier = kvp.Key;
                }
            }

            if (bestTier != null)
            {
                dominantColor = colors[bestTier];
                return true;
            }

            return false;
        }

        public static bool TryGetDollLimbColor(Pawn pawn, Doll doll, DollBodyPartDef partDef, out Color limbColor)
        {
            limbColor = Color.white;
            if (pawn?.health?.hediffSet == null || doll == null || partDef == null) return false;

            // Check if bottom icons coloring is enabled in settings
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
