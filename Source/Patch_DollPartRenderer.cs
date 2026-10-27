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

        private static readonly object[] HeadOverlayArgs = new object[5];
        private static readonly object BoxedStatusOk = DollPartStatus.Ok;

        [HarmonyFinalizer]
        public static void Finalizer()
        {
            OrganVisibilityOverride.Restore();
        }

        private static bool IsDamaged(DollRenderContext ctx, BodyPartRecord part)
        {
            PartFrameInfo frame = ctx.GetPartFrameInfo(part);
            DollPartStatus status = ctx.GetPartVisual(part, ctx.Hediffs.GetHediffs(part), frame.Health, frame.MaxHealth).Status;
            return status != DollPartStatus.Ok && status != DollPartStatus.NonOk;
        }

        [HarmonyPrefix]
        public static bool Prefix(DollRenderContext ctx, DollBodyPartDef _part, Doll doll, Rect rectDoll, float dollScale, bool selectable, Vector2 extraOffset)
        {
            if (ctx?.Pawn == null || _part == null || doll == null || ctx.Remap == null)
            {
                return true;
            }

            // 1. Hands and Feet icons at bottom / overlay
            // In Nice Health Tab, HandDoll and FeetDoll are distinct widgets at the bottom left.
            // They do not undergo body filter modes (bones/organs).
            if (doll is HandDoll || doll is FeetDoll)
            {
                int remapIndex = ctx.Remap.Get(_part.bodyPartId);
                BodyDef pawnBody = ctx.Pawn.RaceProps?.body;
                BodyPartRecord limbRecord = (pawnBody != null && remapIndex >= 0) ? pawnBody.GetPartAtIndex(remapIndex) : null;
                if (limbRecord != null && LimbTierUtility.TryGetDollLimbColor(ctx.Pawn, doll, _part, out Color limbColor))
                {
                    DrawCustomPart(ctx, _part, doll, limbRecord, rectDoll, extraOffset, selectable, limbColor);
                    return false;
                }
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

            // 2. Identify implant / prosthesis alternative
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

            // Determine effective part definition being rendered
            DollBodyPartDef effectivePart = (implantInfo != null && implantInfo.HasAlternative && implantInfo.Part != null)
                ? implantInfo.Part
                : _part;

            var kind = LimbTierUtility.ClassifyPart(val.def, _part, effectivePart);
            bool isBonesOnly = kind.IsBonesOnly;
            bool isBone = kind.IsBone;
            bool isOrgan = kind.IsOrgan;
            bool isEye = kind.IsEye;

            // 3. Strict Filter Mode conformance:

            // User Rule: Spines and ribs must strictly ONLY show in Bone view mode!
            // In Overview and Organ modes, they are always hidden.
            if (isBonesOnly && ctx.FilterMode != DollFilterMode.Bones)
            {
                return false;
            }

            // In Bones view: ONLY bones (and spine) are shown
            if (ctx.FilterMode == DollFilterMode.Bones)
            {
                if (!isBone)
                {
                    return false;
                }
            }
            // In Organs view: ONLY organs are shown (natural and bionic/implanted)
            else if (ctx.FilterMode == DollFilterMode.Organs)
            {
                if (!isOrgan)
                {
                    return false;
                }
            }
            // In Overview mode:
            else
            {
                // User option: display organs ONLY in organ view mode
                // When enabled, hides ALL organs (natural AND bionic/implanted) in overview!
                // Exception: a damaged organ is always shown (armor mode still hides it).
                if (isOrgan && (YAPEMPatchMod.Settings?.OnlyShowOrgansInOrganMode == true || !NiceHediffTabSettings.ShowOrgan || ctx.ArmorMode))
                {
                    if (ctx.ArmorMode || !IsDamaged(ctx, val))
                    {
                        return false;
                    }
                    OrganVisibilityOverride.Force();
                }
                if (isBone && (!NiceHediffTabSettings.ShowBones || ctx.ArmorMode))
                {
                    return false;
                }
                if (isEye && !NiceHediffTabSettings.ShowEyes)
                {
                    return false;
                }
            }

            // 4. Custom Prosthesis / Bionic Limb & Organ recoloring
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

                // If this is an organ or bone with a prosthesis alternative that didn't match a custom tier:
                // Draw it here directly so original DrawPart's faulty def-type check doesn't cull it!
                if (isOrgan || isBone)
                {
                    PartFrameInfo partFrameInfo = ctx.GetPartFrameInfo(val);
                    PartVisual partVisual = ctx.GetPartVisual(val, partHediffs, partFrameInfo.Health, partFrameInfo.MaxHealth);
                    if (partVisual.Status == DollPartStatus.Missing)
                    {
                        return false;
                    }
                    Color drawColor = implantInfo.Color != Color.clear ? implantInfo.Color : Color.white;
                    DrawCustomPart(ctx, implantInfo.Part, doll, val, rectDoll, extraOffset, selectable, drawColor);
                    return false;
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

            // 5. Main Humanlike doll - Body Substrate recoloring (when >= threshold limbs share a tier)
            var humanlikeDoll = doll as HumanlikeDoll;
            if (humanlikeDoll != null)
            {
                if (LimbTierUtility.IsBodySubstratePart(_part) && !ctx.ArmorMode)
                {
                    if (implantInfo == null)
                    {
                        PartFrameInfo partFrameInfo = ctx.GetPartFrameInfo(val);
                        var hediffs = ctx.Hediffs.GetHediffs(val);
                        PartVisual partVisual = ctx.GetPartVisual(val, hediffs, partFrameInfo.Health, partFrameInfo.MaxHealth);

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

            bool isHead = (humanlikeDoll != null && (partDef == humanlikeDoll.headPart || partDef.defName.Contains("Head")));
            if (isHead && DrawHeadOverlayMethod != null)
            {
                partDef.PartColor = Color.white;
                Rect headOverlayRect = (humanlikeDoll != null) ? humanlikeDoll.GetHeadOverlayRect(rectDoll) : Rect.zero;
                if (headOverlayRect != Rect.zero)
                {
                    // Reused buffer: OnGUI is single-threaded and Invoke does not keep the array.
                    HeadOverlayArgs[0] = ctx;
                    HeadOverlayArgs[1] = ctx.Pawn;
                    HeadOverlayArgs[2] = headOverlayRect;
                    HeadOverlayArgs[3] = partDef;
                    HeadOverlayArgs[4] = BoxedStatusOk;
                    DrawHeadOverlayMethod.Invoke(null, HeadOverlayArgs);
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
