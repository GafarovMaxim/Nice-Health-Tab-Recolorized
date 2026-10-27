using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace NiceHealthTab_YAPEM_Patch
{
    public class ImplantGroup
    {
        public string GroupKey;
        public string GroupTitle;
        public List<HediffDef> Defs = new List<HediffDef>();
    }

    public class YAPEMPatchSettings : ModSettings
    {
        // Canonical defaults - Base Tiers (Nicer Health Tab original palette)
        public static readonly Color DefaultColorAdvancedBionic = new Color(161f / 255f, 206f / 255f, 243f / 255f, 1f); // #A1CEF3 Sky Blue
        public static readonly Color DefaultColorUltratech      = new Color(113f / 255f, 203f / 255f, 194f / 255f, 1f); // #71CBC2 Mint Turquoise
        public static readonly Color DefaultColorArchotech      = new Color(203f / 255f, 208f / 255f, 171f / 255f, 1f); // #CBD0AB Sage Green
        public static readonly Color DefaultColorBionic         = new Color(230f / 255f, 230f / 255f, 230f / 255f, 1f); // #E6E6E6 Clean Metal White
        public static readonly Color DefaultColorProsthetic     = new Color(176f / 255f, 176f / 255f, 176f / 255f, 1f); // #B0B0B0 Silver Grey

        // Canonical defaults - DLC & Mod Categories
        public static readonly Color DefaultColorRoyalty            = DefaultColorBionic; // same as the Bionic tier by default
        public static readonly Color DefaultColorBiotech            = DefaultColorBionic; // same as the Bionic tier by default
        public static readonly Color DefaultColorAnomaly            = DefaultColorBionic; // same as the Bionic tier by default
        public static readonly Color DefaultColorOdyssey            = DefaultColorBionic; // same as the Bionic tier by default
        public static readonly Color DefaultColorIntegratedImplants = new Color(153f / 255f, 153f / 255f, 255f / 255f, 1f); // #9999FF Cyber Lavender

        // Body substrate, icon, and display toggles
        public bool EnableBodyTinting = true;
        public int RequiredLimbCount = 3;
        public bool ColorBottomIcons = true;
        public bool OnlyShowOrgansInOrganMode = false;
        public bool UseDLCColors = true;

        // Custom colors - Base Tiers
        public Color ColorAdvancedBionic = DefaultColorAdvancedBionic;
        public Color ColorUltratech      = DefaultColorUltratech;
        public Color ColorArchotech      = DefaultColorArchotech;
        public Color ColorBionic         = DefaultColorBionic;
        public Color ColorProsthetic     = DefaultColorProsthetic;

        // Custom colors - DLC & Mod Categories
        public Color ColorRoyalty            = DefaultColorRoyalty;
        public Color ColorBiotech            = DefaultColorBiotech;
        public Color ColorAnomaly            = DefaultColorAnomaly;
        public Color ColorOdyssey            = DefaultColorOdyssey;
        public Color ColorIntegratedImplants = DefaultColorIntegratedImplants;

        // User Custom Palette Presets (persisted across sessions, 8 fixed slots)
        public List<Color> CustomPresets = new List<Color>();

        // Custom implant recolor tier overrides: defName -> tier ("Auto", "None", "Archotech", etc.)
        public Dictionary<string, string> CustomHediffTiers = new Dictionary<string, string>();

        // "#RRGGBB" per third-party mod (lowercase packageId) and per auto-detected tier (tier key). Absent = Auto.
        // Priority when coloring: CustomHediffTiers (single implant) > ModColors > tier color.
        public Dictionary<string, string> ModColors = new Dictionary<string, string>();
        public Dictionary<string, string> DynamicTierColors = new Dictionary<string, string>();

        public static bool TryParseHex(string hex, out Color color)
        {
            color = Color.white;
            return !string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out color);
        }

        public static string ToHex(Color color) => "#" + ColorUtility.ToHtmlStringRGB(color);

        // Scroll view state
        private Vector2 scrollPosition = Vector2.zero;
        private float viewHeight = 1600f;

        // UI state for Implant Fine-Tuning section
        private static List<ImplantGroup> cachedGroups = null;
        private static int selectedTabGroupIndex = 0;
        private static string searchFilter = "";

        // Palette Profiles
        public List<PaletteProfile> Profiles = new List<PaletteProfile>();
        public string ActiveProfileName = "NHT_YAPEM_ProfileDefault";

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref EnableBodyTinting, "EnableBodyTinting", true);
            Scribe_Values.Look(ref RequiredLimbCount, "RequiredLimbCount", 3);
            Scribe_Values.Look(ref ColorBottomIcons, "ColorBottomIcons", true);
            Scribe_Values.Look(ref OnlyShowOrgansInOrganMode, "OnlyShowOrgansInOrganMode", false);
            Scribe_Values.Look(ref UseDLCColors, "UseDLCColors", true);

            Scribe_Values.Look(ref ColorAdvancedBionic, "ColorAdvancedBionic", DefaultColorAdvancedBionic);
            Scribe_Values.Look(ref ColorUltratech, "ColorUltratech", DefaultColorUltratech);
            Scribe_Values.Look(ref ColorArchotech, "ColorArchotech", DefaultColorArchotech);
            Scribe_Values.Look(ref ColorBionic, "ColorBionic", DefaultColorBionic);
            Scribe_Values.Look(ref ColorProsthetic, "ColorProsthetic", DefaultColorProsthetic);

            Scribe_Values.Look(ref ColorRoyalty, "ColorRoyalty", DefaultColorRoyalty);
            Scribe_Values.Look(ref ColorBiotech, "ColorBiotech", DefaultColorBiotech);
            Scribe_Values.Look(ref ColorAnomaly, "ColorAnomaly", DefaultColorAnomaly);
            Scribe_Values.Look(ref ColorOdyssey, "ColorOdyssey", DefaultColorOdyssey);
            Scribe_Values.Look(ref ColorIntegratedImplants, "ColorIntegratedImplants", DefaultColorIntegratedImplants);

            Scribe_Collections.Look(ref CustomPresets, "CustomPresets", LookMode.Value);
            if (CustomPresets == null)
            {
                CustomPresets = new List<Color>();
            }

            Scribe_Values.Look(ref ActiveProfileName, "ActiveProfileName", "NHT_YAPEM_ProfileDefault");
            Scribe_Collections.Look(ref Profiles, "Profiles", LookMode.Deep);
            EnsureProfilesInitialized();

            Scribe_Collections.Look(ref CustomHediffTiers, "CustomHediffTiers", LookMode.Value, LookMode.Value);
            if (CustomHediffTiers == null)
            {
                CustomHediffTiers = new Dictionary<string, string>();
            }

            Scribe_Collections.Look(ref ModColors, "ModColors", LookMode.Value, LookMode.Value);
            if (ModColors == null)
            {
                ModColors = new Dictionary<string, string>();
            }
            Scribe_Collections.Look(ref DynamicTierColors, "DynamicTierColors", LookMode.Value, LookMode.Value);
            if (DynamicTierColors == null)
            {
                DynamicTierColors = new Dictionary<string, string>();
            }

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                LimbTierUtility.ClearCaches();
            }
        }

        public void EnsureProfilesInitialized()
        {
            if (Profiles == null)
            {
                Profiles = new List<PaletteProfile>();
            }

            if (!Profiles.Any(p => p.Name == "NHT_YAPEM_ProfileDefault"))
            {
                Profiles.Insert(0, PaletteProfile.CreateDefault());
            }
            if (!Profiles.Any(p => p.Name == "NHT_YAPEM_ProfileCyberpunk"))
            {
                Profiles.Add(PaletteProfile.CreateCyberpunk());
            }
            if (!Profiles.Any(p => p.Name == "NHT_YAPEM_ProfilePastel"))
            {
                Profiles.Add(PaletteProfile.CreatePastel());
            }

            if (string.IsNullOrEmpty(ActiveProfileName) || !Profiles.Any(p => p.Name == ActiveProfileName))
            {
                ActiveProfileName = "NHT_YAPEM_ProfileDefault";
            }
        }

        public PaletteProfile GetActiveProfile()
        {
            EnsureProfilesInitialized();
            var prof = Profiles.FirstOrDefault(p => p.Name == ActiveProfileName);
            if (prof == null)
            {
                prof = Profiles.First();
                ActiveProfileName = prof.Name;
            }
            return prof;
        }

        public void ApplyProfile(PaletteProfile prof, bool save = true)
        {
            if (prof == null) return;
            EnsureProfilesInitialized();

            ActiveProfileName = prof.Name;

            ColorAdvancedBionic = prof.ColorAdvancedBionic;
            ColorUltratech      = prof.ColorUltratech;
            ColorArchotech      = prof.ColorArchotech;
            ColorBionic         = prof.ColorBionic;
            ColorProsthetic     = prof.ColorProsthetic;

            ColorRoyalty            = prof.ColorRoyalty;
            ColorBiotech            = prof.ColorBiotech;
            ColorAnomaly            = prof.ColorAnomaly;
            ColorOdyssey            = prof.ColorOdyssey;
            ColorIntegratedImplants = prof.ColorIntegratedImplants;

            if (prof.CustomPresets != null)
            {
                CustomPresets = new List<Color>();
                for (int i = 0; i < prof.CustomPresets.Count; i++)
                {
                    CustomPresets.Add(prof.CustomPresets[i]);
                }
            }

            if (save) Write();
        }

        public void SyncActiveColorsToProfile()
        {
            var prof = GetActiveProfile();
            if (prof != null)
            {
                prof.ColorAdvancedBionic = ColorAdvancedBionic;
                prof.ColorUltratech      = ColorUltratech;
                prof.ColorArchotech      = ColorArchotech;
                prof.ColorBionic         = ColorBionic;
                prof.ColorProsthetic     = ColorProsthetic;

                prof.ColorRoyalty            = ColorRoyalty;
                prof.ColorBiotech            = ColorBiotech;
                prof.ColorAnomaly            = ColorAnomaly;
                prof.ColorOdyssey            = ColorOdyssey;
                prof.ColorIntegratedImplants = ColorIntegratedImplants;

                prof.CustomPresets = new List<Color>();
                if (CustomPresets != null)
                {
                    for (int i = 0; i < CustomPresets.Count; i++)
                    {
                        prof.CustomPresets.Add(CustomPresets[i]);
                    }
                }
                prof.EnsureSlotCount();
            }
        }

        public void CreateProfile(string name)
        {
            EnsureProfilesInitialized();
            if (string.IsNullOrEmpty(name)) return;

            string uniqueName = name.Trim();
            int counter = 2;
            while (Profiles.Any(p => p.Name.Equals(uniqueName, StringComparison.OrdinalIgnoreCase)))
            {
                uniqueName = $"{name.Trim()} ({counter++})";
            }

            var prof = new PaletteProfile(uniqueName, false);
            prof.ColorAdvancedBionic = ColorAdvancedBionic;
            prof.ColorUltratech      = ColorUltratech;
            prof.ColorArchotech      = ColorArchotech;
            prof.ColorBionic         = ColorBionic;
            prof.ColorProsthetic     = ColorProsthetic;

            prof.ColorRoyalty            = ColorRoyalty;
            prof.ColorBiotech            = ColorBiotech;
            prof.ColorAnomaly            = ColorAnomaly;
            prof.ColorOdyssey            = ColorOdyssey;
            prof.ColorIntegratedImplants = ColorIntegratedImplants;

            prof.CustomPresets = new List<Color>();
            if (CustomPresets != null)
            {
                for (int i = 0; i < CustomPresets.Count; i++)
                {
                    prof.CustomPresets.Add(CustomPresets[i]);
                }
            }
            prof.EnsureSlotCount();

            Profiles.Add(prof);
            ActiveProfileName = prof.Name;
            Write();
        }

        public void DeleteProfile(PaletteProfile prof)
        {
            if (prof == null || prof.IsBuiltIn) return;
            EnsureProfilesInitialized();

            Profiles.Remove(prof);
            var def = Profiles.FirstOrDefault(p => p.Name == "NHT_YAPEM_ProfileDefault") ?? Profiles.First();
            ApplyProfile(def, true);
        }

        public void DrawProfileToolbar(Rect rect)
        {
            EnsureProfilesInitialized();
            var activeProf = GetActiveProfile();

            float x = rect.x;
            float y = rect.y;

            Text.Font = GameFont.Small;
            string profLabel = "NHT_YAPEM_Profile".Translate() + ":";
            Vector2 labelSize = Text.CalcSize(profLabel);
            Widgets.Label(new Rect(x, y + 2f, labelSize.x + 8f, rect.height), profLabel);
            x += labelSize.x + 12f;

            float dropW = 160f;
            Rect dropRect = new Rect(x, y, dropW, rect.height);
            string dropTitle = (activeProf?.Label ?? "Profile") + " ▾";
            if (Widgets.ButtonText(dropRect, dropTitle))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (var p in Profiles)
                {
                    var captured = p;
                    options.Add(new FloatMenuOption(captured.Label + (captured.Name == ActiveProfileName ? " ✓" : ""), () =>
                    {
                        ApplyProfile(captured, true);
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
            x += dropW + 6f;

            float btnW = 60f;
            Rect newBtnRect = new Rect(x, y, btnW, rect.height);
            TooltipHandler.TipRegion(newBtnRect, "NHT_YAPEM_NewProfilePrompt".Translate());
            if (Widgets.ButtonText(newBtnRect, "NHT_YAPEM_NewProfile".Translate()))
            {
                string defName = string.Format("NHT_YAPEM_NewProfileDefaultName".Translate().RawText, Profiles.Count(p => !p.IsBuiltIn) + 1);
                Find.WindowStack.Add(new Dialog_InputText("NHT_YAPEM_NewProfilePrompt".Translate(), defName, (enteredName) =>
                {
                    CreateProfile(enteredName);
                }));
            }
            x += btnW + 6f;

            bool canDelete = activeProf != null && !activeProf.IsBuiltIn;
            Rect delBtnRect = new Rect(x, y, btnW, rect.height);
            if (!canDelete)
            {
                GUI.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                TooltipHandler.TipRegion(delBtnRect, "NHT_YAPEM_CannotDeleteDefault".Translate());
            }
            else
            {
                TooltipHandler.TipRegion(delBtnRect, string.Format("NHT_YAPEM_DeleteProfileConfirm".Translate().RawText, activeProf.Label));
            }

            if (Widgets.ButtonText(delBtnRect, "NHT_YAPEM_DeleteProfile".Translate()))
            {
                if (canDelete)
                {
                    string msg = string.Format("NHT_YAPEM_DeleteProfileConfirm".Translate().RawText, activeProf.Label);
                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(msg, () =>
                    {
                        DeleteProfile(activeProf);
                    }));
                }
                else
                {
                    Messages.Message("NHT_YAPEM_CannotDeleteDefault".Translate(), MessageTypeDefOf.RejectInput, false);
                }
            }
            GUI.color = Color.white;
            x += btnW + 6f;

            float smallBtnW = 56f;
            Rect expBtnRect = new Rect(x, y, smallBtnW, rect.height);
            TooltipHandler.TipRegion(expBtnRect, "NHT_YAPEM_ProfileExported".Translate());
            if (Widgets.ButtonText(expBtnRect, "NHT_YAPEM_ExportProfile".Translate()))
            {
                if (activeProf != null)
                {
                    GUIUtility.systemCopyBuffer = activeProf.ExportJson();
                    Messages.Message("NHT_YAPEM_ProfileExported".Translate(), MessageTypeDefOf.PositiveEvent, false);
                }
            }
            x += smallBtnW + 6f;

            Rect impBtnRect = new Rect(x, y, smallBtnW, rect.height);
            TooltipHandler.TipRegion(impBtnRect, "NHT_YAPEM_ImportProfile".Translate());
            if (Widgets.ButtonText(impBtnRect, "NHT_YAPEM_ImportProfile".Translate()))
            {
                string json = GUIUtility.systemCopyBuffer;
                var imported = PaletteProfile.ImportJson(json);
                if (imported != null)
                {
                    Profiles.Add(imported);
                    ApplyProfile(imported, true);
                    Messages.Message(string.Format("NHT_YAPEM_ProfileImported".Translate().RawText, imported.Name), MessageTypeDefOf.PositiveEvent, false);
                }
                else
                {
                    Messages.Message("NHT_YAPEM_InvalidProfileJson".Translate(), MessageTypeDefOf.RejectInput, false);
                }
            }
        }

        public void ResetTierColors()
        {
            ColorAdvancedBionic = DefaultColorAdvancedBionic;
            ColorUltratech = DefaultColorUltratech;
            ColorArchotech = DefaultColorArchotech;
            ColorBionic = DefaultColorBionic;
            ColorProsthetic = DefaultColorProsthetic;
            DynamicTierColors.Clear();
            SyncActiveColorsToProfile();
            Write();
        }

        public void ResetDlcColors()
        {
            ColorRoyalty = DefaultColorRoyalty;
            ColorBiotech = DefaultColorBiotech;
            ColorAnomaly = DefaultColorAnomaly;
            ColorOdyssey = DefaultColorOdyssey;
            UseDLCColors = true;
            SyncActiveColorsToProfile();
            Write();
        }

        public void ResetColorsToDefault()
        {
            ColorAdvancedBionic = DefaultColorAdvancedBionic;
            ColorUltratech = DefaultColorUltratech;
            ColorArchotech = DefaultColorArchotech;
            ColorBionic = DefaultColorBionic;
            ColorProsthetic = DefaultColorProsthetic;

            ColorRoyalty = DefaultColorRoyalty;
            ColorBiotech = DefaultColorBiotech;
            ColorAnomaly = DefaultColorAnomaly;
            ColorOdyssey = DefaultColorOdyssey;
            ColorIntegratedImplants = DefaultColorIntegratedImplants;
        }

        public void ResetAllToDefault()
        {
            EnableBodyTinting = true;
            RequiredLimbCount = 3;
            ColorBottomIcons = true;
            OnlyShowOrgansInOrganMode = false;
            UseDLCColors = true;
            ResetColorsToDefault();
            CustomHediffTiers?.Clear();
            ModColors?.Clear();
            DynamicTierColors?.Clear();
        }

        private const int TabGeneral = 0, TabTiers = 1, TabDLC = 2, TabMods = 3, TabFineTune = 4;
        private static readonly string[] TabLabelKeys =
        {
            "NHT_YAPEM_TabGeneral", "NHT_YAPEM_TabTiers", "NHT_YAPEM_TabDLC", "NHT_YAPEM_TabMods", "NHT_YAPEM_TabFineTune"
        };
        private static int selectedTab = TabGeneral;

        // Row of tab buttons at the top; returns the area left for the tab content.
        private Rect DrawTabBar(Rect inRect)
        {
            const float tabH = 30f;
            Text.Font = GameFont.Small;
            float x = inRect.x;
            for (int i = 0; i < TabLabelKeys.Length; i++)
            {
                string label = TabLabelKeys[i].Translate();
                float w = Text.CalcSize(label).x + 28f;
                Rect r = new Rect(x, inRect.y, w, tabH);
                bool selected = i == selectedTab;
                if (selected)
                {
                    Widgets.DrawBoxSolid(r, new Color(1f, 1f, 1f, 0.12f));
                    Widgets.DrawBoxSolid(new Rect(r.x, r.yMax - 3f, r.width, 3f), new Color(0.37f, 0.66f, 1f));
                }
                else
                {
                    Widgets.DrawHighlightIfMouseover(r);
                }
                Widgets.Label(new Rect(r.x + 14f, r.y + 4f, w - 14f, tabH - 4f), label);
                if (!selected && Widgets.ButtonInvisible(r))
                {
                    selectedTab = i;
                    scrollPosition = Vector2.zero;
                }
                x += w + 2f;
            }
            Widgets.DrawBoxSolid(new Rect(inRect.x, inRect.y + tabH, inRect.width, 1f), new Color(1f, 1f, 1f, 0.2f));
            float top = tabH + 8f;
            return new Rect(inRect.x, inRect.y + top, inRect.width, inRect.height - top);
        }

        // Tiers found automatically in loaded mods (see LimbTierUtility.FindDynamicTierKey).
        private void DrawDetectedTiers(Listing_Standard listing)
        {
            List<KeyValuePair<string, string>> tiers = LimbTierUtility.GetDynamicTiers();
            if (tiers.Count == 0) return;

            listing.Gap(8f);
            listing.GapLine();
            listing.Gap(4f);
            Text.Font = GameFont.Medium;
            listing.Label("NHT_YAPEM_DetectedTiersHeader".Translate());
            Text.Font = GameFont.Small;
            listing.Label("NHT_YAPEM_DetectedTiersHint".Translate());
            listing.Gap(4f);

            foreach (KeyValuePair<string, string> tier in tiers)
            {
                string key = tier.Key;
                Color defaultColor = LimbTierUtility.GetDynamicTierDefaultColor(key);
                Color current = LimbTierUtility.GetDynamicTierColor(key);
                string source = LimbTierUtility.GetDynamicTierSource(key);
                string title = string.IsNullOrEmpty(source) ? tier.Value : tier.Value + "  (" + source + ")";
                DrawColorEntry(listing, title, ref current, defaultColor, c =>
                {
                    if (c == defaultColor) DynamicTierColors.Remove(key);
                    else DynamicTierColors[key] = ToHex(c);
                });
            }
        }

        private static bool IsThirdPartyGroup(ImplantGroup g)
        {
            switch (g.GroupKey)
            {
                case "core": case "royalty": case "biotech": case "anomaly": case "odyssey":
                    return false;
                default:
                    return !string.IsNullOrEmpty(g.GroupKey);
            }
        }

        private void DrawModsTab(Listing_Standard listing)
        {
            Text.Font = GameFont.Medium;
            listing.Label("NHT_YAPEM_TabMods".Translate());
            Text.Font = GameFont.Small;
            listing.Label("NHT_YAPEM_ModsHint".Translate());
            listing.Gap(6f);

            bool any = false;
            if (cachedGroups != null)
            {
                foreach (ImplantGroup g in cachedGroups)
                {
                    if (!IsThirdPartyGroup(g)) continue;
                    any = true;
                    DrawModColorEntry(listing, g);
                }
            }
            if (!any)
            {
                listing.Label("NHT_YAPEM_ModsNone".Translate());
            }
        }

        // Same row layout as DrawColorEntry, but "no entry" means Auto (colored by tier) instead of a default color.
        private void DrawModColorEntry(Listing_Standard listing, ImplantGroup g)
        {
            Rect entryRect = listing.GetRect(32f);
            // Must match LimbTierUtility.GetModKey(def): the real lowercase packageId (group keys are only that for some groups).
            string key = g.Defs.Count > 0 ? LimbTierUtility.GetModKey(g.Defs[0]) : null;
            if (key == null) return;
            Color shown = new Color(0.25f, 0.25f, 0.25f);
            bool hasColor = false;
            if (ModColors.TryGetValue(key, out string hex) && TryParseHex(hex, out Color parsed))
            {
                shown = parsed;
                hasColor = true;
            }

            float titleW = Mathf.Clamp(entryRect.width - 340f, 200f, 440f);
            string title = g.GroupTitle + " (" + string.Format("NHT_YAPEM_ModImplantCount".Translate().RawText, g.Defs.Count) + ")";
            Widgets.Label(new Rect(entryRect.x, entryRect.y + 4f, titleW, 24f), title);

            Action<Color> save = c =>
            {
                ModColors[key] = ToHex(c);
                Write();
            };
            Action open = () => Find.WindowStack.Add(new Dialog_TierColorPicker(g.GroupTitle, hasColor ? shown : Color.white, Color.white, save));

            Rect swatchRect = new Rect(entryRect.x + titleW + 10f, entryRect.y + 2f, 36f, 26f);
            Widgets.DrawBoxSolid(swatchRect, shown);
            Widgets.DrawBox(swatchRect, 1);
            Widgets.DrawHighlightIfMouseover(swatchRect);
            TooltipHandler.TipRegion(swatchRect, "NHT_YAPEM_PaletteTip".Translate());
            if (Widgets.ButtonInvisible(swatchRect)) open();

            Rect hexRect = new Rect(swatchRect.xMax + 10f, entryRect.y + 4f, 80f, 24f);
            Widgets.Label(hexRect, hasColor ? "#" + ColorUtility.ToHtmlStringRGB(shown) : (string)"NHT_YAPEM_ModAuto".Translate());

            Rect pickBtnRect = new Rect(hexRect.xMax + 10f, entryRect.y + 2f, 105f, 26f);
            if (Widgets.ButtonText(pickBtnRect, "NHT_YAPEM_PaletteBtn".Translate())) open();

            Rect resetBtnRect = new Rect(pickBtnRect.xMax + 8f, entryRect.y + 2f, 75f, 26f);
            if (hasColor && Widgets.ButtonText(resetBtnRect, "NHT_YAPEM_ResetBtn".Translate()))
            {
                ModColors.Remove(key);
                Write();
            }

            listing.Gap(2f);
        }

        public void DoSettingsWindowContents(Rect inRect)
        {
            try
            {
                EnsureImplantGroupsCached();
            }
            catch (Exception ex)
            {
                Log.ErrorOnce("[NiceHealthTab: Recolorized] Error caching implant groups: " + ex, 8192837);
            }

            Rect outRect = DrawTabBar(inRect);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 24f, Mathf.Max(viewHeight, outRect.height));

            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

            Listing_Standard listing = new Listing_Standard();
            listing.maxOneColumn = true;
            listing.Begin(viewRect);
            try {

            if (selectedTab == TabGeneral)
            {
            // 1. General, Body & Organ Display Options
            Text.Font = GameFont.Medium;
            listing.Label("NHT_YAPEM_SectionBody".Translate());
            Text.Font = GameFont.Small;
            listing.Gap(4f);

            listing.CheckboxLabeled(
                "NHT_YAPEM_EnableBodyTinting".Translate(),
                ref EnableBodyTinting,
                "NHT_YAPEM_EnableBodyTinting_Desc".Translate()
            );

            if (EnableBodyTinting)
            {
                string rawThreshold = "NHT_YAPEM_RequiredLimbCount".Translate().RawText;
                string thresholdText;
                try { thresholdText = string.Format(rawThreshold, RequiredLimbCount); }
                catch { thresholdText = rawThreshold; }
                listing.Label(thresholdText);
                float val = listing.Slider((float)RequiredLimbCount, 1f, 4f);
                RequiredLimbCount = Mathf.Clamp(Mathf.RoundToInt(val), 1, 4);
            }

            listing.CheckboxLabeled(
                "NHT_YAPEM_ColorBottomIcons".Translate(),
                ref ColorBottomIcons,
                "NHT_YAPEM_ColorBottomIcons_Desc".Translate()
            );

            listing.CheckboxLabeled(
                "NHT_YAPEM_OnlyShowOrgansInOrganMode".Translate(),
                ref OnlyShowOrgansInOrganMode,
                "NHT_YAPEM_OnlyShowOrgansInOrganMode_Desc".Translate()
            );

            listing.Gap(6f);
            listing.GapLine();
            listing.Gap(4f);

            }
            else if (selectedTab == TabTiers)
            {
            // 2. Base Tier Colors
            Text.Font = GameFont.Medium;
            listing.Label("NHT_YAPEM_SectionColors".Translate());
            Text.Font = GameFont.Small;
            listing.Gap(2f);
            listing.Label("NHT_YAPEM_PaletteHint".Translate());
            listing.Gap(4f);

            Rect profToolbarRect = listing.GetRect(28f);
            DrawProfileToolbar(profToolbarRect);
            listing.Gap(6f);

            DrawColorEntry(listing, "NHT_YAPEM_TierAdvBionic".Translate(), ref ColorAdvancedBionic, DefaultColorAdvancedBionic, c => ColorAdvancedBionic = c);
            DrawColorEntry(listing, "NHT_YAPEM_TierUltratech".Translate(), ref ColorUltratech, DefaultColorUltratech, c => ColorUltratech = c);
            DrawColorEntry(listing, "NHT_YAPEM_TierArchotech".Translate(), ref ColorArchotech, DefaultColorArchotech, c => ColorArchotech = c);
            DrawColorEntry(listing, "NHT_YAPEM_TierBionic".Translate(), ref ColorBionic, DefaultColorBionic, c => ColorBionic = c);
            DrawColorEntry(listing, "NHT_YAPEM_TierProsthetic".Translate(), ref ColorProsthetic, DefaultColorProsthetic, c => ColorProsthetic = c);

            DrawDetectedTiers(listing);
            }
            else if (selectedTab == TabDLC)
            {
            // 3. DLC & Mod Category Colors
            Text.Font = GameFont.Medium;
            listing.Label("NHT_YAPEM_SectionDLCColors".Translate());
            Text.Font = GameFont.Small;
            listing.Gap(4f);

            listing.CheckboxLabeled(
                "NHT_YAPEM_UseDLCColors".Translate(),
                ref UseDLCColors,
                "NHT_YAPEM_UseDLCColors_Desc".Translate()
            );

            if (UseDLCColors)
            {
                DrawColorEntry(listing, "NHT_YAPEM_TierRoyalty".Translate(), ref ColorRoyalty, DefaultColorRoyalty, c => ColorRoyalty = c);
                DrawColorEntry(listing, "NHT_YAPEM_TierBiotech".Translate(), ref ColorBiotech, DefaultColorBiotech, c => ColorBiotech = c);
                DrawColorEntry(listing, "NHT_YAPEM_TierAnomaly".Translate(), ref ColorAnomaly, DefaultColorAnomaly, c => ColorAnomaly = c);
                DrawColorEntry(listing, "NHT_YAPEM_TierOdyssey".Translate(), ref ColorOdyssey, DefaultColorOdyssey, c => ColorOdyssey = c);
            }

            }
            else if (selectedTab == TabMods)
            {
                DrawModsTab(listing);
            }
            else
            {
                // 4. Section: Fine-tuning implant categories
                DrawImplantFineTuningSection(listing, viewRect);
            }

            listing.Gap(8f);
            listing.GapLine();
            listing.Gap(6f);

            // Each tab resets only itself; General resets everything. Fine-tune has its own buttons next to the search.
            if (selectedTab != TabFineTune)
            {
                string resetKey = selectedTab == TabGeneral ? "NHT_YAPEM_ResetAllBtn"
                    : selectedTab == TabTiers ? "NHT_YAPEM_ResetTierColors"
                    : selectedTab == TabDLC ? "NHT_YAPEM_ResetDlcColors"
                    : "NHT_YAPEM_ResetModColors";
                Rect resetAllRect = listing.GetRect(32f);
                resetAllRect.width = Mathf.Min(340f, viewRect.width);
                if (Widgets.ButtonText(resetAllRect, resetKey.Translate()))
                {
                    if (selectedTab == TabGeneral) ResetAllToDefault();
                    else if (selectedTab == TabTiers) ResetTierColors();
                    else if (selectedTab == TabDLC) ResetDlcColors();
                    else { ModColors.Clear(); Write(); }
                }
            }

            }
            catch (Exception ex)
            {
                Log.ErrorOnce("[NiceHealthTab: Recolorized] Error rendering settings: " + ex, 8192838);
            }
            finally
            {
                viewHeight = Mathf.Max(listing.CurHeight + 30f, outRect.height);
                listing.End();
                Widgets.EndScrollView();
            }
        }

        private void DrawImplantFineTuningSection(Listing_Standard listing, Rect viewRect)
        {
            Text.Font = GameFont.Medium;
            listing.Label("NHT_YAPEM_SectionCustomTiers".Translate());
            Text.Font = GameFont.Small;
            listing.Gap(2f);
            listing.Label("NHT_YAPEM_SectionCustomTiers_Desc".Translate());
            listing.Gap(6f);

            if (cachedGroups == null || cachedGroups.Count == 0)
            {
                return;
            }

            // Group picker: one dropdown instead of a chip per mod (the list can hold dozens of mods).
            selectedTabGroupIndex = Mathf.Clamp(selectedTabGroupIndex, 0, cachedGroups.Count - 1);
            ImplantGroup selectedGroup = cachedGroups[selectedTabGroupIndex];
            Rect pickerRow = listing.GetRect(30f);
            Widgets.Label(new Rect(pickerRow.x, pickerRow.y + 4f, 70f, 24f), "NHT_YAPEM_GroupLabel".Translate());
            Rect pickerBtn = new Rect(pickerRow.x + 74f, pickerRow.y, Mathf.Min(420f, pickerRow.width - 74f), 30f);
            if (Widgets.ButtonText(pickerBtn, $"{selectedGroup.GroupTitle} ({selectedGroup.Defs.Count}) ▾"))
            {
                var groupOptions = new List<FloatMenuOption>();
                for (int gi = 0; gi < cachedGroups.Count; gi++)
                {
                    int captured = gi;
                    ImplantGroup g = cachedGroups[gi];
                    groupOptions.Add(new FloatMenuOption($"{g.GroupTitle} ({g.Defs.Count})", () => selectedTabGroupIndex = captured));
                }
                Find.WindowStack.Add(new FloatMenu(groupOptions));
            }

            listing.Gap(6f);

            // Search Bar & Reset Tab
            Rect searchRow = listing.GetRect(28f);

            float searchLblW = Text.CalcSize("NHT_YAPEM_SearchImplant".Translate()).x + 8f;
            Rect searchLblRect = new Rect(searchRow.x, searchRow.y + 4f, searchLblW, 24f);
            Widgets.Label(searchLblRect, "NHT_YAPEM_SearchImplant".Translate());

            Rect searchInputRect = new Rect(searchLblRect.xMax, searchRow.y + 1f, 180f, 26f);
            searchFilter = Widgets.TextField(searchInputRect, searchFilter);

            if (!string.IsNullOrEmpty(searchFilter))
            {
                Rect clearSearchRect = new Rect(searchInputRect.xMax + 4f, searchRow.y + 2f, 24f, 24f);
                if (Widgets.ButtonText(clearSearchRect, "×"))
                {
                    searchFilter = "";
                }
            }

            if (selectedTabGroupIndex >= 0 && selectedTabGroupIndex < cachedGroups.Count)
            {
                var curGroup = cachedGroups[selectedTabGroupIndex];
                string resetTabLabel = "NHT_YAPEM_ResetTabOverrides".Translate();
                float resetTabW = Text.CalcSize(resetTabLabel).x + 16f;
                Rect resetTabRect = new Rect(searchRow.xMax - resetTabW, searchRow.y + 1f, resetTabW, 26f);

                // Section-level reset: every per-implant override of every mod.
                string resetAllLabel = "NHT_YAPEM_ResetAllOverrides".Translate();
                float resetAllW = Text.CalcSize(resetAllLabel).x + 16f;
                Rect resetAllOverridesRect = new Rect(resetTabRect.x - resetAllW - 6f, searchRow.y + 1f, resetAllW, 26f);
                if (Widgets.ButtonText(resetAllOverridesRect, resetAllLabel))
                {
                    CustomHediffTiers?.Clear();
                }
                if (Widgets.ButtonText(resetTabRect, resetTabLabel))
                {
                    if (CustomHediffTiers != null)
                    {
                        foreach (var d in curGroup.Defs)
                        {
                            CustomHediffTiers.Remove(d.defName);
                        }
                    }
                }
            }

            listing.Gap(4f);

            // Draw Implants List for current group
            selectedTabGroupIndex = Mathf.Clamp(selectedTabGroupIndex, 0, Mathf.Max(0, cachedGroups.Count - 1));
            if (selectedTabGroupIndex >= 0 && selectedTabGroupIndex < cachedGroups.Count)
            {
                var currentGroup = cachedGroups[selectedTabGroupIndex];
                int rowIndex = 0;

                for (int i = 0; i < currentGroup.Defs.Count; i++)
                {
                    HediffDef def = currentGroup.Defs[i];
                    if (def == null) continue;

                    string label = (def.label != null) ? def.LabelCap.Resolve() : (def.defName ?? "");
                    string defName = def.defName ?? "";

                    if (!string.IsNullOrEmpty(searchFilter))
                    {
                        if (label.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) < 0 &&
                            defName.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            continue;
                        }
                    }

                    Rect rowRect = listing.GetRect(34f);
                    if (rowIndex % 2 == 1)
                    {
                        Widgets.DrawAltRect(rowRect);
                    }
                    Widgets.DrawHighlightIfMouseover(rowRect);

                    if (!def.description.NullOrEmpty())
                    {
                        TooltipHandler.TipRegion(rowRect, $"{label}\n{defName}\n\n{def.description}");
                    }
                    else
                    {
                        TooltipHandler.TipRegion(rowRect, $"{label}\n{defName}");
                    }

                    // Icon
                    Rect iconRect = new Rect(rowRect.x + 4f, rowRect.y + 4f, 26f, 26f);
                    ThingDef spawnThing = def.spawnThingOnRemoved;
                    try
                    {
                        if (spawnThing != null)
                        {
                            Widgets.DefIcon(iconRect, spawnThing);
                        }
                        else
                        {
                            Widgets.DrawBoxSolid(iconRect, new Color(0.2f, 0.2f, 0.2f, 0.3f));
                        }
                    }
                    catch
                    {
                        Widgets.DrawBoxSolid(iconRect, new Color(0.2f, 0.2f, 0.2f, 0.3f));
                    }

                    // Labels
                    float labelX = iconRect.xMax + 8f;
                    float rightControlsW = 230f;
                    float labelW = rowRect.width - (labelX - rowRect.x) - rightControlsW - 8f;

                    Rect titleRect = new Rect(labelX, rowRect.y + 2f, labelW, 16f);
                    Rect subRect = new Rect(labelX, rowRect.y + 18f, labelW, 14f);

                    Text.Font = GameFont.Small;
                    Widgets.Label(titleRect, label);
                    Text.Font = GameFont.Tiny;
                    GUI.color = new Color(0.7f, 0.7f, 0.7f, 0.75f);
                    Widgets.Label(subRect, defName);
                    GUI.color = Color.white;
                    Text.Font = GameFont.Small;

                    // Effective tier and color
                    string overrideTier = null;
                    bool isOverridden = CustomHediffTiers != null && CustomHediffTiers.TryGetValue(defName, out overrideTier);
                    string currentTier;
                    Color currentColor;
                    bool hasTint = LimbTierUtility.CategorizeHediffDefTier(def, out currentTier, out currentColor);

                    // Swatch
                    Rect swatchRect = new Rect(rowRect.xMax - rightControlsW, rowRect.y + 6f, 22f, 22f);
                    if (hasTint)
                    {
                        Widgets.DrawBoxSolid(swatchRect, currentColor);
                        Widgets.DrawBox(swatchRect, 1);
                    }
                    else
                    {
                        Widgets.DrawBoxSolid(swatchRect, new Color(0.2f, 0.2f, 0.2f, 0.5f));
                        Widgets.DrawBox(swatchRect, 1);
                        GUI.color = new Color(0.6f, 0.6f, 0.6f, 0.8f);
                        Widgets.DrawLine(new Vector2(swatchRect.x, swatchRect.y), new Vector2(swatchRect.xMax, swatchRect.yMax), GUI.color, 1f);
                        GUI.color = Color.white;
                    }

                    // Dropdown button
                    string btnLabel;
                    if (isOverridden)
                    {
                        btnLabel = $"★ {LimbTierUtility.GetTierLabel(overrideTier)} ▾";
                    }
                    else
                    {
                        btnLabel = $"{"NHT_YAPEM_TierAuto".Translate()} ▾";
                    }

                    Rect btnRect = new Rect(swatchRect.xMax + 6f, rowRect.y + 4f, 160f, 26f);
                    if (Widgets.ButtonText(btnRect, btnLabel))
                    {
                        OpenTierSelectionMenu(def);
                    }

                    // Reset button if overridden
                    if (isOverridden)
                    {
                        Rect resetItemRect = new Rect(btnRect.xMax + 4f, rowRect.y + 4f, 26f, 26f);
                        if (Widgets.ButtonText(resetItemRect, "↺"))
                        {
                            CustomHediffTiers.Remove(defName);
                        }
                        TooltipHandler.TipRegion(resetItemRect, "NHT_YAPEM_TierAuto".Translate());
                    }

                    rowIndex++;
                    listing.Gap(2f);
                }
            }
        }

        private void OpenTierSelectionMenu(HediffDef def)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();

            // 1. Auto (default)
            options.Add(new FloatMenuOption(
                "NHT_YAPEM_TierAuto".Translate(),
                () => {
                    CustomHediffTiers?.Remove(def.defName);
                },
                BaseContent.WhiteTex,
                Color.white
            ));

            // 2. None (no tint)
            options.Add(new FloatMenuOption(
                "NHT_YAPEM_TierNone".Translate(),
                () => {
                    CustomHediffTiers[def.defName] = "None";
                },
                BaseContent.WhiteTex,
                new Color(0.4f, 0.4f, 0.4f)
            ));

            // 3. Available categories
            var tiers = new (string key, string label, Color color)[]
            {
                ("Archotech",  "NHT_YAPEM_TierArchotech".Translate(),  LimbTierUtility.ColorArchotech),
                ("AdvBionic",  "NHT_YAPEM_TierAdvBionic".Translate(),  LimbTierUtility.ColorAdvancedBionic),
                ("Ultratech",  "NHT_YAPEM_TierUltratech".Translate(),  LimbTierUtility.ColorUltratech),
                ("Bionic",     "NHT_YAPEM_TierBionic".Translate(),     LimbTierUtility.ColorBionic),
                ("Prosthetic", "NHT_YAPEM_TierProsthetic".Translate(), LimbTierUtility.ColorProsthetic),
                ("Royalty",    "NHT_YAPEM_TierRoyalty".Translate(),    LimbTierUtility.ColorRoyalty),
                ("Biotech",    "NHT_YAPEM_TierBiotech".Translate(),    LimbTierUtility.ColorBiotech),
                ("Anomaly",    "NHT_YAPEM_TierAnomaly".Translate(),    LimbTierUtility.ColorAnomaly),
                ("Odyssey",    "NHT_YAPEM_TierOdyssey".Translate(),    LimbTierUtility.ColorOdyssey)
            };

            foreach (var t in tiers)
            {
                string tierKey = t.key;
                options.Add(new FloatMenuOption(
                    t.label,
                    () => {
                        CustomHediffTiers[def.defName] = tierKey;
                    },
                    BaseContent.WhiteTex,
                    t.color
                ));
            }

            // 4. Tiers added by mods, detected automatically
            foreach (KeyValuePair<string, string> dyn in LimbTierUtility.GetDynamicTiers())
            {
                string dynKey = dyn.Key;
                options.Add(new FloatMenuOption(
                    dyn.Value,
                    () => {
                        CustomHediffTiers[def.defName] = dynKey;
                    },
                    BaseContent.WhiteTex,
                    LimbTierUtility.GetDynamicTierColor(dynKey)
                ));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void EnsureImplantGroupsCached()
        {
            if (cachedGroups != null) return;

            cachedGroups = new List<ImplantGroup>();

            ImplantGroup coreGroup = new ImplantGroup { GroupKey = "core", GroupTitle = "Core" };
            ImplantGroup royaltyGroup = new ImplantGroup { GroupKey = "royalty", GroupTitle = "Royalty" };
            ImplantGroup biotechGroup = new ImplantGroup { GroupKey = "biotech", GroupTitle = "Biotech" };
            ImplantGroup anomalyGroup = new ImplantGroup { GroupKey = "anomaly", GroupTitle = "Anomaly" };
            ImplantGroup odysseyGroup = new ImplantGroup { GroupKey = "odyssey", GroupTitle = "Odyssey" };
            ImplantGroup integratedGroup = new ImplantGroup { GroupKey = "integrated", GroupTitle = "Integrated Implants" };

            Dictionary<string, ImplantGroup> modGroups = new Dictionary<string, ImplantGroup>(StringComparer.OrdinalIgnoreCase);

            var allHediffs = DefDatabase<HediffDef>.AllDefsListForReading;
            if (allHediffs == null) return;

            for (int i = 0; i < allHediffs.Count; i++)
            {
                HediffDef h = allHediffs[i];
                if (h == null) continue;

                if (!IsImplantOrProstheticDef(h)) continue;

                string pkg = h.modContentPack?.PackageIdPlayerFacing?.ToLowerInvariant() ?? "";
                string modName = h.modContentPack?.Name ?? pkg;

                if (pkg == "ludeon.rimworld")
                {
                    coreGroup.Defs.Add(h);
                }
                else if (pkg == "ludeon.rimworld.royalty" || LimbTierUtility.RoyaltyDefs.Contains(h.defName))
                {
                    royaltyGroup.Defs.Add(h);
                }
                else if (pkg == "ludeon.rimworld.biotech" || LimbTierUtility.BiotechDefs.Contains(h.defName))
                {
                    biotechGroup.Defs.Add(h);
                }
                else if (pkg == "ludeon.rimworld.anomaly" || LimbTierUtility.AnomalyDefs.Contains(h.defName))
                {
                    anomalyGroup.Defs.Add(h);
                }
                else if (pkg == "ludeon.rimworld.odyssey" || modName.IndexOf("Odyssey", StringComparison.OrdinalIgnoreCase) >= 0 || LimbTierUtility.OdysseyDefs.Contains(h.defName))
                {
                    odysseyGroup.Defs.Add(h);
                }
                else if (pkg == "lts.i" || modName.IndexOf("Integrated Implants", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    integratedGroup.Defs.Add(h);
                }
                else
                {
                    string key = string.IsNullOrEmpty(modName) ? "Unknown" : modName;
                    if (!modGroups.TryGetValue(key, out ImplantGroup grp))
                    {
                        grp = new ImplantGroup { GroupKey = pkg, GroupTitle = key };
                        modGroups[key] = grp;
                    }
                    grp.Defs.Add(h);
                }
            }

            if (coreGroup.Defs.Count > 0) cachedGroups.Add(coreGroup);
            if (royaltyGroup.Defs.Count > 0) cachedGroups.Add(royaltyGroup);
            if (biotechGroup.Defs.Count > 0) cachedGroups.Add(biotechGroup);
            if (anomalyGroup.Defs.Count > 0) cachedGroups.Add(anomalyGroup);
            if (odysseyGroup.Defs.Count > 0) cachedGroups.Add(odysseyGroup);
            if (integratedGroup.Defs.Count > 0) cachedGroups.Add(integratedGroup);

            var sortedModGroups = modGroups.Values.OrderBy(g => g.GroupTitle).ToList();
            foreach (var g in sortedModGroups)
            {
                if (g.Defs.Count > 0)
                {
                    cachedGroups.Add(g);
                }
            }

            foreach (var grp in cachedGroups)
            {
                grp.Defs.Sort((a, b) => {
                    string la = (a?.label != null) ? a.LabelCap.Resolve() : (a?.defName ?? "");
                    string lb = (b?.label != null) ? b.LabelCap.Resolve() : (b?.defName ?? "");
                    return string.Compare(la, lb, StringComparison.CurrentCultureIgnoreCase);
                });
            }
        }

        private static bool IsImplantOrProstheticDef(HediffDef h)
        {
            if (h == null) return false;

            if (h.isBad && h.addedPartProps == null && !h.countsAsAddedPartOrImplant) return false;

            if (h.countsAsAddedPartOrImplant) return true;
            if (h.addedPartProps != null) return true;
            if (h.hediffClass != null && (typeof(Hediff_AddedPart).IsAssignableFrom(h.hediffClass) || typeof(Hediff_Implant).IsAssignableFrom(h.hediffClass))) return true;
            if (h.spawnThingOnRemoved != null && h.spawnThingOnRemoved.isTechHediff) return true;

            if (LimbTierUtility.RoyaltyDefs.Contains(h.defName) ||
                LimbTierUtility.BiotechDefs.Contains(h.defName) ||
                LimbTierUtility.AnomalyDefs.Contains(h.defName) ||
                LimbTierUtility.OdysseyDefs.Contains(h.defName))
            {
                return true;
            }

            return false;
        }

        private void DrawColorEntry(Listing_Standard listing, string title, ref Color color, Color defaultColor, Action<Color> saveAction)
        {
            Rect entryRect = listing.GetRect(32f);

            float titleW = Mathf.Clamp(entryRect.width - 340f, 200f, 440f);

            // 1. Title
            Rect titleRect = new Rect(entryRect.x, entryRect.y + 4f, titleW, 24f);
            Widgets.Label(titleRect, title);

            Action<Color> wrappedAction = c =>
            {
                saveAction?.Invoke(c);
                SyncActiveColorsToProfile();
                Write();
            };

            // 2. Clickable Color Swatch Square
            Rect swatchRect = new Rect(titleRect.xMax + 10f, entryRect.y + 2f, 36f, 26f);
            Widgets.DrawBoxSolid(swatchRect, color);
            Widgets.DrawBox(swatchRect, 1);
            Widgets.DrawHighlightIfMouseover(swatchRect);
            TooltipHandler.TipRegion(swatchRect, "NHT_YAPEM_PaletteTip".Translate());
            if (Widgets.ButtonInvisible(swatchRect))
            {
                Color cur = color;
                Find.WindowStack.Add(new Dialog_TierColorPicker(title, cur, defaultColor, wrappedAction));
            }

            // 3. Hex code display
            Rect hexRect = new Rect(swatchRect.xMax + 10f, entryRect.y + 4f, 80f, 24f);
            Widgets.Label(hexRect, $"#{ColorUtility.ToHtmlStringRGB(color)}");

            // 4. "Palette..." button
            Rect pickBtnRect = new Rect(hexRect.xMax + 10f, entryRect.y + 2f, 105f, 26f);
            if (Widgets.ButtonText(pickBtnRect, "NHT_YAPEM_PaletteBtn".Translate()))
            {
                Color cur = color;
                Find.WindowStack.Add(new Dialog_TierColorPicker(title, cur, defaultColor, wrappedAction));
            }

            // 5. Reset button
            Rect resetBtnRect = new Rect(pickBtnRect.xMax + 8f, entryRect.y + 2f, 75f, 26f);
            if (Widgets.ButtonText(resetBtnRect, "NHT_YAPEM_ResetBtn".Translate()))
            {
                color = defaultColor;
                wrappedAction(defaultColor);
            }

            listing.Gap(2f);
        }
    }
}
