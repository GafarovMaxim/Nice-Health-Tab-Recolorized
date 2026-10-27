using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using NiceHealthTab;

namespace NiceHealthTab_YAPEM_Patch
{
    // Settings preview: Nice Health Tab's own doll drawn for a temporary pawn that carries one implant per tier / mod.
    // The pawn is never spawned or passed to the world, and is discarded when the preview is switched off or the window closes.
    public static class DollPreview
    {
        public const float PanelWidth = 280f;
        private const int MaxImplants = 24;

        public static bool Show;
        public static bool Drawing;
        private static bool organsView;

        private static Pawn pawn;
        private static DollRenderContext ctx;
        private static float widenedBy;

        public static bool CanShow => Current.ProgramState == ProgramState.Playing && Current.Game != null;

        public static void DrawToggle(Rect rect)
        {
            bool prev = Show;
            Widgets.CheckboxLabeled(rect, "NHT_YAPEM_Preview".Translate(), ref Show);
            if (prev != Show)
            {
                ApplyWindowWidth();
                if (!Show) Dispose();
            }
        }

        // Window is widened only while the preview is open; the panel width is remembered so closing restores it exactly.
        private static void ApplyWindowWidth()
        {
            Dialog_ModSettings window = Find.WindowStack?.WindowOfType<Dialog_ModSettings>();
            if (window == null) return;
            float delta = Show ? PanelWidth : -widenedBy;
            widenedBy = Show ? PanelWidth : 0f;
            Rect r = window.windowRect;
            r.width += delta;
            r.x = Mathf.Max(0f, r.x - delta / 2f);
            window.windowRect = r;
        }

        public static void OnWindowClosed()
        {
            Show = false;
            widenedBy = 0f;
            Dispose();
        }

        public static void Dispose()
        {
            if (pawn == null) return;
            try
            {
                if (!pawn.Destroyed) pawn.Destroy();
                pawn.Discard(true);
                if (Find.WorldPawns != null && Find.WorldPawns.Contains(pawn))
                {
                    Log.Warning("[NiceHealthTab: Recolorized] Preview pawn ended up in WorldPawns.");
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[NiceHealthTab: Recolorized] Preview pawn cleanup failed: " + ex.Message);
            }
            pawn = null;
        }

        public static void DrawPanel(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            if (!CanShow)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect.ContractedBy(10f), "NHT_YAPEM_PreviewInGameOnly".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            try
            {
                if (pawn == null) pawn = CreatePawn();
                if (pawn == null) return;
                if (ctx == null) ctx = new DollRenderContext();

                Rect buttons = new Rect(rect.x + 8f, rect.yMax - 36f, rect.width - 16f, 28f);
                if (Widgets.ButtonText(buttons, (organsView ? "NHT_YAPEM_PreviewBody" : "NHT_YAPEM_PreviewOrgans").Translate()))
                {
                    organsView = !organsView;
                }

                ctx.Prepare(pawn, false, null);
                ctx.FilterMode = organsView ? DollFilterMode.Organs : DollFilterMode.None;
                ctx.OperationPreviewEnabled = false;

                Rect dollRect = new Rect(rect.x, rect.y + 4f, rect.width, rect.height - 44f);
                Drawing = true;
                try
                {
                    DollRenderer.DrawDoll(ctx, ctx.MainDoll, dollRect, true, organsView);
                }
                finally
                {
                    Drawing = false;
                }

                if (ctx.HoveredPart != null)
                {
                    TooltipHandler.TipRegion(ctx.HoverRect, new TipSignal(ctx.HoverTooltip + ModLine(ctx.HoveredPart), 91827364));
                }
            }
            catch (Exception ex)
            {
                Log.ErrorOnce("[NiceHealthTab: Recolorized] Preview failed: " + ex, 91827365);
                Show = false;
                ApplyWindowWidth();
                Dispose();
            }
        }

        private static string ModLine(BodyPartRecord part)
        {
            List<Hediff> list = ctx.Hediffs.GetHediffs(part);
            for (int i = 0; i < list.Count; i++)
            {
                Hediff h = list[i];
                if (h.def.countsAsAddedPartOrImplant || h.def.addedPartProps != null)
                {
                    return "\n" + h.LabelCap + "\n" + string.Format("NHT_YAPEM_PreviewModLine".Translate().RawText, h.def.modContentPack?.Name ?? "?");
                }
            }
            return string.Empty;
        }

        private class Candidate
        {
            public HediffDef Def;
            public string Tier;
            public string ModKey;
            public List<BodyPartDef> Parts;
        }

        private static Pawn CreatePawn()
        {
            var request = new PawnGenerationRequest(PawnKindDefOf.Colonist, null, PawnGenerationContext.NonPlayer,
                forceGenerateNewPawn: true, canGeneratePawnRelations: false, allowFood: false, allowAddictions: false,
                fixedBiologicalAge: 25f, fixedChronologicalAge: 25f, fixedGender: Gender.Male);
            Pawn p = PawnGenerator.GeneratePawn(request);
            if (p?.health?.hediffSet == null) return p;

            p.health.hediffSet.hediffs.Clear();
            AddImplants(p);
            p.health.hediffSet.DirtyCache();
            return p;
        }

        private static void AddImplants(Pawn p)
        {
            List<Candidate> all = CollectCandidates();

            // mod -> number of its implants: big mods first, base game and DLC always first
            var modSize = new Dictionary<string, int>();
            foreach (Candidate c in all)
            {
                if (c.ModKey == null) continue;
                modSize.TryGetValue(c.ModKey, out int n);
                modSize[c.ModKey] = n + 1;
            }

            var groups = new Dictionary<string, List<Candidate>>();
            foreach (Candidate c in all)
            {
                string key = c.Tier + "|" + c.ModKey;
                if (!groups.TryGetValue(key, out List<Candidate> g)) groups[key] = g = new List<Candidate>();
                g.Add(c);
            }
            var ordered = new List<List<Candidate>>(groups.Values);
            ordered.Sort((a, b) =>
            {
                int sa = a[0].ModKey == null ? int.MaxValue : modSize[a[0].ModKey];
                int sb = b[0].ModKey == null ? int.MaxValue : modSize[b[0].ModKey];
                return sb.CompareTo(sa);
            });

            var used = new List<BodyPartRecord>();
            foreach (List<Candidate> group in ordered)
            {
                if (used.Count >= MaxImplants) break;
                foreach (Candidate c in group)
                {
                    BodyPartRecord part = FindFreePart(p.RaceProps.body, c.Parts, used);
                    if (part == null) continue;
                    Hediff h = HediffMaker.MakeHediff(c.Def, p, part);
                    p.health.hediffSet.AddDirect(h);
                    used.Add(part);
                    break;
                }
            }
        }

        private static List<Candidate> CollectCandidates()
        {
            var result = new List<Candidate>();
            foreach (HediffDef def in DefDatabase<HediffDef>.AllDefsListForReading)
            {
                if (def.addedPartProps == null && !def.countsAsAddedPartOrImplant) continue;
                if (!LimbTierUtility.CategorizeHediffDefTier(def, out string tier, out _) || tier == null) continue;

                List<BodyPartDef> parts = null;
                foreach (RecipeDef r in DefDatabase<RecipeDef>.AllDefsListForReading)
                {
                    if (r.addsHediff != def || r.appliedOnFixedBodyParts == null) continue;
                    parts = parts ?? new List<BodyPartDef>();
                    parts.AddRange(r.appliedOnFixedBodyParts);
                }
                if (parts == null) continue;
                result.Add(new Candidate { Def = def, Tier = tier, ModKey = LimbTierUtility.GetModKey(def), Parts = parts });
            }
            return result;
        }

        private static BodyPartRecord FindFreePart(BodyDef body, List<BodyPartDef> allowed, List<BodyPartRecord> used)
        {
            foreach (BodyPartRecord part in body.AllParts)
            {
                if (!allowed.Contains(part.def)) continue;
                bool blocked = false;
                for (int i = 0; i < used.Count && !blocked; i++)
                {
                    blocked = Related(part, used[i]);
                }
                if (!blocked) return part;
            }
            return null;
        }

        private static bool Related(BodyPartRecord a, BodyPartRecord b) => a == b || IsAncestor(a, b) || IsAncestor(b, a);

        private static bool IsAncestor(BodyPartRecord ancestor, BodyPartRecord part)
        {
            for (BodyPartRecord x = part.parent; x != null; x = x.parent)
            {
                if (x == ancestor) return true;
            }
            return false;
        }
    }

    // The preview pawn is a temporary dummy: its medical menu must not open.
    [HarmonyPatch(typeof(DollOperations), nameof(DollOperations.OpenFloatMenuForPart))]
    public static class Patch_PreviewNoFloatMenu
    {
        public static bool Prefix() => !DollPreview.Drawing;
    }
}
