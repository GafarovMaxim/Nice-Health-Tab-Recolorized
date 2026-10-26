using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace NiceHealthTab_YAPEM_Patch
{
    public static class PawnSettingsPreviewDrawer
    {
        public enum PreviewPreset
        {
            Showcase,
            FullBionic,
            FullArchotech,
            DLCImplants
        }

        public static PreviewPreset CurrentPreset = PreviewPreset.Showcase;

        // Cached texture references
        private static Texture2D texTorso;
        private static Texture2D texHead;
        private static Texture2D texUpperArm;
        private static Texture2D texLowerArm;
        private static Texture2D texLeg;
        private static Texture2D texHand;
        private static Texture2D texFeet;
        private static Texture2D texOutline;

        private static Texture2D iconBrain;
        private static Texture2D iconHeart;
        private static Texture2D iconLung;
        private static Texture2D iconStomach;
        private static Texture2D iconKidney;
        private static Texture2D iconGland;

        private static bool texturesLoaded;

        private static void EnsureTextures()
        {
            if (texturesLoaded) return;
            texturesLoaded = true;

            texTorso    = ContentFinder<Texture2D>.Get("HediffTab/Baseliner/Male/Torso", false);
            texHead     = ContentFinder<Texture2D>.Get("HediffTab/Baseliner/Male/Head", false);
            texUpperArm = ContentFinder<Texture2D>.Get("HediffTab/Baseliner/Male/UpperArm", false);
            texLowerArm = ContentFinder<Texture2D>.Get("HediffTab/Baseliner/Male/LowerArm", false);
            texLeg      = ContentFinder<Texture2D>.Get("HediffTab/Baseliner/Male/Leg", false);
            texHand     = ContentFinder<Texture2D>.Get("HediffTab/Baseliner/Male/Hand", false);
            texFeet     = ContentFinder<Texture2D>.Get("HediffTab/Baseliner/Male/Feet", false);
            texOutline  = ContentFinder<Texture2D>.Get("HediffTab/Baseliner/Male/Outline", false);

            iconBrain   = ContentFinder<Texture2D>.Get("HediffTab/Icons/Brain", false);
            iconHeart   = ContentFinder<Texture2D>.Get("HediffTab/Icons/Blood", false);
            iconLung    = ContentFinder<Texture2D>.Get("HediffTab/Icons/Lung", false);
            iconStomach = ContentFinder<Texture2D>.Get("HediffTab/Icons/Stomach", false);
            iconKidney  = ContentFinder<Texture2D>.Get("HediffTab/Icons/BloodFilter", false);
            iconGland   = ContentFinder<Texture2D>.Get("HediffTab/Icons/Apple", false);
        }

        public static void DrawPreview(Rect rect, YAPEMPatchSettings settings)
        {
            EnsureTextures();

            // Background card
            Widgets.DrawBoxSolid(rect, new Color(0.08f, 0.09f, 0.11f, 0.95f));
            Widgets.DrawBox(rect, 1);

            // Title & Subtitle
            Rect headerRect = new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, 26f);
            Text.Font = GameFont.Medium;
            Widgets.Label(headerRect, "NHT_YAPEM_PreviewTitle".Translate());
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.7f, 0.75f, 0.8f);
            Widgets.Label(new Rect(rect.x + 10f, headerRect.yMax - 2f, rect.width - 20f, 18f), "NHT_YAPEM_PreviewSubtitle".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            // Preset Buttons
            Rect presetBar = new Rect(rect.x + 8f, headerRect.yMax + 18f, rect.width - 16f, 24f);
            float btnW = (presetBar.width - 9f) / 4f;

            DrawPresetButton(new Rect(presetBar.x, presetBar.y, btnW, 24f), "Showcase", PreviewPreset.Showcase);
            DrawPresetButton(new Rect(presetBar.x + btnW + 3f, presetBar.y, btnW, 24f), "Bionics", PreviewPreset.FullBionic);
            DrawPresetButton(new Rect(presetBar.x + (btnW + 3f) * 2f, presetBar.y, btnW, 24f), "Archotech", PreviewPreset.FullArchotech);
            DrawPresetButton(new Rect(presetBar.x + (btnW + 3f) * 3f, presetBar.y, btnW, 24f), "DLC", PreviewPreset.DLCImplants);

            // Main Doll Canvas Area
            Rect dollCanvas = new Rect(rect.x + (rect.width - 220f) / 2f, presetBar.yMax + 12f, 220f, 280f);
            Widgets.DrawBoxSolid(dollCanvas, new Color(0.04f, 0.05f, 0.06f, 0.8f));
            Widgets.DrawBox(dollCanvas, 1);

            DrawDoll(dollCanvas, settings);

            // Status message at bottom of preview box
            Rect statusRect = new Rect(rect.x + 10f, dollCanvas.yMax + 10f, rect.width - 20f, 40f);
            DrawDominantStatus(statusRect, settings);
        }

        private static void DrawPresetButton(Rect r, string label, PreviewPreset preset)
        {
            bool active = CurrentPreset == preset;
            if (active)
            {
                Widgets.DrawBoxSolid(r, new Color(0.2f, 0.45f, 0.7f, 0.9f));
                Widgets.DrawBox(r, 1);
            }
            if (Widgets.ButtonText(r, label, drawBackground: !active))
            {
                CurrentPreset = preset;
            }
        }

        private static void DrawDoll(Rect canvas, YAPEMPatchSettings settings)
        {
            // Determine active limbs and implants based on preset:
            Color colAdv = settings.ColorAdvancedBionic;
            Color colUltra = settings.ColorUltratech;
            Color colArcho = settings.ColorArchotech;
            Color colBio = settings.ColorBionic;
            Color colPro = settings.ColorProsthetic;

            Color rightArmCol, leftArmCol, rightLegCol, leftLegCol;
            string rightArmName, leftArmName, rightLegName, leftLegName;

            List<Color> limbColors = new List<Color>();

            switch (CurrentPreset)
            {
                case PreviewPreset.FullArchotech:
                    rightArmCol = leftArmCol = rightLegCol = leftLegCol = colArcho;
                    rightArmName = leftArmName = "Archotech Arm";
                    rightLegName = leftLegName = "Archotech Leg";
                    limbColors.AddRange(new[] { colArcho, colArcho, colArcho, colArcho });
                    break;

                case PreviewPreset.FullBionic:
                    rightArmCol = leftArmCol = rightLegCol = leftLegCol = colBio;
                    rightArmName = leftArmName = "Bionic Arm";
                    rightLegName = leftLegName = "Bionic Leg";
                    limbColors.AddRange(new[] { colBio, colBio, colBio, colBio });
                    break;

                case PreviewPreset.DLCImplants:
                    Color colRoyaltyLimb = settings.UseDLCColors ? settings.ColorRoyalty : colBio;
                    Color colOdysseyLimb = settings.UseDLCColors ? settings.ColorOdyssey : colUltra;
                    rightArmCol = colRoyaltyLimb; rightArmName = "Drill Arm (Royalty)";
                    leftArmCol = colRoyaltyLimb;  leftArmName = "Field Hand (Royalty)";
                    rightLegCol = colBio;         rightLegName = "Bionic Leg";
                    leftLegCol = colOdysseyLimb;  leftLegName = "Vacskin Limb (Odyssey)";
                    limbColors.AddRange(new[] { colRoyaltyLimb, colRoyaltyLimb, colBio, colOdysseyLimb });
                    break;

                case PreviewPreset.Showcase:
                default:
                    rightArmCol = colArcho; rightArmName = "Archotech Arm";
                    leftArmCol = colAdv;   leftArmName = "Advanced Bionic Arm (YAPEM)";
                    rightLegCol = colBio;  rightLegName = "Bionic Leg";
                    leftLegCol = colUltra; leftLegName = "Ultratech Leg";
                    limbColors.AddRange(new[] { colArcho, colAdv, colBio, colUltra });
                    break;
            }

            // Check dominant tinting:
            Color bodySubstrateCol = new Color(0.24f, 0.27f, 0.31f); // default neutral doll color
            if (settings.EnableBodyTinting)
            {
                // Count dominant limbs
                Dictionary<Color, int> counts = new Dictionary<Color, int>();
                foreach (var c in limbColors)
                {
                    counts[c] = counts.TryGetValue(c, out int val) ? val + 1 : 1;
                }
                foreach (var kvp in counts)
                {
                    if (kvp.Value >= settings.RequiredLimbCount)
                    {
                        bodySubstrateCol = Color.Lerp(kvp.Key, Color.white, 0.15f);
                        break;
                    }
                }
            }

            Vector2 center = canvas.center;

            // 1. Base Silhouette / Body Substrate
            DrawPartSprite(texHead, new Rect(center.x - 18f, canvas.y + 16f, 36f, 42f), bodySubstrateCol);
            DrawPartSprite(texTorso, new Rect(center.x - 26f, canvas.y + 54f, 52f, 85f), bodySubstrateCol);

            // 2. Limbs
            // Right Arm
            Rect rArmRect = new Rect(center.x - 52f, canvas.y + 58f, 26f, 75f);
            DrawPartSprite(texUpperArm, rArmRect, rightArmCol);
            TooltipHandler.TipRegion(rArmRect, $"{rightArmName}\n#{ColorUtility.ToHtmlStringRGB(rightArmCol)}");

            // Left Arm
            Rect lArmRect = new Rect(center.x + 26f, canvas.y + 58f, 26f, 75f);
            DrawPartSpriteFlipped(texUpperArm, lArmRect, leftArmCol);
            TooltipHandler.TipRegion(lArmRect, $"{leftArmName}\n#{ColorUtility.ToHtmlStringRGB(leftArmCol)}");

            // Right Leg
            Rect rLegRect = new Rect(center.x - 26f, canvas.y + 135f, 24f, 88f);
            DrawPartSprite(texLeg, rLegRect, rightLegCol);
            TooltipHandler.TipRegion(rLegRect, $"{rightLegName}\n#{ColorUtility.ToHtmlStringRGB(rightLegCol)}");

            // Left Leg
            Rect lLegRect = new Rect(center.x + 2f, canvas.y + 135f, 24f, 88f);
            DrawPartSpriteFlipped(texLeg, lLegRect, leftLegCol);
            TooltipHandler.TipRegion(lLegRect, $"{leftLegName}\n#{ColorUtility.ToHtmlStringRGB(leftLegCol)}");

            // 3. Hands & Feet Icons at bottom
            float iconY = canvas.yMax - 40f;
            Color rHandCol = settings.ColorBottomIcons ? rightArmCol : Color.white;
            Color lHandCol = settings.ColorBottomIcons ? leftArmCol : Color.white;
            Color rFootCol = settings.ColorBottomIcons ? rightLegCol : Color.white;
            Color lFootCol = settings.ColorBottomIcons ? leftLegCol : Color.white;

            Rect rHandBox = new Rect(center.x - 88f, iconY, 28f, 28f);
            Rect rFootBox = new Rect(center.x - 44f, iconY, 28f, 28f);
            Rect lFootBox = new Rect(center.x + 16f, iconY, 28f, 28f);
            Rect lHandBox = new Rect(center.x + 60f, iconY, 28f, 28f);

            DrawIndicatorIcon(texHand, rHandBox, rHandCol, "Right Hand Indicator");
            DrawIndicatorIcon(texFeet, rFootBox, rFootCol, "Right Foot Indicator");
            DrawIndicatorIcon(texFeet, lFootBox, lFootCol, "Left Foot Indicator");
            DrawIndicatorIcon(texHand, lHandBox, lHandCol, "Left Hand Indicator");

            // 4. Internal Implants & Organs Overlays (Torso & Head)
            DrawInternalImplants(canvas, center, settings);
        }

        private static void DrawInternalImplants(Rect canvas, Vector2 center, YAPEMPatchSettings settings)
        {
            Color colBio = settings.ColorBionic;
            Color colArcho = settings.ColorArchotech;
            Color colUltra = settings.ColorUltratech;
            Color colAdv = settings.ColorAdvancedBionic;

            // Colors for DLC categories:
            Color colRoyalty = settings.UseDLCColors ? settings.ColorRoyalty : colBio;
            Color colBiotech = settings.UseDLCColors ? settings.ColorBiotech : colBio;
            Color colAnomaly = settings.UseDLCColors ? settings.ColorAnomaly : colBio;
            Color colOdyssey = settings.UseDLCColors ? settings.ColorOdyssey : colBio;

            // Brain / Head Implant
            string brainName;
            Color brainCol;
            switch (CurrentPreset)
            {
                case PreviewPreset.FullArchotech:
                    brainName = "Archotech Eye / Psylink";
                    brainCol = colArcho;
                    break;
                case PreviewPreset.DLCImplants:
                    brainName = "Pilot Assistant (Odyssey)";
                    brainCol = colOdyssey;
                    break;
                default:
                    brainName = "Neurocalculator (Royalty) / Brain Implant";
                    brainCol = colRoyalty;
                    break;
            }
            Rect brainRect = new Rect(center.x - 9f, canvas.y + 26f, 18f, 18f);
            DrawOrganIcon(iconBrain, brainRect, brainCol, brainName);

            // Heart (Chest Left)
            string heartName;
            Color heartCol;
            switch (CurrentPreset)
            {
                case PreviewPreset.FullArchotech:
                    heartName = "Archotech Heart"; heartCol = colArcho; break;
                case PreviewPreset.DLCImplants:
                    heartName = "Adrenal Heart (Anomaly)"; heartCol = colAnomaly; break;
                default:
                    heartName = "Adv. Bionic Heart (YAPEM)"; heartCol = colAdv; break;
            }
            Rect heartRect = new Rect(center.x - 18f, canvas.y + 68f, 16f, 16f);
            DrawOrganIcon(iconHeart, heartRect, heartCol, heartName);

            // Lung (Chest Right)
            string lungName = (CurrentPreset == PreviewPreset.DLCImplants) ? "Detoxifier Lung (Biotech)" : "Bionic Lung";
            Color lungCol = (CurrentPreset == PreviewPreset.DLCImplants) ? colBiotech : colBio;
            Rect lungRect = new Rect(center.x + 2f, canvas.y + 68f, 16f, 16f);
            DrawOrganIcon(iconLung, lungRect, lungCol, lungName);

            // Stomach (Abdomen Center)
            string stomachName;
            Color stomachCol;
            switch (CurrentPreset)
            {
                case PreviewPreset.FullArchotech:
                    stomachName = "Archotech Stomach"; stomachCol = colArcho; break;
                case PreviewPreset.DLCImplants:
                    stomachName = "Nuclear Stomach (Royalty)"; stomachCol = colRoyalty; break;
                default:
                    stomachName = "Ultratech Stomach"; stomachCol = colUltra; break;
            }
            Rect stomachRect = new Rect(center.x - 8f, canvas.y + 92f, 16f, 16f);
            DrawOrganIcon(iconStomach, stomachRect, stomachCol, stomachName);

            // Glands / Dermal Armor (Upper Torso Center)
            string glandName;
            Color glandCol;
            switch (CurrentPreset)
            {
                case PreviewPreset.FullArchotech:
                    glandName = "Archotech Shield Core"; glandCol = colArcho; break;
                case PreviewPreset.DLCImplants:
                    glandName = "Stoneskin Gland (Royalty) / Vacskin (Odyssey)"; glandCol = colRoyalty; break;
                default:
                    glandName = "Stoneskin Gland (Royalty)"; glandCol = colRoyalty; break;
            }
            Rect glandRect = new Rect(center.x - 7f, canvas.y + 56f, 14f, 14f);
            DrawOrganIcon(iconGland, glandRect, glandCol, glandName);

            // Kidney / Blood Filter (Lower Left Torso)
            string kidneyName = (CurrentPreset == PreviewPreset.DLCImplants) ? "Blood Warmer (Odyssey/Anomaly)" : "Detoxifier Kidney (Biotech)";
            Color kidneyCol = (CurrentPreset == PreviewPreset.DLCImplants) ? colAnomaly : colBiotech;
            Rect kidneyRect = new Rect(center.x - 17f, canvas.y + 110f, 14f, 14f);
            DrawOrganIcon(iconKidney, kidneyRect, kidneyCol, kidneyName);
        }

        private static void DrawPartSprite(Texture2D tex, Rect r, Color col)
        {
            Color prev = GUI.color;
            GUI.color = col;
            if (tex != null)
            {
                GUI.DrawTexture(r, tex);
            }
            else
            {
                Widgets.DrawBoxSolid(r, col);
            }
            GUI.color = prev;
        }

        private static void DrawPartSpriteFlipped(Texture2D tex, Rect r, Color col)
        {
            Color prev = GUI.color;
            GUI.color = col;
            if (tex != null)
            {
                Rect uv = new Rect(1f, 0f, -1f, 1f);
                GUI.DrawTextureWithTexCoords(r, tex, uv);
            }
            else
            {
                Widgets.DrawBoxSolid(r, col);
            }
            GUI.color = prev;
        }

        private static void DrawIndicatorIcon(Texture2D tex, Rect r, Color col, string tip)
        {
            Widgets.DrawBoxSolid(r, new Color(0.12f, 0.14f, 0.17f, 0.9f));
            Widgets.DrawBox(r, 1);
            DrawPartSprite(tex, GenUI.ContractedBy(r, 3f), col);
            TooltipHandler.TipRegion(r, $"{tip}\n#{ColorUtility.ToHtmlStringRGB(col)}");
        }

        private static void DrawOrganIcon(Texture2D tex, Rect r, Color col, string tip)
        {
            // Glow backdrop
            Widgets.DrawBoxSolid(GenUI.ExpandedBy(r, 2f), new Color(col.r, col.g, col.b, 0.25f));
            Color prev = GUI.color;
            GUI.color = col;
            if (tex != null)
            {
                GUI.DrawTexture(r, tex);
            }
            else
            {
                Widgets.DrawBoxSolid(r, col);
            }
            GUI.color = prev;
            TooltipHandler.TipRegion(r, $"{tip}\n#{ColorUtility.ToHtmlStringRGB(col)}");
        }

        private static void DrawDominantStatus(Rect r, YAPEMPatchSettings settings)
        {
            Text.Font = GameFont.Tiny;
            string status;
            Color statusCol;

            if (settings.EnableBodyTinting)
            {
                if (CurrentPreset == PreviewPreset.FullArchotech || CurrentPreset == PreviewPreset.FullBionic)
                {
                    status = "NHT_YAPEM_DominantActive".Translate(4);
                    statusCol = new Color(0.4f, 0.95f, 0.5f);
                }
                else if (CurrentPreset == PreviewPreset.DLCImplants && settings.RequiredLimbCount <= 3)
                {
                    status = "NHT_YAPEM_DominantActive".Translate(3);
                    statusCol = new Color(0.4f, 0.95f, 0.5f);
                }
                else
                {
                    status = "NHT_YAPEM_DominantInactive".Translate();
                    statusCol = new Color(0.7f, 0.7f, 0.7f);
                }
            }
            else
            {
                status = "NHT_YAPEM_DominantInactive".Translate();
                statusCol = new Color(0.6f, 0.6f, 0.6f);
            }

            GUI.color = statusCol;
            Widgets.Label(r, status);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }
    }
}
