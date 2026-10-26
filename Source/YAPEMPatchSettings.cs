using System;
using UnityEngine;
using Verse;

namespace NiceHealthTab_YAPEM_Patch
{
    public class YAPEMPatchSettings : ModSettings
    {
        // Canonical defaults
        public static readonly Color DefaultColorAdvancedBionic = new Color(161f / 255f, 206f / 255f, 243f / 255f, 1f); // #A1CEF3
        public static readonly Color DefaultColorUltratech      = new Color(113f / 255f, 203f / 255f, 194f / 255f, 1f); // #71CBC2
        public static readonly Color DefaultColorArchotech      = new Color(203f / 255f, 208f / 255f, 171f / 255f, 1f); // #CBD0AB
        public static readonly Color DefaultColorBionic         = new Color(0.9f, 0.9f, 0.9f, 1f);                      // #E6E6E6
        public static readonly Color DefaultColorProsthetic     = new Color(0.85f, 0.85f, 0.85f, 1f);

        // Body substrate and icon toggles
        public bool EnableBodyTinting = true;
        public int RequiredLimbCount = 3;
        public bool ColorBottomIcons = true;

        // Custom colors
        public Color ColorAdvancedBionic = DefaultColorAdvancedBionic;
        public Color ColorUltratech      = DefaultColorUltratech;
        public Color ColorArchotech      = DefaultColorArchotech;
        public Color ColorBionic         = DefaultColorBionic;
        public Color ColorProsthetic     = DefaultColorProsthetic;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref EnableBodyTinting, "EnableBodyTinting", true);
            Scribe_Values.Look(ref RequiredLimbCount, "RequiredLimbCount", 3);
            Scribe_Values.Look(ref ColorBottomIcons, "ColorBottomIcons", true);

            Scribe_Values.Look(ref ColorAdvancedBionic, "ColorAdvancedBionic", DefaultColorAdvancedBionic);
            Scribe_Values.Look(ref ColorUltratech, "ColorUltratech", DefaultColorUltratech);
            Scribe_Values.Look(ref ColorArchotech, "ColorArchotech", DefaultColorArchotech);
            Scribe_Values.Look(ref ColorBionic, "ColorBionic", DefaultColorBionic);
            Scribe_Values.Look(ref ColorProsthetic, "ColorProsthetic", DefaultColorProsthetic);
        }

        public void ResetColorsToDefault()
        {
            ColorAdvancedBionic = DefaultColorAdvancedBionic;
            ColorUltratech = DefaultColorUltratech;
            ColorArchotech = DefaultColorArchotech;
            ColorBionic = DefaultColorBionic;
            ColorProsthetic = DefaultColorProsthetic;
        }

        public void ResetAllToDefault()
        {
            EnableBodyTinting = true;
            RequiredLimbCount = 3;
            ColorBottomIcons = true;
            ResetColorsToDefault();
        }

        public void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            Text.Font = GameFont.Medium;
            listing.Label("NHT_YAPEM_SectionBody".Translate());
            Text.Font = GameFont.Small;
            listing.Gap(6f);

            listing.CheckboxLabeled(
                "NHT_YAPEM_EnableBodyTinting".Translate(),
                ref EnableBodyTinting,
                "NHT_YAPEM_EnableBodyTinting_Desc".Translate()
            );

            if (EnableBodyTinting)
            {
                listing.Gap(4f);
                string thresholdText = "NHT_YAPEM_RequiredLimbCount".Translate(RequiredLimbCount);
                listing.Label(thresholdText);
                float val = listing.Slider(RequiredLimbCount, 1f, 4f);
                RequiredLimbCount = Mathf.Clamp(Mathf.RoundToInt(val), 1, 4);
            }

            listing.Gap(4f);
            listing.CheckboxLabeled(
                "NHT_YAPEM_ColorBottomIcons".Translate(),
                ref ColorBottomIcons,
                "NHT_YAPEM_ColorBottomIcons_Desc".Translate()
            );

            listing.Gap(12f);
            listing.GapLine();
            listing.Gap(8f);

            Text.Font = GameFont.Medium;
            listing.Label("NHT_YAPEM_SectionColors".Translate());
            Text.Font = GameFont.Small;
            listing.Gap(2f);
            listing.Label("NHT_YAPEM_PaletteHint".Translate());
            listing.Gap(8f);

            DrawColorEntry(listing, "NHT_YAPEM_TierAdvBionic".Translate(), ref ColorAdvancedBionic, DefaultColorAdvancedBionic, c => ColorAdvancedBionic = c);
            DrawColorEntry(listing, "NHT_YAPEM_TierUltratech".Translate(), ref ColorUltratech, DefaultColorUltratech, c => ColorUltratech = c);
            DrawColorEntry(listing, "NHT_YAPEM_TierArchotech".Translate(), ref ColorArchotech, DefaultColorArchotech, c => ColorArchotech = c);
            DrawColorEntry(listing, "NHT_YAPEM_TierBionic".Translate(), ref ColorBionic, DefaultColorBionic, c => ColorBionic = c);
            DrawColorEntry(listing, "NHT_YAPEM_TierProsthetic".Translate(), ref ColorProsthetic, DefaultColorProsthetic, c => ColorProsthetic = c);

            listing.Gap(12f);
            listing.GapLine();
            listing.Gap(8f);

            Rect resetAllRect = listing.GetRect(32f);
            resetAllRect.width = 380f;
            if (Widgets.ButtonText(resetAllRect, "NHT_YAPEM_ResetAllBtn".Translate()))
            {
                ResetAllToDefault();
            }

            listing.End();
        }

        private void DrawColorEntry(Listing_Standard listing, string title, ref Color color, Color defaultColor, Action<Color> saveAction)
        {
            Rect entryRect = listing.GetRect(34f);

            // 1. Title
            Rect titleRect = new Rect(entryRect.x, entryRect.y + 4f, 370f, 24f);
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
            Rect hexRect = new Rect(swatchRect.xMax + 12f, entryRect.y + 4f, 85f, 24f);
            Widgets.Label(hexRect, $"#{ColorUtility.ToHtmlStringRGB(color)}");

            // 4. "Palette..." button
            Rect pickBtnRect = new Rect(hexRect.xMax + 10f, entryRect.y + 2f, 120f, 26f);
            if (Widgets.ButtonText(pickBtnRect, "NHT_YAPEM_PaletteBtn".Translate()))
            {
                Color cur = color;
                Find.WindowStack.Add(new Dialog_TierColorPicker(title, cur, defaultColor, saveAction));
            }

            // 5. Reset button
            Rect resetBtnRect = new Rect(pickBtnRect.xMax + 10f, entryRect.y + 2f, 90f, 26f);
            if (Widgets.ButtonText(resetBtnRect, "NHT_YAPEM_ResetBtn".Translate()))
            {
                color = defaultColor;
            }

            listing.Gap(4f);
        }
    }
}
