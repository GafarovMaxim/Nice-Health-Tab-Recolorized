using UnityEngine;
using HarmonyLib;
using Verse;
using NiceHealthTab;

namespace NiceHealthTab_YAPEM_Patch
{
    // Optional single hue for wounded parts (natural and implants). Like NHT: two levels, light (painful) and serious (critical).
    // The picked color is the serious one; the light one is derived with the same saturation ratio NHT uses (0.53 / 0.82).
    [HarmonyPatch(typeof(DollCondition), nameof(DollCondition.GetPartColor))]
    public static class Patch_DamageColor
    {
        private const float LightSaturationRatio = 0.646f;

        public static void Postfix(ref Color __result, DollPartStatus status)
        {
            if (status != DollPartStatus.Painful && status != DollPartStatus.Critical) return;
            YAPEMPatchSettings settings = YAPEMPatchMod.Settings;
            if (settings == null || !settings.UseDamageColor) return;

            // The untouched default is NHT's own pair (#F82D45 / #FF7878), so leave NHT's result alone.
            if (settings.ColorDamage == YAPEMPatchSettings.DefaultColorDamage) return;

            if (status == DollPartStatus.Critical)
            {
                __result = settings.ColorDamage;
                return;
            }
            Color.RGBToHSV(settings.ColorDamage, out float h, out float s, out float v);
            __result = Color.HSVToRGB(h, s * LightSaturationRatio, Mathf.Min(1f, v * 1.027f));
        }
    }
}
