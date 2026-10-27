using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using NiceHealthTab;

namespace NiceHealthTab_YAPEM_Patch
{
    // Palette icon in the bottom-right corner of the health tab doll (mirror of NHT's own "vanilla list" button on the left).
    // Click opens this mod's settings window.
    [HarmonyPatch(typeof(DollWidget), nameof(DollWidget.Draw))]
    public static class Patch_PaletteButton
    {
        // Same greys NHT uses for its inactive icons (Assets.IconInactiveColor / IconInactiveHoverColor, internal there).
        private static readonly Color IdleColor = new Color(0.45f, 0.45f, 0.45f, 0.25f);
        private static readonly Color HoverColor = new Color(0.45f, 0.45f, 0.45f, 1f);
        private static Texture2D icon;

        public static void Postfix(DollWidget __instance)
        {
            if (!__instance.Visible || __instance.Pawn == null) return;
            Rect area = __instance.InnerRect;
            if (area.width <= 0f || area.height <= 0f) return;

            icon = icon ?? ContentFinder<Texture2D>.Get("Recolorized/PaletteIcon", false);
            if (icon == null) return;

            Rect rect = new Rect(area.xMax - 6f - 20f, area.yMax - 6f - 20f, 20f, 20f);
            bool over = Mouse.IsOver(rect);
            GUI.color = over ? HoverColor : IdleColor;
            GUI.DrawTexture(rect, icon);
            GUI.color = Color.white;
            TooltipHandler.TipRegion(rect, "NHT_YAPEM_OpenSettings".Translate());
            if (Widgets.ButtonInvisible(rect, true))
            {
                Find.WindowStack.Add(new Dialog_ModSettings(LoadedModManager.GetMod<YAPEMPatchMod>()));
            }
        }
    }
}
