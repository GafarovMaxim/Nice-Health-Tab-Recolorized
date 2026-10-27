using UnityEngine;
using Verse;

namespace NiceHealthTab_YAPEM_Patch
{
    public class YAPEMPatchMod : Verse.Mod
    {
        public static YAPEMPatchSettings Settings { get; private set; }

        public YAPEMPatchMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<YAPEMPatchSettings>();
        }

        public override string SettingsCategory()
        {
            return "NHT_YAPEM_ModCategory".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Rect left = inRect;
            if (DollPreview.Show)
            {
                left.width -= DollPreview.PanelWidth + 8f;
                DollPreview.DrawPanel(new Rect(left.xMax + 8f, inRect.y + 36f, DollPreview.PanelWidth, inRect.height - 36f));
            }
            Settings.DoSettingsWindowContents(left);
            DollPreview.DrawToggle(new Rect(inRect.xMax - 140f, inRect.y, 140f, 28f));
            base.DoSettingsWindowContents(inRect);
        }

        public override void WriteSettings()
        {
            DollPreview.OnWindowClosed();
            base.WriteSettings();
        }
    }
}
