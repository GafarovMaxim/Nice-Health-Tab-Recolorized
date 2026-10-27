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

        // Resolved once: TypeByName scans every loaded assembly and was called per head draw.
        private static readonly Type PartStatusType = AccessTools.TypeByName("NiceHealthTab.PartStatus");
        private static readonly object PartStatusOk = PartStatusType != null ? Enum.ToObject(PartStatusType, 0) : null;
        private static readonly int HeadOverlayParamCount = DrawHeadOverlayMethod?.GetParameters().Length ?? 0;

        // Reused Invoke buffers: OnGUI is single-threaded and Invoke does not keep the arrays.
        private static readonly object[] PartConditionArgs = new object[5];
        private static readonly object[] HeadOverlayArgs = new object[4];

        [HarmonyFinalizer]
        public static void Finalizer()
        {
            OrganVisibilityOverride.Restore();
        }

        // Like the original DrawPart: the tier color only shows on an undamaged part, a wounded one shows its damage color.
        private static Color DamageAwareColor(System.Collections.Generic.List<Hediff> hediffs, Pawn pawn, BodyPartRecord part, Color tierColor)
        {
            if (GetPartConditionLabelMethod == null) return tierColor;
            object[] args = PartConditionArgs;
            args[0] = hediffs;
            args[1] = pawn;
            args[2] = part;
            args[3] = null;
            args[4] = null;
            object damageColor = GetPartConditionLabelMethod.Invoke(null, args);
            string status = args[3]?.ToString() ?? "";
            return status == "" || status == "Ok" || status == "NonOk" || !(damageColor is Color c) ? tierColor : c;
        }

        private static bool IsDamaged(Pawn pawn, BodyPartRecord part)
        {
            if (GetPartConditionLabelMethod == null) return false;
            object[] args = PartConditionArgs;
            args[0] = DollDrawer.Hediffs.GetHediffs(part);
            args[1] = pawn;
            args[2] = part;
            args[3] = null;
            args[4] = null;
            GetPartConditionLabelMethod.Invoke(null, args);
            string status = args[3]?.ToString() ?? "";
            return status != "Ok" && status != "NonOk";
        }

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

            // No alternative-part lookup on this legacy path, so the rendered part is _part itself.
            var kind = LimbTierUtility.ClassifyPart(val.def, _part, _part);
            bool isBonesOnly = kind.IsBonesOnly;
            bool isBone = kind.IsBone;
            bool isOrgan = kind.IsOrgan;
            bool isEye = kind.IsEye;

            // Strict Filter Conformance:
            // Spines only show when bones are viewed!
            if (isBonesOnly && (!DollDrawer.ShowBones || DollDrawer.ShowArmorCover))
            {
                return false;
            }
            // Exception: a damaged organ is always shown (armor cover still hides it).
            if (isOrgan && (YAPEMPatchMod.Settings?.OnlyShowOrgansInOrganMode == true || !DollDrawer.ShowOrgan || DollDrawer.ShowArmorCover))
            {
                if (DollDrawer.ShowArmorCover || !IsDamaged(pawn, val))
                {
                    return false;
                }
                OrganVisibilityOverride.Force();
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
                            DrawCustomPart(_part, doll, val, rectDoll, ExtraOffset, Selectable, DamageAwareColor(hediffs, pawn, val, tierColor), pawn);
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
                                // Reused buffer: slots 3-4 are out params, reset so a stale value is never read.
                                object[] args = PartConditionArgs;
                                args[0] = hediffs;
                                args[1] = pawn;
                                args[2] = val;
                                args[3] = null;
                                args[4] = null;
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
                if (headOverlayRect != Rect.zero)
                {
                    try
                    {
                        if (HeadOverlayParamCount == 4)
                        {
                            HeadOverlayArgs[0] = pawn;
                            HeadOverlayArgs[1] = headOverlayRect;
                            HeadOverlayArgs[2] = partDef;
                            HeadOverlayArgs[3] = PartStatusOk;
                            DrawHeadOverlayMethod.Invoke(null, HeadOverlayArgs);
                        }
                    }
                    catch { }
                }
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
