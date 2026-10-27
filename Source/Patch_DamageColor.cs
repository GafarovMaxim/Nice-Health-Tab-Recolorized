using UnityEngine;
using HarmonyLib;
using Verse;
using NiceHealthTab;

namespace NiceHealthTab_YAPEM_Patch
{
    // Optional single color for wounded parts (natural and implants). Saturation grows with the lost health.
    [HarmonyPatch(typeof(DollCondition), nameof(DollCondition.GetPartColor))]
    public static class Patch_DamageColor
    {
        private const float MinSaturation = 0.3f;

        public static void Postfix(ref Color __result, DollPartStatus status, float partHealth, float maxHealth)
        {
            if (status != DollPartStatus.Painful && status != DollPartStatus.Critical) return;
            YAPEMPatchSettings settings = YAPEMPatchMod.Settings;
            if (settings == null || !settings.UseDamageColor || maxHealth <= 0f) return;

            float damage = Mathf.Clamp01(1f - partHealth / maxHealth);
            Color.RGBToHSV(settings.ColorDamage, out float h, out float s, out float v);
            __result = Color.HSVToRGB(h, s * Mathf.Lerp(MinSaturation, 1f, damage), v);
        }
    }
}
