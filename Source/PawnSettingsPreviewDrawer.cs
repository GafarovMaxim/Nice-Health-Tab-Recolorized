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
        private static Texture2D texNeck;
        private static Texture2D texShoulder;
        private static Texture2D texArm;
        private static Texture2D texLeg;

        // Prosthetic overlays
        private static Texture2D texAdvArm;
        private static Texture2D texAdvLeg;
        private static Texture2D texArchoArm;
        private static Texture2D texArchoLeg;
        private static Texture2D texUltraArm;
        private static Texture2D texUltraLeg;

        // Organs
        private static Texture2D texBrain;
        private static Texture2D texHeart;
        private static Texture2D texLung;
        private static Texture2D texStomach;
        private static Texture2D texKidney;

        // Bottom icons
        private static Texture2D texIconHand;
        private static Texture2D texIconFoot;

        private static bool texturesLoaded;

        private static void EnsureTextures()
        {
            if (texturesLoaded) return;
            texturesLoaded = true;

            // Baseliner Female/Male base body parts
            texTorso    = ContentFinder<Texture2D>.Get("HediffTab/Baseliner/Female/Torso", false) 
                       ?? ContentFinder<Texture2D>.Get("HediffTab/Baseliner/Male/Torso", false);
            texHead     = ContentFinder<Texture2D>.Get("HediffTab/Baseliner/Female/NiceHead", false) 
                       ?? ContentFinder<Texture2D>.Get("HediffTab/Baseliner/Female/Head", false)
                       ?? ContentFinder<Texture2D>.Get("HediffTab/Baseliner/Male/Head", false);
            texNeck     = ContentFinder<Texture2D>.Get("HediffTab/Baseliner/Female/Neck", false);
            texShoulder = ContentFinder<Texture2D>.Get("HediffTab/Baseliner/Female/Shoulder", false);
            texArm      = ContentFinder<Texture2D>.Get("HediffTab/Baseliner/Female/Arm", false)
                       ?? ContentFinder<Texture2D>.Get("HediffTab/Baseliner/Male/Arm", false);
            texLeg      = ContentFinder<Texture2D>.Get("HediffTab/Baseliner/Female/Leg", false)
                       ?? ContentFinder<Texture2D>.Get("HediffTab/Baseliner/Male/Leg", false);

            // Prosthesis sprites
            texAdvArm   = ContentFinder<Texture2D>.Get("HediffTab/Prosthesis/Female/Arms/AdvBionicArm", false)
                       ?? ContentFinder<Texture2D>.Get("HediffTab/Prosthesis/Male/Arms/AdvBionicArm", false);
            texAdvLeg   = ContentFinder<Texture2D>.Get("HediffTab/Prosthesis/Female/Legs/AdvBionicLeg", false)
                       ?? ContentFinder<Texture2D>.Get("HediffTab/Prosthesis/Male/Legs/AdvBionicLeg", false);

            texArchoArm = ContentFinder<Texture2D>.Get("HediffTab/Prosthesis/Female/Arms/ArchotechArm", false)
                       ?? ContentFinder<Texture2D>.Get("HediffTab/Prosthesis/Male/Arms/ArchotechArm", false);
            texArchoLeg = ContentFinder<Texture2D>.Get("HediffTab/Prosthesis/Female/Legs/ArchotechLeg", false)
                       ?? ContentFinder<Texture2D>.Get("HediffTab/Prosthesis/Male/Legs/ArchotechLeg", false);

            texUltraArm = ContentFinder<Texture2D>.Get("HediffTab/Prosthesis/Female/Arms/UltratechArm", false)
                       ?? ContentFinder<Texture2D>.Get("HediffTab/Prosthesis/Male/Arms/UltratechArm", false);
            texUltraLeg = ContentFinder<Texture2D>.Get("HediffTab/Prosthesis/Female/Legs/UltratechLeg", false)
                       ?? ContentFinder<Texture2D>.Get("HediffTab/Prosthesis/Male/Legs/UltratechLeg", false);

            // Organs
            texBrain    = ContentFinder<Texture2D>.Get("HediffTab/Organs/Brain", false)
                       ?? ContentFinder<Texture2D>.Get("HediffTab/Icons/Brain", false);
            texHeart    = ContentFinder<Texture2D>.Get("HediffTab/Organs/Heart", false)
                       ?? ContentFinder<Texture2D>.Get("HediffTab/Icons/Blood", false);
            texLung     = ContentFinder<Texture2D>.Get("HediffTab/Organs/Lung", false)
                       ?? ContentFinder<Texture2D>.Get("HediffTab/Icons/Lung", false);
            texStomach  = ContentFinder<Texture2D>.Get("HediffTab/Organs/Stomach", false)
                       ?? ContentFinder<Texture2D>.Get("HediffTab/Icons/Stomach", false);
            texKidney   = ContentFinder<Texture2D>.Get("HediffTab/Organs/Kidney", false)
                       ?? ContentFinder<Texture2D>.Get("HediffTab/Icons/BloodFilter", false);

            // Bottom indicator icons
            texIconHand = ContentFinder<Texture2D>.Get("HediffTab/Baseliner/Female/Hand", false)
                       ?? ContentFinder<Texture2D>.Get("HediffTab/Baseliner/Hand/HandBase", false);
            texIconFoot = ContentFinder<Texture2D>.Get("HediffTab/Baseliner/Feet/Feet", false)
                       ?? ContentFinder<Texture2D>.Get("HediffTab/Baseliner/Female/Feet", false);
        }

        public static void DrawPreview(Rect rect, YAPEMPatchSettings settings)
        {
            EnsureTextures();

            // Nice Health Tab styled card container
            Widgets.DrawBoxSolid(rect, new Color(0.10f, 0.11f, 0.13f, 0.96f));
            Widgets.DrawBox(rect, 1);

            // Header: Title & Subtitle
            Rect headerRect = new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, 26f);
            Text.Font = GameFont.Medium;
            Widgets.Label(headerRect, "NHT_YAPEM_PreviewTitle".Translate());
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.72f, 0.77f, 0.82f);
            Widgets.Label(new Rect(rect.x + 10f, headerRect.yMax - 2f, rect.width - 20f, 18f), "NHT_YAPEM_PreviewSubtitle".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            // Preset Tabs
            Rect presetBar = new Rect(rect.x + 8f, headerRect.yMax + 16f, rect.width - 16f, 24f);
            float btnW = (presetBar.width - 9f) / 4f;

            DrawPresetButton(new Rect(presetBar.x, presetBar.y, btnW, 24f), "Showcase", PreviewPreset.Showcase);
            DrawPresetButton(new Rect(presetBar.x + btnW + 3f, presetBar.y, btnW, 24f), "Bionics", PreviewPreset.FullBionic);
            DrawPresetButton(new Rect(presetBar.x + (btnW + 3f) * 2f, presetBar.y, btnW, 24f), "Archotech", PreviewPreset.FullArchotech);
            DrawPresetButton(new Rect(presetBar.x + (btnW + 3f) * 3f, presetBar.y, btnW, 24f), "DLC", PreviewPreset.DLCImplants);

            // Main Doll Canvas Area
            float canvasH = Mathf.Min(310f, rect.height - 120f);
            Rect dollCanvas = new Rect(rect.x + (rect.width - 240f) / 2f, presetBar.yMax + 10f, 240f, canvasH);
            Widgets.DrawBoxSolid(dollCanvas, new Color(0.06f, 0.07f, 0.08f, 0.88f));
            Widgets.DrawBox(dollCanvas, 1);

            DrawDoll(dollCanvas, settings);

            // Status indicator message at bottom
            Rect statusRect = new Rect(rect.x + 10f, dollCanvas.yMax + 8f, rect.width - 20f, 32f);
            DrawDominantStatus(statusRect, settings);
        }

        private static void DrawPresetButton(Rect r, string label, PreviewPreset preset)
        {
            bool active = CurrentPreset == preset;
            if (active)
            {
                Widgets.DrawBoxSolid(r, new Color(0.24f, 0.44f, 0.68f, 0.95f));
                Widgets.DrawBox(r, 1);
            }
            if (Widgets.ButtonText(r, label, drawBackground: !active))
            {
                CurrentPreset = preset;
            }
        }

        private static void DrawDoll(Rect canvas, YAPEMPatchSettings settings)
        {
            // Tier Colors
            Color colAdv = settings.ColorAdvancedBionic;
            Color colUltra = settings.ColorUltratech;
            Color colArcho = settings.ColorArchotech;
            Color colBio = settings.ColorBionic;
            Color colPro = settings.ColorProsthetic;

            Color rightArmCol, leftArmCol, rightLegCol, leftLegCol;
            string rightArmName, leftArmName, rightLegName, leftLegName;
            Texture2D rightArmTex, leftArmTex, rightLegTex, leftLegTex;

            List<Color> limbColors = new List<Color>();

            switch (CurrentPreset)
            {
                case PreviewPreset.FullArchotech:
                    rightArmCol = leftArmCol = rightLegCol = leftLegCol = colArcho;
                    rightArmName = leftArmName = "Archotech Arm";
                    rightLegName = leftLegName = "Archotech Leg";
                    rightArmTex = leftArmTex = texArchoArm ?? texArm;
                    rightLegTex = leftLegTex = texArchoLeg ?? texLeg;
                    limbColors.AddRange(new[] { colArcho, colArcho, colArcho, colArcho });
                    break;

                case PreviewPreset.FullBionic:
                    rightArmCol = leftArmCol = rightLegCol = leftLegCol = colBio;
                    rightArmName = leftArmName = "Bionic Arm";
                    rightLegName = leftLegName = "Bionic Leg";
                    rightArmTex = leftArmTex = texArm;
                    rightLegTex = leftLegTex = texLeg;
                    limbColors.AddRange(new[] { colBio, colBio, colBio, colBio });
                    break;

                case PreviewPreset.DLCImplants:
                    Color colRoyaltyLimb = settings.UseDLCColors ? settings.ColorRoyalty : colBio;
                    Color colOdysseyLimb = settings.UseDLCColors ? settings.ColorOdyssey : colUltra;
                    rightArmCol = colRoyaltyLimb; rightArmName = "Drill Arm (Royalty)";
                    leftArmCol = colRoyaltyLimb;  leftArmName = "Field Hand (Royalty)";
                    rightLegCol = colBio;         rightLegName = "Bionic Leg";
                    leftLegCol = colOdysseyLimb;  leftLegName = "Vacskin Limb (Odyssey)";
                    rightArmTex = leftArmTex = texArm;
                    rightLegTex = leftLegTex = texLeg;
                    limbColors.AddRange(new[] { colRoyaltyLimb, colRoyaltyLimb, colBio, colOdysseyLimb });
                    break;

                case PreviewPreset.Showcase:
                default:
                    rightArmCol = colArcho; rightArmName = "Archotech Arm";
                    leftArmCol = colAdv;   leftArmName = "Advanced Bionic Arm (YAPEM)";
                    rightLegCol = colBio;  rightLegName = "Bionic Leg";
                    leftLegCol = colUltra; leftLegName = "Ultratech Leg";
                    rightArmTex = texArchoArm ?? texArm;
                    leftArmTex = texAdvArm ?? texArm;
                    rightLegTex = texLeg;
                    leftLegTex = texUltraLeg ?? texLeg;
                    limbColors.AddRange(new[] { colArcho, colAdv, colBio, colUltra });
                    break;
            }

            // Check dominant tinting:
            Color bodySubstrateCol = new Color(0.92f, 0.94f, 0.96f); // Default clean mannequin white
            if (settings.EnableBodyTinting)
            {
                Dictionary<Color, int> counts = new Dictionary<Color, int>();
                foreach (var c in limbColors)
                {
                    counts[c] = counts.TryGetValue(c, out int val) ? val + 1 : 1;
                }
                foreach (var kvp in counts)
                {
                    if (kvp.Value >= settings.RequiredLimbCount)
                    {
                        bodySubstrateCol = Color.Lerp(kvp.Key, Color.white, 0.25f);
                        break;
                    }
                }
            }

            Vector2 center = canvas.center;
            float topY = canvas.y + 10f;

            // 1. Base Silhouette: Head, Neck, Torso (Nice Health Tab authentic proportions)
            // Head
            Rect headRect = new Rect(center.x - 22f, topY, 44f, 44f);
            DrawPartSprite(texHead, headRect, bodySubstrateCol);

            // Neck
            Rect neckRect = new Rect(center.x - 10f, topY + 38f, 20f, 18f);
            DrawPartSprite(texNeck, neckRect, bodySubstrateCol);

            // Torso & Shoulders
            Rect torsoRect = new Rect(center.x - 30f, topY + 48f, 60f, 85f);
            DrawPartSprite(texTorso, torsoRect, bodySubstrateCol);

            // Shoulders
            Rect rShoulderRect = new Rect(center.x - 48f, topY + 48f, 24f, 32f);
            DrawPartSprite(texShoulder, rShoulderRect, bodySubstrateCol);

            Rect lShoulderRect = new Rect(center.x + 24f, topY + 48f, 24f, 32f);
            DrawPartSpriteFlipped(texShoulder, lShoulderRect, bodySubstrateCol);

            // 2. Limbs (Left & Right Arms, Left & Right Legs)
            // Right Arm
            Rect rArmRect = new Rect(center.x - 56f, topY + 54f, 28f, 76f);
            DrawPartSprite(rightArmTex, rArmRect, rightArmCol);
            TooltipHandler.TipRegion(rArmRect, $"{rightArmName}\n#{ColorUtility.ToHtmlStringRGB(rightArmCol)}");

            // Left Arm
            Rect lArmRect = new Rect(center.x + 28f, topY + 54f, 28f, 76f);
            DrawPartSpriteFlipped(leftArmTex, lArmRect, leftArmCol);
            TooltipHandler.TipRegion(lArmRect, $"{leftArmName}\n#{ColorUtility.ToHtmlStringRGB(leftArmCol)}");

            // Right Leg
            Rect rLegRect = new Rect(center.x - 30f, topY + 128f, 28f, 95f);
            DrawPartSprite(rightLegTex, rLegRect, rightLegCol);
            TooltipHandler.TipRegion(rLegRect, $"{rightLegName}\n#{ColorUtility.ToHtmlStringRGB(rightLegCol)}");

            // Left Leg
            Rect lLegRect = new Rect(center.x + 2f, topY + 128f, 28f, 95f);
            DrawPartSpriteFlipped(leftLegTex, lLegRect, leftLegCol);
            TooltipHandler.TipRegion(lLegRect, $"{leftLegName}\n#{ColorUtility.ToHtmlStringRGB(leftLegCol)}");

            // 3. Organs inside Torso and Head
            DrawInternalImplants(canvas, center, topY, settings);

            // 4. Nice Health Tab bottom 4 indicator icons: [Hand L] [Hand R] [Foot L] [Foot R]
            float iconY = canvas.yMax - 36f;
            Color rHandCol = settings.ColorBottomIcons ? rightArmCol : Color.white;
            Color lHandCol = settings.ColorBottomIcons ? leftArmCol : Color.white;
            Color rFootCol = settings.ColorBottomIcons ? rightLegCol : Color.white;
            Color lFootCol = settings.ColorBottomIcons ? leftLegCol : Color.white;

            float slotW = 28f;
            float gap = 16f;
            float totalIconsW = slotW * 4f + gap * 3f;
            float startX = center.x - totalIconsW / 2f;

            Rect rHandBox = new Rect(startX, iconY, slotW, 28f);
            Rect lHandBox = new Rect(rHandBox.xMax + gap, iconY, slotW, 28f);
            Rect rFootBox = new Rect(lHandBox.xMax + gap, iconY, slotW, 28f);
            Rect lFootBox = new Rect(rFootBox.xMax + gap, iconY, slotW, 28f);

            DrawIndicatorIcon(texIconHand, rHandBox, rHandCol, false, "Right Hand Indicator");
            DrawIndicatorIcon(texIconHand, lHandBox, lHandCol, true, "Left Hand Indicator");
            DrawIndicatorIcon(texIconFoot, rFootBox, rFootCol, false, "Right Foot Indicator");
            DrawIndicatorIcon(texIconFoot, lFootBox, lFootCol, true, "Left Foot Indicator");
        }

        private static void DrawInternalImplants(Rect canvas, Vector2 center, float topY, YAPEMPatchSettings settings)
        {
            Color colBio = settings.ColorBionic;
            Color colArcho = settings.ColorArchotech;
            Color colUltra = settings.ColorUltratech;
            Color colAdv = settings.ColorAdvancedBionic;

            Color colRoyalty = settings.UseDLCColors ? settings.ColorRoyalty : colBio;
            Color colBiotech = settings.UseDLCColors ? settings.ColorBiotech : colBio;
            Color colAnomaly = settings.UseDLCColors ? settings.ColorAnomaly : colBio;
            Color colOdyssey = settings.UseDLCColors ? settings.ColorOdyssey : colBio;

            // Brain (inside Head)
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
            Rect brainRect = new Rect(center.x - 11f, topY + 14f, 22f, 22f);
            DrawOrganIcon(texBrain, brainRect, brainCol, brainName);

            // Heart (Chest Right)
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
            Rect heartRect = new Rect(center.x - 20f, topY + 62f, 18f, 18f);
            DrawOrganIcon(texHeart, heartRect, heartCol, heartName);

            // Lung (Chest Left)
            string lungName = (CurrentPreset == PreviewPreset.DLCImplants) ? "Detoxifier Lung (Biotech)" : "Bionic Lung";
            Color lungCol = (CurrentPreset == PreviewPreset.DLCImplants) ? colBiotech : colBio;
            Rect lungRect = new Rect(center.x + 3f, topY + 62f, 18f, 18f);
            DrawOrganIcon(texLung, lungRect, lungCol, lungName);

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
            Rect stomachRect = new Rect(center.x - 9f, topY + 84f, 18f, 18f);
            DrawOrganIcon(texStomach, stomachRect, stomachCol, stomachName);

            // Kidney (Abdomen Side)
            string kidneyName = (CurrentPreset == PreviewPreset.DLCImplants) ? "Detoxifier Kidney (Biotech)" : "Bionic Kidney";
            Color kidneyCol = (CurrentPreset == PreviewPreset.DLCImplants) ? colBiotech : colBio;
            Rect kidneyRect = new Rect(center.x + 9f, topY + 98f, 16f, 16f);
            DrawOrganIcon(texKidney, kidneyRect, kidneyCol, kidneyName);
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
                GUI.DrawTextureWithTexCoords(r, tex, new Rect(1f, 0f, -1f, 1f));
            }
            else
            {
                Widgets.DrawBoxSolid(r, col);
            }
            GUI.color = prev;
        }

        private static void DrawIndicatorIcon(Texture2D tex, Rect r, Color col, bool flipped, string tip)
        {
            // Indicator slot background
            Widgets.DrawBoxSolid(r, new Color(0.12f, 0.14f, 0.17f, 0.95f));
            Widgets.DrawBox(r, 1);

            Rect iconInner = GenUI.ContractedBy(r, 2f);
            Color prev = GUI.color;
            GUI.color = col;
            if (tex != null)
            {
                if (flipped)
                    GUI.DrawTextureWithTexCoords(iconInner, tex, new Rect(1f, 0f, -1f, 1f));
                else
                    GUI.DrawTexture(iconInner, tex);
            }
            else
            {
                Widgets.DrawBoxSolid(iconInner, col);
            }
            GUI.color = prev;

            TooltipHandler.TipRegion(r, $"{tip}\n#{ColorUtility.ToHtmlStringRGB(col)}");
        }

        private static void DrawOrganIcon(Texture2D tex, Rect r, Color col, string tip)
        {
            // Glow backdrop
            Widgets.DrawBoxSolid(GenUI.ExpandedBy(r, 2f), new Color(col.r, col.g, col.b, 0.35f));
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
                    statusCol = new Color(0.35f, 0.90f, 0.45f);
                }
                else if (CurrentPreset == PreviewPreset.DLCImplants && settings.RequiredLimbCount <= 3)
                {
                    status = "NHT_YAPEM_DominantActive".Translate(3);
                    statusCol = new Color(0.35f, 0.90f, 0.45f);
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
