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

            // 0. If _part is a prosthesis, ensure PartColor matches current settings
            if (_part is DollPartProsthesis && val != null)
            {
                var hediffs = DollDrawer.Hediffs.GetHediffs(val);
                if (hediffs != null)
                {
                    foreach (var h in hediffs)
                    {
                        if (LimbTierUtility.CategorizeHediffTier(h, out _, out Color tierColor))
                        {
                            _part.PartColor = tierColor;
                            break;
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
