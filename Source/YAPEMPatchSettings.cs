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
        public static readonly Color DefaultColorRoyalty            = new Color(222f / 255f, 184f / 255f, 65f / 255f, 1f);  // #DEB841 Imperial Gold
        public static readonly Color DefaultColorBiotech            = new Color(56f / 255f, 217f / 255f, 169f / 255f, 1f); // #38D9A9 Bio Mint
        public static readonly Color DefaultColorAnomaly            = new Color(168f / 255f, 85f / 255f, 247f / 255f, 1f); // #A855F7 Void Violet
        public static readonly Color DefaultColorOdyssey            = new Color(56f / 255f, 189f / 255f, 248f / 255f, 1f); // #38BDF8 Cosmic Sky
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

        // Scroll view state
        private Vector2 scrollPosition = Vector2.zero;
        private float viewHeight = 950f;

        // UI state for Implant Fine-Tuning section
        private static List<ImplantGroup> cachedGroups = null;
        private static int selectedTabGroupIndex = 0;
        private static string searchFilter = "";

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

            Scribe_Collections.Look(ref CustomHediffTiers, "CustomHediffTiers", LookMode.Value, LookMode.Value);
            if (CustomHediffTiers == null)
            {
                CustomHediffTiers = new Dictionary<string, string>();
            }
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

            Rect outRect = inRect;
            Rect viewRect = new Rect(0f, 0f, inRect.width - 24f, Mathf.Max(viewHeight, inRect.height));

            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(viewRect);
            try {

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

            // 2. Base Tier Colors
            Text.Font = GameFont.Medium;
            listing.Label("NHT_YAPEM_SectionColors".Translate());
            Text.Font = GameFont.Small;
            listing.Gap(2f);
            listing.Label("NHT_YAPEM_PaletteHint".Translate());
            listing.Gap(4f);

            DrawColorEntry(listing, "NHT_YAPEM_TierAdvBionic".Translate(), ref ColorAdvancedBionic, DefaultColorAdvancedBionic, c => ColorAdvancedBionic = c);
            DrawColorEntry(listing, "NHT_YAPEM_TierUltratech".Translate(), ref ColorUltratech, DefaultColorUltratech, c => ColorUltratech = c);
            DrawColorEntry(listing, "NHT_YAPEM_TierArchotech".Translate(), ref ColorArchotech, DefaultColorArchotech, c => ColorArchotech = c);
            DrawColorEntry(listing, "NHT_YAPEM_TierBionic".Translate(), ref ColorBionic, DefaultColorBionic, c => ColorBionic = c);
            DrawColorEntry(listing, "NHT_YAPEM_TierProsthetic".Translate(), ref ColorProsthetic, DefaultColorProsthetic, c => ColorProsthetic = c);

            listing.Gap(6f);
            listing.GapLine();
            listing.Gap(4f);

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
                DrawColorEntry(listing, "NHT_YAPEM_TierIntegrated".Translate(), ref ColorIntegratedImplants, DefaultColorIntegratedImplants, c => ColorIntegratedImplants = c);
            }

            listing.Gap(8f);
            listing.GapLine();
            listing.Gap(6f);

            // 4. Section: Fine-tuning implant categories
            DrawImplantFineTuningSection(listing, viewRect);

            listing.Gap(8f);
            listing.GapLine();
            listing.Gap(6f);

            Rect resetAllRect = listing.GetRect(32f);
            resetAllRect.width = Mathf.Min(340f, viewRect.width);
            if (Widgets.ButtonText(resetAllRect, "NHT_YAPEM_ResetAllBtn".Translate()))
            {
                ResetAllToDefault();
            }

            }
            catch (Exception ex)
            {
                Log.ErrorOnce("[NiceHealthTab: Recolorized] Error rendering settings: " + ex, 8192838);
            }
            finally
            {
                viewHeight = Mathf.Max(listing.CurHeight + 30f, inRect.height);
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

            // Draw Tabs
            float containerWidth = listing.ColumnWidth;
            float curX = 0f;
            float curY = 0f;
            float rowH = 28f;
            float gap = 4f;

            // Measure tab bar height
            for (int i = 0; i < cachedGroups.Count; i++)
            {
                string tabTitle = $"{cachedGroups[i].GroupTitle} ({cachedGroups[i].Defs.Count})";
                float btnW = Mathf.Max(80f, Text.CalcSize(tabTitle).x + 16f);
                if (curX + btnW > containerWidth && curX > 0f)
                {
                    curX = 0f;
                    curY += rowH + gap;
                }
                curX += btnW + gap;
            }
            float totalTabH = curY + rowH;

            Rect tabsRect = listing.GetRect(totalTabH);
            curX = 0f;
            curY = 0f;

            for (int i = 0; i < cachedGroups.Count; i++)
            {
                var grp = cachedGroups[i];
                string tabTitle = $"{grp.GroupTitle} ({grp.Defs.Count})";
                float btnW = Mathf.Max(80f, Text.CalcSize(tabTitle).x + 16f);

                if (curX + btnW > containerWidth && curX > 0f)
                {
                    curX = 0f;
                    curY += rowH + gap;
                }

                Rect btnRect = new Rect(tabsRect.x + curX, tabsRect.y + curY, btnW, rowH);
                bool isSelected = (i == selectedTabGroupIndex);

                if (isSelected)
                {
                    Widgets.DrawBoxSolid(btnRect, new Color(0.22f, 0.45f, 0.65f, 0.45f));
                    Widgets.DrawHighlightSelected(btnRect);
                    Widgets.DrawBox(btnRect, 1);
                }

                if (Widgets.ButtonText(btnRect, tabTitle, drawBackground: !isSelected))
                {
                    selectedTabGroupIndex = i;
                }

                curX += btnW + gap;
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
                    if (spawnThing != null)
                    {
                        Widgets.DefIcon(iconRect, spawnThing);
                    }
                    else
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
                ("Odyssey",    "NHT_YAPEM_TierOdyssey".Translate(),    LimbTierUtility.ColorOdyssey),
                ("Integrated", "NHT_YAPEM_TierIntegrated".Translate(), LimbTierUtility.ColorIntegratedImplants)
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

            // 2. Clickable Color Swatch Square
            Rect swatchRect = new Rect(titleRect.xMax + 10f, entryRect.y + 2f, 36f, 26f);
            Widgets.DrawBoxSolid(swatchRect, color);
            Widgets.DrawBox(swatchRect, 1);
            Widgets.DrawHighlightIfMouseover(swatchRect);
            TooltipHandler.TipRegion(swatchRect, "NHT_YAPEM_PaletteTip".Translate());
            if (Widgets.ButtonInvisible(swatchRect))
            {
                Color cur = color;
                Find.WindowStack.Add(new Dialog_TierColorPicker(title, cur, defaultColor, saveAction));
            }

            // 3. Hex code display
            Rect hexRect = new Rect(swatchRect.xMax + 10f, entryRect.y + 4f, 80f, 24f);
            Widgets.Label(hexRect, $"#{ColorUtility.ToHtmlStringRGB(color)}");

            // 4. "Palette..." button
            Rect pickBtnRect = new Rect(hexRect.xMax + 10f, entryRect.y + 2f, 105f, 26f);
            if (Widgets.ButtonText(pickBtnRect, "NHT_YAPEM_PaletteBtn".Translate()))
            {
                Color cur = color;
                Find.WindowStack.Add(new Dialog_TierColorPicker(title, cur, defaultColor, saveAction));
            }

            // 5. Reset button
            Rect resetBtnRect = new Rect(pickBtnRect.xMax + 8f, entryRect.y + 2f, 75f, 26f);
            if (Widgets.ButtonText(resetBtnRect, "NHT_YAPEM_ResetBtn".Translate()))
            {
                color = defaultColor;
                saveAction?.Invoke(defaultColor);
            }

            listing.Gap(2f);
        }
    }
}
