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
            Settings.DoSettingsWindowContents(inRect);
            base.DoSettingsWindowContents(inRect);
        }
    }
}
