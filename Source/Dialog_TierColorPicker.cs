using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace NiceHealthTab_YAPEM_Patch
{
    public class Dialog_TierColorPicker : Window
    {
        private const int TexSize = 220;
        private const float Center = TexSize / 2f;
        private const float RadiusOuter = 100f;
        private const float RadiusInner = 76f;
        private const float RadiusDiamond = 60f;
        public const int CustomSlotCount = 8;

        private struct PresetInfo
        {
            public string NameKey;
            public string Fallback;
            public Color Color;

            public PresetInfo(string nameKey, string fallback, Color color)
            {
                NameKey = nameKey;
                Fallback = fallback;
                Color = color;
            }

            public string Tooltip
            {
                get
                {
                    string label = NameKey.Translate().RawText;
                    if (string.IsNullOrEmpty(label) || label == NameKey)
                    {
                        label = Fallback;
                    }
                    return $"{label} (#{ColorUtility.ToHtmlStringRGB(Color)})";
                }
            }
        }

        // Canonical default presets with category names for tooltips
        private static readonly PresetInfo[] DefaultPresets = new PresetInfo[]
        {
            new PresetInfo("NHT_YAPEM_TierArchotech",  "Archotech",           new Color(203f / 255f, 208f / 255f, 171f / 255f)), // #CBD0AB
            new PresetInfo("NHT_YAPEM_TierAdvBionic",  "Advanced Bionic",     new Color(161f / 255f, 206f / 255f, 243f / 255f)), // #A1CEF3
            new PresetInfo("NHT_YAPEM_TierUltratech",  "Ultratech",           new Color(113f / 255f, 203f / 255f, 194f / 255f)), // #71CBC2
            new PresetInfo("NHT_YAPEM_TierBionic",     "Standard Bionic",     new Color(230f / 255f, 230f / 255f, 230f / 255f)), // #E6E6E6
            new PresetInfo("NHT_YAPEM_TierProsthetic", "Simple Prosthetic",   new Color(176f / 255f, 176f / 255f, 176f / 255f)), // #B0B0B0
            new PresetInfo("NHT_YAPEM_PureWhite",      "Pure White",          new Color(1f, 1f, 1f))                              // #FFFFFF
        };

        private readonly string title;
        private readonly Color originalColor;
        private readonly Color defaultColor;
        private readonly Action<Color> onColorSaved;

        private Color currentColor;
        private float currentH;
        private float currentS;
        private float currentL;

        private Texture2D wheelTexture;
        private string hexBuffer;
        private bool isDraggingHue;
        private bool isDraggingDiamond;
        private bool accepted;

        public override Vector2 InitialSize => new Vector2(440f, 650f);

        public Dialog_TierColorPicker(string title, Color initialColor, Color defaultColor, Action<Color> onColorSaved)
        {
            this.title = title;
            this.originalColor = initialColor;
            this.defaultColor = defaultColor;
            this.currentColor = initialColor;
            this.onColorSaved = onColorSaved;

            closeOnClickedOutside = true;
            closeOnAccept = true;
            doCloseButton = false;
            doCloseX = false;
            absorbInputAroundWindow = true;

            hexBuffer = ColorUtility.ToHtmlStringRGB(currentColor);
            RgbToHsl(currentColor, out currentH, out currentS, out currentL);

            EnsureCustomPresetSlots();
            GenerateWheelTexture();
        }

        private static void EnsureCustomPresetSlots()
        {
            if (YAPEMPatchMod.Settings == null) return;
            if (YAPEMPatchMod.Settings.CustomPresets == null)
            {
                YAPEMPatchMod.Settings.CustomPresets = new List<Color>();
            }
            while (YAPEMPatchMod.Settings.CustomPresets.Count < CustomSlotCount)
            {
                YAPEMPatchMod.Settings.CustomPresets.Add(Color.clear);
            }
        }

        private void GenerateWheelTexture()
        {
            if (wheelTexture != null)
            {
                UnityEngine.Object.Destroy(wheelTexture);
            }

            wheelTexture = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false);
            wheelTexture.wrapMode = TextureWrapMode.Clamp;
            wheelTexture.filterMode = FilterMode.Bilinear;

            // 1. Draw Outer Hue Ring
            for (int y = 0; y < TexSize; y++)
            {
                float dy_gui = Center - y;
                for (int x = 0; x < TexSize; x++)
                {
                    float dx_gui = x - Center;
                    float dist = Mathf.Sqrt(dx_gui * dx_gui + dy_gui * dy_gui);

                    if (dist >= RadiusInner - 1f && dist <= RadiusOuter + 1f)
                    {
                        float angle = Mathf.Atan2(dy_gui, dx_gui);
                        float h = (angle / (2f * Mathf.PI) + 1f) % 1f;
                        Color hueColor = HslToRgb(h, 1f, 0.5f);

                        // Antialiased ring boundaries
                        float alpha = 1f;
                        if (dist < RadiusInner)
                            alpha = Mathf.Clamp01(dist - (RadiusInner - 1f));
                        else if (dist > RadiusOuter)
                            alpha = Mathf.Clamp01((RadiusOuter + 1f) - dist);

                        hueColor.a = alpha;
                        wheelTexture.SetPixel(x, y, hueColor);
                    }
                    else
                    {
                        wheelTexture.SetPixel(x, y, Color.clear);
                    }
                }
            }

            // 2. Draw Diamond
            RedrawDiamond();
        }

        private void RedrawDiamond()
        {
            if (wheelTexture == null) return;

            int minX = Mathf.FloorToInt(Center - RadiusDiamond - 1f);
            int maxX = Mathf.CeilToInt(Center + RadiusDiamond + 1f);
            int minY = Mathf.FloorToInt(Center - RadiusDiamond - 1f);
            int maxY = Mathf.CeilToInt(Center + RadiusDiamond + 1f);

            for (int y = minY; y <= maxY; y++)
            {
                float dy_gui = Center - y;
                for (int x = minX; x <= maxX; x++)
                {
                    float dx_gui = x - Center;
                    float manhattan = Mathf.Abs(dx_gui) + Mathf.Abs(dy_gui);

                    if (manhattan <= RadiusDiamond + 0.75f)
                    {
                        // Top (dy_gui = -RadiusDiamond) -> White (l = 1.0)
                        // Bottom (dy_gui = +RadiusDiamond) -> Black (l = 0.0)
                        float l = Mathf.Clamp01(0.5f - dy_gui / (2f * RadiusDiamond));
                        float span = RadiusDiamond - Mathf.Abs(dy_gui);
                        float s = span > 0.001f ? Mathf.Clamp01((dx_gui + span) / (2f * span)) : 0f;

                        Color col = HslToRgb(currentH, s, l);

                        // Antialiased diamond boundary
                        float edgeDist = RadiusDiamond - manhattan;
                        if (edgeDist < 0f)
                            col.a = Mathf.Clamp01(1f + edgeDist);
                        else
                            col.a = 1f;

                        wheelTexture.SetPixel(x, y, col);
                    }
                    else
                    {
                        float dist = Mathf.Sqrt(dx_gui * dx_gui + dy_gui * dy_gui);
                        if (dist < RadiusInner)
                        {
                            wheelTexture.SetPixel(x, y, Color.clear);
                        }
                    }
                }
            }

            wheelTexture.Apply();
        }

        public override void DoWindowContents(Rect inRect)
        {
            EnsureCustomPresetSlots();

            // 1. Header Bar: Swatch Icon, Category Title, Subtitle, Close 'X'
            float iconSize = 22f;
            Rect iconRect = new Rect(0f, 4f, iconSize, iconSize);
            Widgets.DrawBoxSolid(iconRect, currentColor);
            Widgets.DrawBox(iconRect, 1);

            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(iconSize + 8f, 0f, inRect.width - iconSize - 36f, 24f);
            Widgets.Label(titleRect, title);

            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.68f, 0.72f, 0.78f, 1f);
            Rect subTitleRect = new Rect(iconSize + 8f, 22f, inRect.width - iconSize - 36f, 16f);
            Widgets.Label(subTitleRect, "NHT_YAPEM_ColorPickerSubtitle".Translate() + $" • #{ColorUtility.ToHtmlStringRGB(currentColor)}");
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            Rect closeBtnRect = new Rect(inRect.width - 24f, 2f, 22f, 22f);
            if (Widgets.ButtonText(closeBtnRect, "✕"))
            {
                onColorSaved?.Invoke(originalColor);
                Close();
            }

            // Divider 1
            Widgets.DrawLineHorizontal(0f, 40f, inRect.width);

            // 2. Central Wheel Canvas
            Rect wheelRect = new Rect((inRect.width - TexSize) / 2f, 44f, TexSize, TexSize);
            if (wheelTexture != null)
            {
                GUI.DrawTexture(wheelRect, wheelTexture);
            }

            // Subtle HSV Diamond badge
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.45f, 0.50f, 0.55f, 0.8f);
            Widgets.Label(new Rect(wheelRect.xMax - 66f, wheelRect.yMax - 14f, 66f, 14f), "HSV Diamond");
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            // Mouse interaction with Wheel & Diamond
            Vector2 mouse = Event.current.mousePosition;
            Vector2 local = mouse - new Vector2(wheelRect.x, wheelRect.y);
            float dx = local.x - Center;
            float dy = local.y - Center;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && wheelRect.Contains(mouse))
            {
                if (dist >= RadiusInner - 6f && dist <= RadiusOuter + 6f)
                {
                    isDraggingHue = true;
                    UpdateHue(dx, dy);
                    Event.current.Use();
                }
                else if (Mathf.Abs(dx) + Mathf.Abs(dy) <= RadiusDiamond + 6f)
                {
                    isDraggingDiamond = true;
                    UpdateDiamond(dx, dy);
                    Event.current.Use();
                }
            }
            else if (Event.current.type == EventType.MouseDrag && Event.current.button == 0)
            {
                if (isDraggingHue)
                {
                    UpdateHue(dx, dy);
                    Event.current.Use();
                }
                else if (isDraggingDiamond)
                {
                    UpdateDiamond(dx, dy);
                    Event.current.Use();
                }
            }
            else if (Event.current.rawType == EventType.MouseUp)
            {
                isDraggingHue = false;
                isDraggingDiamond = false;
            }

            // Reticle cursors
            DrawHueCursor(wheelRect);
            DrawDiamondCursor(wheelRect);

            // Divider 2
            float yDiv2 = wheelRect.yMax + 6f;
            Widgets.DrawLineHorizontal(0f, yDiv2, inRect.width);

            // 3. Row 1: Default Presets with Category Tooltips
            float yPresets1 = yDiv2 + 4f;
            float chipSize = 22f;
            float chipGap = 5f;

            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.75f, 0.78f, 0.82f, 1f);
            Widgets.Label(new Rect(0f, yPresets1, 150f, 16f), "NHT_YAPEM_PresetsDefault".Translate());
            GUI.color = new Color(0.52f, 0.58f, 0.65f, 1f);
            Widgets.Label(new Rect(inRect.width - 95f, yPresets1, 95f, 16f), $"{DefaultPresets.Length} {"NHT_YAPEM_PresetsCount".Translate()}");
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            float chipsRow1Y = yPresets1 + 18f;
            float totalWidth1 = DefaultPresets.Length * chipSize + (DefaultPresets.Length - 1) * chipGap;
            float startX1 = (inRect.width - totalWidth1) / 2f;

            for (int i = 0; i < DefaultPresets.Length; i++)
            {
                var preset = DefaultPresets[i];
                Rect chipRect = new Rect(startX1 + i * (chipSize + chipGap), chipsRow1Y, chipSize, chipSize);

                Widgets.DrawBoxSolid(chipRect, preset.Color);
                Widgets.DrawBox(chipRect, 1);
                Widgets.DrawHighlightIfMouseover(chipRect);

                if (IsColorClose(currentColor, preset.Color))
                {
                    Widgets.DrawBox(chipRect.ExpandedBy(2f), 2);
                }

                TooltipHandler.TipRegion(chipRect, preset.Tooltip);

                if (Widgets.ButtonInvisible(chipRect))
                {
                    SelectColor(preset.Color);
                }
            }

            // Divider 3
            float yDiv3 = chipsRow1Y + chipSize + 6f;
            Widgets.DrawLineHorizontal(0f, yDiv3, inRect.width);

            // 4. Row 2: User Custom Preset Slots [1..8]
            float yPresets2 = yDiv3 + 4f;

            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.75f, 0.78f, 0.82f, 1f);
            Widgets.Label(new Rect(0f, yPresets2, 160f, 16f), "NHT_YAPEM_PresetsCustom".Translate());
            GUI.color = new Color(0.52f, 0.58f, 0.65f, 1f);
            Widgets.Label(new Rect(inRect.width - 150f, yPresets2, 150f, 16f), "NHT_YAPEM_CustomSlotsHint".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            float chipsRow2Y = yPresets2 + 18f;
            List<Color> customPresets = YAPEMPatchMod.Settings?.CustomPresets;
            float totalWidth2 = CustomSlotCount * chipSize + (CustomSlotCount - 1) * chipGap;
            float startX2 = (inRect.width - totalWidth2) / 2f;

            for (int i = 0; i < CustomSlotCount; i++)
            {
                int slotIndex = i;
                int slotNumber = i + 1;
                Rect chipRect = new Rect(startX2 + i * (chipSize + chipGap), chipsRow2Y, chipSize, chipSize);

                bool isFilled = (customPresets != null && slotIndex < customPresets.Count && customPresets[slotIndex].a > 0.01f);

                if (isFilled)
                {
                    Color c = customPresets[slotIndex];
                    Widgets.DrawBoxSolid(chipRect, c);
                    Widgets.DrawBox(chipRect, 1);
                    Widgets.DrawHighlightIfMouseover(chipRect);

                    if (IsColorClose(currentColor, c))
                    {
                        Widgets.DrawBox(chipRect.ExpandedBy(2f), 2);
                    }

                    // Tiny slot number badge
                    Text.Font = GameFont.Tiny;
                    GUI.color = Color.white;
                    Widgets.Label(new Rect(chipRect.x + 2f, chipRect.y - 1f, 12f, 12f), slotNumber.ToString());
                    Text.Font = GameFont.Small;

                    string tip = string.Format("NHT_YAPEM_SlotTitle".Translate().RawText, slotNumber) + $" (#{ColorUtility.ToHtmlStringRGB(c)})\n" + "NHT_YAPEM_CustomSlotTip".Translate().RawText;
                    TooltipHandler.TipRegion(chipRect, tip);

                    if (Widgets.ButtonInvisible(chipRect, true))
                    {
                        if (Event.current.button == 1) // RMB
                        {
                            OpenCustomSlotMenu(slotIndex, slotNumber, c);
                        }
                        else // LMB
                        {
                            SelectColor(c);
                        }
                    }
                    else if (Event.current.type == EventType.ContextClick && Mouse.IsOver(chipRect))
                    {
                        OpenCustomSlotMenu(slotIndex, slotNumber, c);
                        Event.current.Use();
                    }
                }
                else
                {
                    // Empty slot: clickable to save directly into this exact slot
                    Widgets.DrawBoxSolid(chipRect, new Color(0.12f, 0.14f, 0.18f, 0.9f));
                    Widgets.DrawBox(chipRect, 1);
                    Widgets.DrawHighlightIfMouseover(chipRect);

                    Text.Font = GameFont.Tiny;
                    GUI.color = new Color(0.48f, 0.52f, 0.58f, 1f);
                    Widgets.Label(new Rect(chipRect.x + 2f, chipRect.y - 1f, 10f, 10f), slotNumber.ToString());
                    Widgets.Label(new Rect(chipRect.x + 7f, chipRect.y + 2f, 12f, 14f), "+");
                    GUI.color = Color.white;
                    Text.Font = GameFont.Small;

                    string tip = string.Format("NHT_YAPEM_SlotTitle".Translate().RawText, slotNumber) + ": " + "NHT_YAPEM_CustomSlotEmptyTip".Translate().RawText;
                    TooltipHandler.TipRegion(chipRect, tip);

                    if (Widgets.ButtonInvisible(chipRect, true))
                    {
                        customPresets[slotIndex] = currentColor;
                        YAPEMPatchMod.Settings?.Write();
                    }
                    else if (Event.current.type == EventType.ContextClick && Mouse.IsOver(chipRect))
                    {
                        customPresets[slotIndex] = currentColor;
                        YAPEMPatchMod.Settings?.Write();
                        Event.current.Use();
                    }
                }
            }

            // Divider 4
            float yDiv4 = chipsRow2Y + chipSize + 6f;
            Widgets.DrawLineHorizontal(0f, yDiv4, inRect.width);

            // 5. Comparison & Value Inspector Card
            float yCard = yDiv4 + 6f;
            float cardH = 68f;
            Rect cardRect = new Rect(0f, yCard, inRect.width, cardH);
            Widgets.DrawBoxSolid(cardRect, new Color(0.10f, 0.12f, 0.15f, 0.75f));
            Widgets.DrawBox(cardRect, 1);

            // Split Swatch (Left = Before, Right = After)
            float swX = cardRect.x + 10f;
            float swY = cardRect.y + 10f;
            float swW = 64f;
            float swH = 36f;
            Rect splitRect = new Rect(swX, swY, swW, swH);
            Rect leftHalf = new Rect(swX, swY, swW / 2f, swH);
            Rect rightHalf = new Rect(swX + swW / 2f, swY, swW / 2f, swH);

            Widgets.DrawBoxSolid(leftHalf, originalColor);
            Widgets.DrawBoxSolid(rightHalf, currentColor);
            Widgets.DrawBox(splitRect, 1);
            Widgets.DrawLineVertical(swX + swW / 2f, swY, swH);
            Widgets.DrawHighlightIfMouseover(splitRect);

            TooltipHandler.TipRegion(leftHalf, $"#{ColorUtility.ToHtmlStringRGB(originalColor)} (" + "NHT_YAPEM_ColorBefore".Translate().RawText + ")");
            TooltipHandler.TipRegion(rightHalf, $"#{ColorUtility.ToHtmlStringRGB(currentColor)} (" + "NHT_YAPEM_ColorAfter".Translate().RawText + ")");

            if (Widgets.ButtonInvisible(leftHalf))
            {
                SelectColor(originalColor);
            }

            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.65f, 0.68f, 0.74f, 1f);
            Widgets.Label(new Rect(swX + 4f, swY + swH + 2f, 30f, 14f), "NHT_YAPEM_ColorBefore".Translate());
            Widgets.Label(new Rect(swX + swW / 2f + 4f, swY + swH + 2f, 30f, 14f), "NHT_YAPEM_ColorAfter".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            // HEX Text Input & Copy Button
            float hexX = splitRect.xMax + 14f;
            Rect hexHashRect = new Rect(hexX, cardRect.y + 16f, 12f, 24f);
            Widgets.Label(hexHashRect, "#");

            Rect hexFieldRect = new Rect(hexHashRect.xMax + 2f, cardRect.y + 14f, 84f, 26f);
            string newHex = Widgets.TextField(hexFieldRect, hexBuffer);
            if (newHex != hexBuffer)
            {
                hexBuffer = newHex.Trim().TrimStart('#').ToUpperInvariant();
                if (hexBuffer.Length == 6 && ColorUtility.TryParseHtmlString("#" + hexBuffer, out Color parsed))
                {
                    currentColor = parsed;
                    RgbToHsl(currentColor, out currentH, out currentS, out currentL);
                    RedrawDiamond();
                    onColorSaved?.Invoke(currentColor);
                }
            }

            // Copy [📋] button
            Rect copyBtnRect = new Rect(hexFieldRect.xMax + 4f, cardRect.y + 14f, 28f, 26f);
            TooltipHandler.TipRegion(copyBtnRect, "NHT_YAPEM_CopyHex".Translate());
            if (Widgets.ButtonText(copyBtnRect, "📋"))
            {
                string curHex = ColorUtility.ToHtmlStringRGB(currentColor);
                GUIUtility.systemCopyBuffer = "#" + curHex;
                Messages.Message(string.Format("NHT_YAPEM_HexCopied".Translate().RawText, curHex), MessageTypeDefOf.TaskCompletion, false);
            }

            // Numeric Readouts: RGB and HSL
            int r255 = Mathf.RoundToInt(currentColor.r * 255f);
            int g255 = Mathf.RoundToInt(currentColor.g * 255f);
            int b255 = Mathf.RoundToInt(currentColor.b * 255f);

            int hDeg = Mathf.RoundToInt(currentH * 360f);
            int sPct = Mathf.RoundToInt(currentS * 100f);
            int lPct = Mathf.RoundToInt(currentL * 100f);

            float infoX = copyBtnRect.xMax + 12f;
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.82f, 0.85f, 0.90f, 1f);
            Widgets.Label(new Rect(infoX, cardRect.y + 12f, inRect.width - infoX, 16f), $"R: {r255}   G: {g255}   B: {b255}");
            GUI.color = new Color(0.65f, 0.70f, 0.78f, 1f);
            Widgets.Label(new Rect(infoX, cardRect.y + 30f, inRect.width - infoX, 16f), $"H: {hDeg}°   S: {sPct}%   L: {lPct}%");
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            // Divider 5
            float yDiv5 = cardRect.yMax + 6f;
            Widgets.DrawLineHorizontal(0f, yDiv5, inRect.width);

            // 6. Profile Toolbar
            float yProfile = yDiv5 + 6f;
            Rect profRect = new Rect(0f, yProfile, inRect.width, 28f);
            DrawProfileBar(profRect);

            // Divider 6
            float yDiv6 = yProfile + 34f;
            Widgets.DrawLineHorizontal(0f, yDiv6, inRect.width);

            // 7. Action Buttons Bar
            float yBtns = inRect.height - 34f;
            float btnH = 30f;

            // Reset Button
            Rect resetRect = new Rect(0f, yBtns, 110f, btnH);
            if (Widgets.ButtonText(resetRect, "NHT_YAPEM_ResetBtn".Translate()))
            {
                SelectColor(defaultColor);
            }

            // Cancel Button
            Rect cancelRect = new Rect(inRect.width - 200f, yBtns, 90f, btnH);
            if (Widgets.ButtonText(cancelRect, "Cancel".Translate()))
            {
                onColorSaved?.Invoke(originalColor);
                Close();
            }

            // Accept / OK Button
            Rect okRect = new Rect(inRect.width - 100f, yBtns, 100f, btnH);
            if (Widgets.ButtonText(okRect, "NHT_YAPEM_ApplyBtn".Translate()))
            {
                accepted = true;
                onColorSaved?.Invoke(currentColor);
                YAPEMPatchMod.Settings?.SyncActiveColorsToProfile();
                YAPEMPatchMod.Settings?.Write();
                Close();
            }
        }

        private void DrawProfileBar(Rect rect)
        {
            var settings = YAPEMPatchMod.Settings;
            if (settings == null) return;

            settings.EnsureProfilesInitialized();
            var activeProf = settings.GetActiveProfile();

            float x = rect.x;
            float y = rect.y;

            Text.Font = GameFont.Small;
            string profLabel = "NHT_YAPEM_Profile".Translate() + ":";
            Vector2 labelSize = Text.CalcSize(profLabel);
            Widgets.Label(new Rect(x, y + 2f, labelSize.x + 6f, rect.height), profLabel);
            x += labelSize.x + 8f;

            // Dropdown
            float dropW = 125f;
            Rect dropRect = new Rect(x, y, dropW, rect.height);
            string dropTitle = (activeProf?.Label ?? "Profile") + " ▾";
            if (Widgets.ButtonText(dropRect, dropTitle))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (var p in settings.Profiles)
                {
                    var captured = p;
                    options.Add(new FloatMenuOption(captured.Label + (captured.Name == settings.ActiveProfileName ? " ✓" : ""), () =>
                    {
                        settings.ApplyProfile(captured, true);
                        // Refresh current color from the newly active profile
                        SelectColor(settings.ColorAdvancedBionic);
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
            x += dropW + 4f;

            // [+ Нов.]
            float btnW = 48f;
            Rect newBtnRect = new Rect(x, y, btnW, rect.height);
            TooltipHandler.TipRegion(newBtnRect, "NHT_YAPEM_NewProfilePrompt".Translate());
            if (Widgets.ButtonText(newBtnRect, "NHT_YAPEM_NewProfile".Translate()))
            {
                string defName = string.Format("NHT_YAPEM_NewProfileDefaultName".Translate().RawText, settings.Profiles.Count(p => !p.IsBuiltIn) + 1);
                Find.WindowStack.Add(new Dialog_InputText("NHT_YAPEM_NewProfilePrompt".Translate(), defName, (enteredName) =>
                {
                    settings.CreateProfile(enteredName);
                }));
            }
            x += btnW + 4f;

            // [✕ Удал]
            bool canDelete = activeProf != null && !activeProf.IsBuiltIn;
            Rect delBtnRect = new Rect(x, y, btnW, rect.height);
            if (!canDelete)
            {
                GUI.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                TooltipHandler.TipRegion(delBtnRect, "NHT_YAPEM_CannotDeleteDefault".Translate());
            }
            else
            {
                TooltipHandler.TipRegion(delBtnRect, string.Format("NHT_YAPEM_DeleteProfileConfirm".Translate().RawText, activeProf.Label));
            }

            if (Widgets.ButtonText(delBtnRect, "NHT_YAPEM_DeleteProfile".Translate()))
            {
                if (canDelete)
                {
                    string msg = string.Format("NHT_YAPEM_DeleteProfileConfirm".Translate().RawText, activeProf.Label);
                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(msg, () =>
                    {
                        settings.DeleteProfile(activeProf);
                    }));
                }
                else
                {
                    Messages.Message("NHT_YAPEM_CannotDeleteDefault".Translate(), MessageTypeDefOf.RejectInput, false);
                }
            }
            GUI.color = Color.white;
            x += btnW + 6f;

            // [Эксп.]
            float smallBtnW = 44f;
            Rect expBtnRect = new Rect(x, y, smallBtnW, rect.height);
            TooltipHandler.TipRegion(expBtnRect, "NHT_YAPEM_ProfileExported".Translate());
            if (Widgets.ButtonText(expBtnRect, "NHT_YAPEM_ExportProfile".Translate()))
            {
                if (activeProf != null)
                {
                    GUIUtility.systemCopyBuffer = activeProf.ExportJson();
                    Messages.Message("NHT_YAPEM_ProfileExported".Translate(), MessageTypeDefOf.PositiveEvent, false);
                }
            }
            x += smallBtnW + 4f;

            // [Имп.]
            Rect impBtnRect = new Rect(x, y, smallBtnW, rect.height);
            TooltipHandler.TipRegion(impBtnRect, "NHT_YAPEM_ImportProfile".Translate());
            if (Widgets.ButtonText(impBtnRect, "NHT_YAPEM_ImportProfile".Translate()))
            {
                string json = GUIUtility.systemCopyBuffer;
                var imported = PaletteProfile.ImportJson(json);
                if (imported != null)
                {
                    settings.Profiles.Add(imported);
                    settings.ApplyProfile(imported, true);
                    Messages.Message(string.Format("NHT_YAPEM_ProfileImported".Translate().RawText, imported.Name), MessageTypeDefOf.PositiveEvent, false);
                }
                else
                {
                    Messages.Message("NHT_YAPEM_InvalidProfileJson".Translate(), MessageTypeDefOf.RejectInput, false);
                }
            }
        }

        private void OpenCustomSlotMenu(int index, int slotNumber, Color slotColor)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            string slotHex = ColorUtility.ToHtmlStringRGB(slotColor);
            string curHex = ColorUtility.ToHtmlStringRGB(currentColor);
            string prefix = string.Format("NHT_YAPEM_SlotTitle".Translate().RawText, slotNumber) + ": ";

            // 1. Select / Apply this slot color
            options.Add(new FloatMenuOption(prefix + string.Format("NHT_YAPEM_ApplySlot".Translate().RawText, slotHex), () =>
            {
                SelectColor(slotColor);
            }));

            // 2. Overwrite this slot with current color
            options.Add(new FloatMenuOption(prefix + string.Format("NHT_YAPEM_OverwriteSlot".Translate().RawText, curHex), () =>
            {
                var custom = YAPEMPatchMod.Settings?.CustomPresets;
                if (custom != null && index >= 0 && index < custom.Count)
                {
                    custom[index] = currentColor;
                    YAPEMPatchMod.Settings?.Write();
                }
            }));

            // 3. Clear this slot
            options.Add(new FloatMenuOption(prefix + "NHT_YAPEM_ClearSlot".Translate().RawText, () =>
            {
                var custom = YAPEMPatchMod.Settings?.CustomPresets;
                if (custom != null && index >= 0 && index < custom.Count)
                {
                    custom[index] = Color.clear;
                    YAPEMPatchMod.Settings?.Write();
                }
            }));

            // 4. Clear all custom presets
            options.Add(new FloatMenuOption("NHT_YAPEM_ClearAllSlots".Translate().RawText, () =>
            {
                var custom = YAPEMPatchMod.Settings?.CustomPresets;
                if (custom != null)
                {
                    for (int s = 0; s < custom.Count; s++)
                    {
                        custom[s] = Color.clear;
                    }
                    YAPEMPatchMod.Settings?.Write();
                }
            }));

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static bool IsColorClose(Color a, Color b)
        {
            if (a.a <= 0.01f || b.a <= 0.01f) return false;
            return Mathf.Abs(a.r - b.r) < 0.02f &&
                   Mathf.Abs(a.g - b.g) < 0.02f &&
                   Mathf.Abs(a.b - b.b) < 0.02f;
        }

        private void SelectColor(Color color)
        {
            currentColor = color;
            hexBuffer = ColorUtility.ToHtmlStringRGB(currentColor);
            RgbToHsl(currentColor, out currentH, out currentS, out currentL);
            RedrawDiamond();
            onColorSaved?.Invoke(currentColor);
        }

        private void UpdateHue(float dx, float dy)
        {
            float angle = Mathf.Atan2(dy, dx);
            currentH = (angle / (2f * Mathf.PI) + 1f) % 1f;
            currentColor = HslToRgb(currentH, currentS, currentL);
            hexBuffer = ColorUtility.ToHtmlStringRGB(currentColor);
            RedrawDiamond();
            onColorSaved?.Invoke(currentColor);
        }

        private void UpdateDiamond(float dx, float dy)
        {
            dy = Mathf.Clamp(dy, -RadiusDiamond, RadiusDiamond);
            currentL = Mathf.Clamp01(0.5f - dy / (2f * RadiusDiamond));
            float span = RadiusDiamond - Mathf.Abs(dy);
            currentS = span > 0.001f ? Mathf.Clamp01((dx + span) / (2f * span)) : 0f;

            currentColor = HslToRgb(currentH, currentS, currentL);
            hexBuffer = ColorUtility.ToHtmlStringRGB(currentColor);
            onColorSaved?.Invoke(currentColor);
        }

        private void DrawHueCursor(Rect wheelRect)
        {
            float angle = currentH * 2f * Mathf.PI;
            float rMid = (RadiusInner + RadiusOuter) / 2f;
            float x = wheelRect.x + Center + rMid * Mathf.Cos(angle);
            float y = wheelRect.y + Center + rMid * Mathf.Sin(angle);

            DrawCursor(new Vector2(x, y));
        }

        private void DrawDiamondCursor(Rect wheelRect)
        {
            float dy = (0.5f - currentL) * 2f * RadiusDiamond;
            float span = RadiusDiamond - Mathf.Abs(dy);
            float dx = (2f * currentS - 1f) * span;

            float x = wheelRect.x + Center + dx;
            float y = wheelRect.y + Center + dy;

            DrawCursor(new Vector2(x, y));
        }

        private void DrawCursor(Vector2 center)
        {
            Rect outer = new Rect(center.x - 7f, center.y - 7f, 14f, 14f);
            Widgets.DrawBoxSolid(outer, new Color(0f, 0f, 0f, 0.95f));
            Rect middle = outer.ContractedBy(1.5f);
            Widgets.DrawBoxSolid(middle, Color.white);
            Rect inner = middle.ContractedBy(1.5f);
            Widgets.DrawBoxSolid(inner, currentColor);
        }

        public override void PreClose()
        {
            base.PreClose();
            if (!accepted)
            {
                onColorSaved?.Invoke(originalColor);
            }
        }

        public override void PostClose()
        {
            base.PostClose();
            if (wheelTexture != null)
            {
                UnityEngine.Object.Destroy(wheelTexture);
                wheelTexture = null;
            }
        }

        public static Color HslToRgb(float h, float s, float l)
        {
            if (s <= 0.0001f) return new Color(l, l, l, 1f);
            float q = l < 0.5f ? l * (1f + s) : l + s - l * s;
            float p = 2f * l - q;
            return new Color(HueToRgb(p, q, h + 1f / 3f), HueToRgb(p, q, h), HueToRgb(p, q, h - 1f / 3f), 1f);
        }

        private static float HueToRgb(float p, float q, float t)
        {
            t = (t % 1f + 1f) % 1f;
            if (t < 1f / 6f) return p + (q - p) * 6f * t;
            if (t < 1f / 2f) return q;
            if (t < 2f / 3f) return p + (q - p) * (2f / 3f - t) * 6f;
            return p;
        }

        public static void RgbToHsl(Color col, out float h, out float s, out float l)
        {
            float r = col.r;
            float g = col.g;
            float b = col.b;
            float max = Mathf.Max(r, Mathf.Max(g, b));
            float min = Mathf.Min(r, Mathf.Min(g, b));
            l = (max + min) / 2f;

            if (Mathf.Abs(max - min) < 0.0001f)
            {
                h = 0f;
                s = 0f;
            }
            else
            {
                float d = max - min;
                s = l > 0.5f ? d / (2f - max - min) : d / (max + min);
                if (Mathf.Approximately(max, r))
                    h = (g - b) / d + (g < b ? 6f : 0f);
                else if (Mathf.Approximately(max, g))
                    h = (b - r) / d + 2f;
                else
                    h = (r - g) / d + 4f;
                h /= 6f;
            }
        }
    }
}
