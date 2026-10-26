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

        public static bool CategorizeHediffTier(Hediff hediff, out string tier, out Color color)
        {
            tier = null;
            color = Color.white;
            if (hediff?.def == null) return false;

            string defName = hediff.def.defName ?? "";
            string defLabel = hediff.def.label ?? "";
            string customLabel = hediff.Label ?? "";
            string fullLabel = defLabel + " " + customLabel;
            TechLevel tech = hediff.def.spawnThingOnRemoved?.techLevel ?? TechLevel.Undefined;

            // 1. Archotech (Любые архотек-импланты из любых модов: Archotech Expanded, EPOE, Vanilla и др.)
            if (tech == TechLevel.Archotech ||
                defName.IndexOf("Archo", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fullLabel.IndexOf("archotech", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fullLabel.IndexOf("архотек", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                tier = "Archotech";
                color = ColorArchotech;
                return true;
            }

            // 2. Advanced Bionic (Продвинутая бионика: YAPEM, EPOE-Forked и др.)
            if (defName.IndexOf("AdvancedBionic", StringComparison.OrdinalIgnoreCase) >= 0 ||
                defName.IndexOf("BIE_Advanced", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fullLabel.IndexOf("advanced bionic", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fullLabel.IndexOf("продвинут", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                tier = "AdvancedBionic";
                color = ColorAdvancedBionic;
                return true;
            }

            // 3. Ultratech (Ультратехнологические протезы и импланты)
            if (defName.IndexOf("Ultra", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fullLabel.IndexOf("ultratech", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fullLabel.IndexOf("ультратек", StringComparison.OrdinalIgnoreCase) >= 0 ||
                (tech == TechLevel.Ultra && defName.IndexOf("Bionic", StringComparison.OrdinalIgnoreCase) < 0 && fullLabel.IndexOf("bionic", StringComparison.OrdinalIgnoreCase) < 0))
            {
                tier = "Ultratech";
                color = ColorUltratech;
                return true;
            }

            // 4. Standard Bionic (Стандартная бионика)
            if (defName.IndexOf("Bionic", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fullLabel.IndexOf("bionic", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fullLabel.IndexOf("бионич", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                tier = "Bionic";
                color = ColorBionic;
                return true;
            }

            // 5. Simple Prosthetic (Простые протезы)
            if (defName.IndexOf("Prosthetic", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fullLabel.IndexOf("prosthetic", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fullLabel.IndexOf("протез", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                tier = "Prosthetic";
                color = ColorProsthetic;
                return true;
            }

            return false;
        }

        public static LimbSlot GetUpperLimb(Pawn pawn, bool isLeft)
        {
            if (pawn?.health?.hediffSet == null) return default;

            foreach (var h in pawn.health.hediffSet.hediffs)
            {
                if (h.Part == null) continue;
                if (!(h is Hediff_AddedPart || h.def.countsAsAddedPartOrImplant)) continue;

                var part = h.Part;
                // In RimWorld, arm prosthetics can be installed on Shoulder, Arm, or Hand
                if (part.def == BodyPartDefOf.Shoulder || part.def == BodyPartDefOf.Arm || part.def == BodyPartDefOf.Hand)
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

        public static LimbSlot GetLowerLimb(Pawn pawn, bool isLeft)
        {
            if (pawn?.health?.hediffSet == null) return default;

            foreach (var h in pawn.health.hediffSet.hediffs)
            {
                if (h.Part == null) continue;
                if (!(h is Hediff_AddedPart || h.def.countsAsAddedPartOrImplant)) continue;

                var part = h.Part;
                // In RimWorld, leg prosthetics can be installed on Leg or Foot
                if (part.def == BodyPartDefOf.Leg || part.def.defName == "Foot")
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
