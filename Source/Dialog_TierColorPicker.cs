using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace NiceHealthTab_YAPEM_Patch
{
    public class Dialog_TierColorPicker : Dialog_ColorPickerBase
    {
        private static readonly List<Color> DefaultPresets = new List<Color>
        {
            YAPEMPatchSettings.DefaultColorAdvancedBionic,
            YAPEMPatchSettings.DefaultColorUltratech,
            YAPEMPatchSettings.DefaultColorArchotech,
            YAPEMPatchSettings.DefaultColorBionic,
            YAPEMPatchSettings.DefaultColorProsthetic,
            new Color(0.2f, 0.6f, 1f),    // Deep Sky Blue
            new Color(0.1f, 0.85f, 0.75f),// Mint / Turquoise
            new Color(0.35f, 0.9f, 0.45f),// Neon Green
            new Color(0.95f, 0.8f, 0.2f), // Cyber Gold
            new Color(1f, 0.45f, 0.3f),   // Coral / Orange
            new Color(0.95f, 0.35f, 0.65f),// Pink / Magenta
            new Color(0.7f, 0.45f, 0.95f),// Purple
            Color.white,
            new Color(0.75f, 0.75f, 0.75f),
            new Color(0.45f, 0.45f, 0.45f)
        };

        private readonly Action<Color> onColorSaved;
        private readonly Color defaultColor;
        private bool accepted;

        public Dialog_TierColorPicker(string title, Color currentColor, Color defaultColor, Action<Color> onColorSaved)
            : base(Widgets.ColorComponents.All, Widgets.ColorComponents.All)
        {
            this.optionalTitle = title;
            this.color = currentColor;
            this.oldColor = currentColor;
            this.defaultColor = defaultColor;
            this.onColorSaved = onColorSaved;
            this.closeOnClickedOutside = true;
            this.absorbInputAroundWindow = true;
        }

        public override Vector2 InitialSize => new Vector2(620f, 480f);

        protected override Color DefaultColor => defaultColor;
        protected override bool ShowDarklight => false;
        protected override List<Color> PickableColors => DefaultPresets;
        protected override float ForcedColorValue => -1f;
        protected override bool ShowColorTemperatureBar => false;

        public override void DoWindowContents(Rect inRect)
        {
            base.DoWindowContents(inRect);
            // Live update while dragging in the dialog
            onColorSaved?.Invoke(this.color);
        }

        protected override void SaveColor(Color newColor)
        {
            accepted = true;
            onColorSaved?.Invoke(newColor);
        }

        public override void PreClose()
        {
            base.PreClose();
            if (!accepted)
            {
                onColorSaved?.Invoke(oldColor);
            }
        }
    }
}
