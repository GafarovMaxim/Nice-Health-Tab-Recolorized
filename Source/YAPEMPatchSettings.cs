using System;
using UnityEngine;
using Verse;

namespace NiceHealthTab_YAPEM_Patch
{
    public class YAPEMPatchSettings : ModSettings
    {
        // Canonical defaults - Base Tiers (Nicer Health Tab original palette)
        public static readonly Color DefaultColorAdvancedBionic = new Color(161f / 255f, 206f / 255f, 243f / 255f, 1f); // #A1CEF3 Sky Blue
        public static readonly Color DefaultColorUltratech      = new Color(113f / 255f, 203f / 255f, 194f / 255f, 1f); // #71CBC2 Mint Turquoise
        public static readonly Color DefaultColorArchotech      = new Color(203f / 255f, 208f / 255f, 171f / 255f, 1f); // #CBD0AB Sage Green
        public static readonly Color DefaultColorBionic         = new Color(230f / 255f, 230f / 255f, 230f / 255f, 1f); // #E6E6E6 Clean Metal White
        public static readonly Color DefaultColorProsthetic     = new Color(176f / 255f, 176f / 255f, 176f / 255f, 1f); // #B0B0B0 Silver Grey

        // Canonical defaults - DLC Categories
        public static readonly Color DefaultColorRoyalty        = new Color(222f / 255f, 184f / 255f, 65f / 255f, 1f);  // #DEB841 Imperial Gold
        public static readonly Color DefaultColorBiotech        = new Color(56f / 255f, 217f / 255f, 169f / 255f, 1f); // #38D9A9 Bio Mint
        public static readonly Color DefaultColorAnomaly        = new Color(168f / 255f, 85f / 255f, 247f / 255f, 1f); // #A855F7 Void Violet
        public static readonly Color DefaultColorOdyssey        = new Color(56f / 255f, 189f / 255f, 248f / 255f, 1f); // #38BDF8 Cosmic Sky

        // Body substrate and icon toggles
        public bool EnableBodyTinting = true;
        public int RequiredLimbCount = 3;
        public bool ColorBottomIcons = true;
        public bool UseDLCColors = true;

        // Custom colors - Base Tiers
        public Color ColorAdvancedBionic = DefaultColorAdvancedBionic;
        public Color ColorUltratech      = DefaultColorUltratech;
        public Color ColorArchotech      = DefaultColorArchotech;
        public Color ColorBionic         = DefaultColorBionic;
        public Color ColorProsthetic     = DefaultColorProsthetic;

        // Custom colors - DLC Categories
        public Color ColorRoyalty        = DefaultColorRoyalty;
        public Color ColorBiotech        = DefaultColorBiotech;
        public Color ColorAnomaly        = DefaultColorAnomaly;
        public Color ColorOdyssey        = DefaultColorOdyssey;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref EnableBodyTinting, "EnableBodyTinting", true);
            Scribe_Values.Look(ref RequiredLimbCount, "RequiredLimbCount", 3);
            Scribe_Values.Look(ref ColorBottomIcons, "ColorBottomIcons", true);
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
        }

        public void ResetAllToDefault()
        {
            EnableBodyTinting = true;
            RequiredLimbCount = 3;
            ColorBottomIcons = true;
            UseDLCColors = true;
            ResetColorsToDefault();
        }

        public void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            // 1. General & Body Tinting Options
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
                string thresholdText = "NHT_YAPEM_RequiredLimbCount".Translate(RequiredLimbCount);
                listing.Label(thresholdText);
                float val = listing.Slider(RequiredLimbCount, 1f, 4f);
                RequiredLimbCount = Mathf.Clamp(Mathf.RoundToInt(val), 1, 4);
            }

            listing.CheckboxLabeled(
                "NHT_YAPEM_ColorBottomIcons".Translate(),
                ref ColorBottomIcons,
                "NHT_YAPEM_ColorBottomIcons_Desc".Translate()
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

            // 3. DLC Category Colors
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

            listing.Gap(8f);
            listing.GapLine();
            listing.Gap(6f);

            Rect resetAllRect = listing.GetRect(32f);
            resetAllRect.width = Mathf.Min(340f, inRect.width);
            if (Widgets.ButtonText(resetAllRect, "NHT_YAPEM_ResetAllBtn".Translate()))
            {
                ResetAllToDefault();
            }

            listing.End();
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
