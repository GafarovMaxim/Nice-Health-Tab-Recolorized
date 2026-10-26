using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using NiceHealthTab;

namespace NiceHealthTab_YAPEM_Patch
{
    [HarmonyPatch(typeof(DollPartRenderer), "DrawPart")]
    public static class Patch_DollPartRenderer_DrawPart
    {
        private static readonly MethodInfo DrawHeadOverlayMethod = 
            AccessTools.Method(typeof(DollPartRenderer), "DrawHeadOverlay");

        [HarmonyPrefix]
        public static bool Prefix(DollRenderContext ctx, DollBodyPartDef _part, Doll doll, Rect rectDoll, float dollScale, bool selectable, Vector2 extraOffset)
        {
            if (ctx?.Pawn == null || _part == null || doll == null || ctx.Remap == null)
            {
                return true;
            }

            int num = ctx.Remap.Get(_part.bodyPartId);
            if (num < 0)
            {
                return true;
            }

            BodyDef body = ctx.Pawn.RaceProps?.body;
            BodyPartRecord val = (body != null) ? body.GetPartAtIndex(num) : null;
            if (val == null)
            {
                return true;
            }

            // 0. Ensure implantInfo.Color is up-to-date with current settings
            PartImplantInfo implantInfo = ctx.GetImplantInfo(_part, val);
            if (implantInfo != null)
            {
                var partHediffs = ctx.Hediffs.GetHediffs(val);
                if (partHediffs != null)
                {
                    foreach (var h in partHediffs)
                    {
                        if (LimbTierUtility.CategorizeHediffTier(h, out _, out Color tierColor))
                        {
                            implantInfo.Color = tierColor;
                            break;
                        }
                    }
                }
            }

            // 0b. Direct Prosthesis / Bionic Limb recoloring
            // Nice Health Tab resets GUI.color to Color.white for DollPartProsthesis if overrideColor is true.
            // By drawing the prosthesis directly with its tierColor here, we prevent the white reset!
            if (implantInfo != null && implantInfo.HasAlternative && implantInfo.Part != null)
            {
                var partHediffs = ctx.Hediffs.GetHediffs(val);
                if (partHediffs != null)
                {
                    foreach (var h in partHediffs)
                    {
                        if (LimbTierUtility.CategorizeHediffTier(h, out _, out Color tierColor))
                        {
                            implantInfo.Color = tierColor;
                            if (implantInfo.Part is DollPartProsthesis p)
                            {
                                p.overrideColor = false;
                            }
                            PartFrameInfo partFrameInfo = ctx.GetPartFrameInfo(val);
                            PartVisual partVisual = ctx.GetPartVisual(val, partHediffs, partFrameInfo.Health, partFrameInfo.MaxHealth);
                            if (partVisual.Status == DollPartStatus.Missing)
                            {
                                return false;
                            }
                            DrawCustomPart(ctx, implantInfo.Part, doll, val, rectDoll, extraOffset, selectable, tierColor);
                            return false;
                        }
                    }
                }
            }

            if (_part is DollPartProsthesis directProsthesis)
            {
                directProsthesis.overrideColor = false;
                var partHediffs = ctx.Hediffs.GetHediffs(val);
                if (partHediffs != null)
                {
                    foreach (var h in partHediffs)
                    {
                        if (LimbTierUtility.CategorizeHediffTier(h, out _, out Color tierColor))
                        {
                            PartFrameInfo partFrameInfo = ctx.GetPartFrameInfo(val);
                            PartVisual partVisual = ctx.GetPartVisual(val, partHediffs, partFrameInfo.Health, partFrameInfo.MaxHealth);
                            if (partVisual.Status == DollPartStatus.Missing)
                            {
                                return false;
                            }
                            DrawCustomPart(ctx, directProsthesis, doll, val, rectDoll, extraOffset, selectable, tierColor);
                            return false;
                        }
                    }
                }
            }

            // 1. Hands and Feet icons at bottom / overlay
            if (doll is HandDoll || doll is FeetDoll)
            {
                if (LimbTierUtility.TryGetDollLimbColor(ctx.Pawn, doll, _part, out Color limbColor))
                {
                    DrawCustomPart(ctx, _part, doll, val, rectDoll, extraOffset, selectable, limbColor);
                    return false;
                }
                return true;
            }

            // 2. Main Humanlike doll - Body Substrate recoloring (when >= threshold limbs share a tier)
            var humanlikeDoll = doll as HumanlikeDoll;
            if (humanlikeDoll != null)
            {
                // Only recolor base body substrate (not organs, bones, eyes, or prostheses)
                if (LimbTierUtility.IsBodySubstratePart(_part) && !ctx.ArmorMode)
                {
                    // Check if this part specifically has an implant / prosthesis
                    if (implantInfo == null)
                    {
                        PartFrameInfo partFrameInfo = ctx.GetPartFrameInfo(val);
                        var hediffs = ctx.Hediffs.GetHediffs(val);
                        PartVisual partVisual = ctx.GetPartVisual(val, hediffs, partFrameInfo.Health, partFrameInfo.MaxHealth);

                        // Benign condition (wounds and missing parts are skipped)
                        if (partVisual.Status != DollPartStatus.Missing &&
                            partVisual.Status != DollPartStatus.Critical &&
                            partVisual.Status != DollPartStatus.Painful)
                        {
                            if (LimbTierUtility.TryGetDominantTierColor(ctx.Pawn, out Color dominantColor))
                            {
                                DrawCustomSubstrate(ctx, _part, doll, val, rectDoll, extraOffset, selectable, dominantColor, humanlikeDoll);
                                return false;
                            }
                        }
                    }
                }
            }

            return true;
        }

        private static void DrawCustomPart(DollRenderContext ctx, DollBodyPartDef partDef, Doll doll, BodyPartRecord val, Rect rectDoll, Vector2 extraOffset, bool selectable, Color color)
        {
            Rect r = partDef.GetRect(doll.BoundingBox, rectDoll, extraOffset);
            Rect val3 = r;
            if (partDef.width < 0f)
            {
                DollRenderer.ScaleRect(ref r, -1f);
            }

            partDef.PartColor = color;
            partDef.GetSource(out Texture2D tex, out Rect uv);

            Color prev = GUI.color;
            GUI.color = color;
            if (tex != null)
            {
                GUI.DrawTextureWithTexCoords(r, (Texture)(object)tex, uv);
            }
            GUI.color = prev;

            if (selectable)
            {
                Rect val4 = GenUI.ContractedBy(val3, Math.Abs(val3.width) * (1f - partDef.hitboxScale.x), Math.Abs(val3.height) * (1f - partDef.hitboxScale.y));
                if (Mouse.IsOver(val4))
                {
                    if (ctx.BlockOverlay && ctx.OverlayBlockRect != Rect.zero && Mouse.IsOver(ctx.OverlayBlockRect))
                    {
                        return;
                    }
                    ctx.HoveredPart = val;
                    ctx.HoverRect = val3;
                    ctx.HoverTooltip = val.LabelCap;
                    if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
                    {
                        DollOperations.OpenFloatMenuForPart(ctx.Pawn, val, ctx.ThingForMedBills);
                    }
                }
            }
        }

        private static void DrawCustomSubstrate(DollRenderContext ctx, DollBodyPartDef partDef, Doll doll, BodyPartRecord val, Rect rectDoll, Vector2 extraOffset, bool selectable, Color dominantColor, HumanlikeDoll humanlikeDoll)
        {
            Rect r = partDef.GetRect(doll.BoundingBox, rectDoll, extraOffset);
            Rect val3 = r;
            if (partDef.width < 0f)
            {
                DollRenderer.ScaleRect(ref r, -1f);
            }

            Vector2 center = val3.center;
            Rect val4 = GenUI.ContractedBy(val3, Math.Abs(val3.width) * (1f - partDef.hitboxScale.x), Math.Abs(val3.height) * (1f - partDef.hitboxScale.y));
            bool isHovered = selectable && Mouse.IsOver(val4);
            if (isHovered && ctx.BlockOverlay && ctx.OverlayBlockRect != Rect.zero && Mouse.IsOver(ctx.OverlayBlockRect))
            {
                isHovered = false;
            }

            Color drawCol = dominantColor;
            if (isHovered)
            {
                drawCol = Color.Lerp(drawCol, Color.white, 0.2f);
            }

            partDef.PartColor = drawCol;
            partDef.GetSource(out Texture2D tex, out Rect uv);

            Color prev = GUI.color;
            GUI.color = drawCol;
            if (tex != null)
            {
                GUI.DrawTextureWithTexCoords(r, (Texture)(object)tex, uv);
            }
            GUI.color = prev;

            // If head part, draw hair/beard overlay with natural white part color
            bool isHead = (humanlikeDoll != null && (partDef == humanlikeDoll.headPart || partDef.defName.Contains("Head")));
            if (isHead && DrawHeadOverlayMethod != null)
            {
                partDef.PartColor = Color.white;
                Rect headOverlayRect = (humanlikeDoll != null) ? humanlikeDoll.GetHeadOverlayRect(rectDoll) : Rect.zero;
                if (headOverlayRect != Rect.zero)
                {
                    DrawHeadOverlayMethod.Invoke(null, new object[] { ctx, ctx.Pawn, headOverlayRect, partDef, DollPartStatus.Ok });
                }
            }

            if (isHovered && !ctx.ArmorMode)
            {
                ctx.HoveredPart = val;
                ctx.HoverRect = val3;
                ctx.HoverTooltip = val.LabelCap + "\n" + "GoodCondition".Translate();
                if (ctx.HitPercentMode)
                {
                    ctx.HoverFilterCandidate = val;
                }
                if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
                {
                    DollOperations.OpenFloatMenuForPart(ctx.Pawn, val, ctx.ThingForMedBills);
                }
            }
        }
    }
}
