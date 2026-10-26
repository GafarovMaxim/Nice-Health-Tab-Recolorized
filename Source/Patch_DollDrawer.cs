using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using NiceHealthTab;

namespace NiceHealthTab_YAPEM_Patch
{
    [HarmonyPatch(typeof(DollDrawer), "DrawPart")]
    public static class Patch_DollDrawer_DrawPart
    {
        private static readonly MethodInfo DrawHeadOverlayMethod = 
            AccessTools.Method(typeof(DollDrawer), "DrawHeadOverlay");

        private static readonly MethodInfo GetPartConditionLabelMethod =
            AccessTools.Method(typeof(DollDrawer), "_GetPartConditionLabel");

        [HarmonyPrefix]
        public static bool Prefix(BodyPartIndexesRemap BodyPartIndexRemapper, DollBodyPartDef _part, Pawn pawn, Doll doll, Rect rectDoll, float DollScale, bool Selectable, Vector2 ExtraOffset)
        {
            if (pawn == null || _part == null || doll == null || BodyPartIndexRemapper == null)
            {
                return true;
            }

            int num = BodyPartIndexRemapper.Get(_part.bodyPartId);
            if (num < 0)
            {
                return true;
            }

            BodyDef body = pawn.RaceProps?.body;
            BodyPartRecord val = (body != null) ? body.GetPartAtIndex(num) : null;
            if (val == null)
            {
                return true;
            }

            bool isSpine = (val.def != null && val.def.tags != null && val.def.tags.Contains(BodyPartTagDefOf.Spine))
                || (val.def != null && val.def.defName.IndexOf("Spine", StringComparison.OrdinalIgnoreCase) >= 0)
                || (_part != null && _part.defName.IndexOf("Spine", StringComparison.OrdinalIgnoreCase) >= 0);

            bool isBone = (_part is DollBone)
                || isSpine
                || (val.def != null && val.def.tags != null && val.def.tags.Contains(BodyPartTagDefOf.Pelvis))
                || (val.def != null && (
                    val.def.defName.IndexOf("Bone", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    val.def.defName.IndexOf("Clavicle", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    val.def.defName.IndexOf("Ribcage", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    val.def.defName.IndexOf("Pelvis", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    val.def.defName.IndexOf("Sternum", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    val.def.defName.IndexOf("Skull", StringComparison.OrdinalIgnoreCase) >= 0
                ))
                || (_part != null && (
                    _part.defName.IndexOf("Bone", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    _part.defName.IndexOf("Ribcage", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    _part.defName.IndexOf("Pelvis", StringComparison.OrdinalIgnoreCase) >= 0
                ));

            bool isOrgan = !isBone && (
                (_part is DollOrgan)
                || (val.def == BodyPartDefOf.Heart)
                || (val.def == BodyPartDefOf.Lung)
                || (val.def != null && (
                    (val.def.tags != null && (
                        val.def.tags.Contains(BodyPartTagDefOf.BloodFiltrationSource) ||
                        val.def.tags.Contains(BodyPartTagDefOf.BloodFiltrationLiver) ||
                        val.def.tags.Contains(BodyPartTagDefOf.BloodFiltrationKidney) ||
                        val.def.tags.Contains(BodyPartTagDefOf.BloodPumpingSource) ||
                        val.def.tags.Contains(BodyPartTagDefOf.BreathingSource) ||
                        val.def.tags.Contains(BodyPartTagDefOf.BreathingPathway) ||
                        val.def.tags.Contains(BodyPartTagDefOf.ConsciousnessSource) ||
                        val.def.tags.Contains(BodyPartTagDefOf.MetabolismSource)
                    )) ||
                    val.def.defName.IndexOf("Stomach", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    val.def.defName.IndexOf("Heart", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    val.def.defName.IndexOf("Liver", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    val.def.defName.IndexOf("Kidney", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    val.def.defName.IndexOf("Lung", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    val.def.defName.IndexOf("Brain", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    val.def.defName.IndexOf("Nose", StringComparison.OrdinalIgnoreCase) >= 0
                ))
                || (_part != null && (
                    _part.defName.IndexOf("Organ", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    _part.defName.IndexOf("Stomach", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    _part.defName.IndexOf("Heart", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    _part.defName.IndexOf("Liver", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    _part.defName.IndexOf("Kidney", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    _part.defName.IndexOf("Lung", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    _part.defName.IndexOf("Brain", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    _part.defName.IndexOf("Nose", StringComparison.OrdinalIgnoreCase) >= 0
                ))
            );

            bool isEye = _part is DollEye;

            // Strict Filter Conformance:
            // Spines only show when bones are viewed!
            if (isSpine && (!DollDrawer.ShowBones || DollDrawer.ShowArmorCover))
            {
                return false;
            }
            if (isOrgan && (YAPEMPatchMod.Settings?.OnlyShowOrgansInOrganMode == true || !DollDrawer.ShowOrgan || DollDrawer.ShowArmorCover))
            {
                return false;
            }
            if (isBone && (!DollDrawer.ShowBones || DollDrawer.ShowArmorCover))
            {
                return false;
            }
            if (isEye && !DollDrawer.ShowEyes)
            {
                return false;
            }

            // 0. If _part is a prosthesis, ensure PartColor matches current settings and draw directly
            if (_part is DollPartProsthesis directProsthesis && val != null)
            {
                directProsthesis.overrideColor = false;
                var hediffs = DollDrawer.Hediffs.GetHediffs(val);
                if (hediffs != null)
                {
                    foreach (var h in hediffs)
                    {
                        if (LimbTierUtility.CategorizeHediffTier(h, out _, out Color tierColor))
                        {
                            _part.PartColor = tierColor;
                            DrawCustomPart(_part, doll, val, rectDoll, ExtraOffset, Selectable, tierColor, pawn);
                            return false;
                        }
                    }
                }
            }

            // 1. Hands and Feet icons at bottom / overlay
            if (doll is HandDoll || doll is FeetDoll)
            {
                if (LimbTierUtility.TryGetDollLimbColor(pawn, doll, _part, out Color limbColor))
                {
                    DrawCustomPart(_part, doll, val, rectDoll, ExtraOffset, Selectable, limbColor, pawn);
                    return false;
                }
                return true;
            }

            // 2. Main Humanlike doll - Body Substrate recoloring (when >= threshold limbs share a tier)
            var humanlikeDoll = doll as HumanlikeDoll;
            if (humanlikeDoll != null)
            {
                if (LimbTierUtility.IsBodySubstratePart(_part) && !DollDrawer.ShowArmorCover)
                {
                    // Check if this part specifically has an implant / prosthesis
                    if (!DollDrawer.Implants.ContainsKey(val))
                    {
                        var hediffs = DollDrawer.Hediffs.GetHediffs(val);
                        string addedPart = DollDrawer.FindAddedAndImplantedParts(hediffs, out _);
                        if (string.IsNullOrEmpty(addedPart))
                        {
                            bool isMissingOrCritical = false;
                            if (GetPartConditionLabelMethod != null)
                            {
                                object[] args = new object[] { hediffs, pawn, val, null, null };
                                GetPartConditionLabelMethod.Invoke(null, args);
                                string statusStr = args[3]?.ToString() ?? "";
                                if (statusStr == "Missing" || statusStr == "Critical" || statusStr == "Painful")
                                {
                                    isMissingOrCritical = true;
                                }
                            }

                            if (!isMissingOrCritical)
                            {
                                if (LimbTierUtility.TryGetDominantTierColor(pawn, out Color dominantColor))
                                {
                                    DrawCustomSubstrate(_part, doll, val, rectDoll, ExtraOffset, Selectable, dominantColor, humanlikeDoll, pawn);
                                    return false;
                                }
                            }
                        }
                    }
                }
            }

            return true;
        }

        private static void DrawCustomPart(DollBodyPartDef partDef, Doll doll, BodyPartRecord val, Rect rectDoll, Vector2 extraOffset, bool selectable, Color color, Pawn pawn)
        {
            Rect r = partDef.GetRect(doll.BoundingBox, rectDoll, extraOffset);
            Rect val2 = r;
            if (partDef.width < 0f)
            {
                DollDrawer.ScaleRect(ref r, -1f);
            }

            partDef.PartColor = color;

            Color prev = GUI.color;
            GUI.color = color;
            Texture2D tex = partDef.tex;
            if (tex != null)
            {
                GUI.DrawTexture(r, (Texture)(object)tex);
            }
            GUI.color = prev;

            if (selectable)
            {
                Rect val3 = GenUI.ContractedBy(val2, Math.Abs(val2.width) * (1f - partDef.hitboxScale.x), Math.Abs(val2.height) * (1f - partDef.hitboxScale.y));
                if (Mouse.IsOver(val3))
                {
                    DollDrawer.bodyPartRecordHovered = val;
                    DollDrawer.rectHovered = val2;
                    DollDrawer.TooltipText = val.LabelCap;
                    if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
                    {
                        DollDrawer.MakeMedOperationsFloatMenuForPart(pawn, val);
                    }
                }
            }
        }

        private static void DrawCustomSubstrate(DollBodyPartDef partDef, Doll doll, BodyPartRecord val, Rect rectDoll, Vector2 extraOffset, bool selectable, Color dominantColor, HumanlikeDoll humanlikeDoll, Pawn pawn)
        {
            Rect r = partDef.GetRect(doll.BoundingBox, rectDoll, extraOffset);
            Rect val2 = r;
            if (partDef.width < 0f)
            {
                DollDrawer.ScaleRect(ref r, -1f);
            }

            Rect val3 = GenUI.ContractedBy(val2, Math.Abs(val2.width) * (1f - partDef.hitboxScale.x), Math.Abs(val2.height) * (1f - partDef.hitboxScale.y));
            bool isHovered = selectable && Mouse.IsOver(val3);

            Color drawCol = dominantColor;
            if (isHovered)
            {
                drawCol = Color.Lerp(drawCol, Color.white, 0.2f);
            }

            partDef.PartColor = drawCol;

            Color prev = GUI.color;
            GUI.color = drawCol;
            Texture2D tex = partDef.tex;
            if (tex != null)
            {
                GUI.DrawTexture(r, (Texture)(object)tex);
            }
            GUI.color = prev;

            // Head overlay (hair, beard)
            bool isHead = (humanlikeDoll != null && (partDef == humanlikeDoll.headPart || partDef.defName.Contains("Head")));
            if (isHead && DrawHeadOverlayMethod != null)
            {
                partDef.PartColor = Color.white;
                Rect headOverlayRect = (humanlikeDoll != null) ? humanlikeDoll.GetHeadOverlayRect(rectDoll) : Rect.zero;
                Type partStatusType = AccessTools.TypeByName("NiceHealthTab.PartStatus");
                object okVal = partStatusType != null ? Enum.ToObject(partStatusType, 0) : null;
                DrawHeadOverlayMethod.Invoke(null, new object[] { pawn, headOverlayRect, partDef, okVal });
            }

            if (isHovered && !DollDrawer.ShowArmorCover)
            {
                DollDrawer.bodyPartRecordHovered = val;
                DollDrawer.rectHovered = val2;
                DollDrawer.TooltipText = val.LabelCap + "\n" + "GoodCondition".Translate();
                if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
                {
                    DollDrawer.MakeMedOperationsFloatMenuForPart(pawn, val);
                }
            }
        }
    }
}
