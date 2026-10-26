using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace NiceHealthTab_YAPEM_Patch
{
    public class Dialog_TierColorPicker : Window
    {
        private const int TexSize = 200;
        private const float Center = TexSize / 2f;
        private const float RadiusOuter = 94f;
        private const float RadiusInner = 72f;
        private const float RadiusDiamond = 58f;
        private const int CustomSlotCount = 8;

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
                    string label = NameKey.Translate();
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
            new PresetInfo("NHT_YAPEM_TierRoyalty",    "Royalty",             new Color(222f / 255f, 184f / 255f, 65f / 255f)),  // #DEB841
            new PresetInfo("NHT_YAPEM_TierBiotech",    "Biotech",             new Color(56f / 255f, 217f / 255f, 169f / 255f)),  // #38D9A9
            new PresetInfo("NHT_YAPEM_TierAnomaly",    "Anomaly",             new Color(168f / 255f, 85f / 255f, 247f / 255f)),  // #A855F7
            new PresetInfo("NHT_YAPEM_TierOdyssey",    "Odyssey",             new Color(56f / 255f, 189f / 255f, 248f / 255f)),  // #38BDF8
            new PresetInfo("NHT_YAPEM_TierIntegrated", "Integrated Implants", new Color(153f / 255f, 153f / 255f, 255f / 255f)), // #9999FF
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

        public override Vector2 InitialSize => new Vector2(390f, 530f);

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

            GenerateWheelTexture();
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

            // 1. Draw Hue Ring
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
                        // In GUI coords: Top (dy_gui = -RadiusDiamond) -> White (l = 1.0)
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
            // 1. Header: Category Title & Close 'X' Button
            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(0f, 0f, inRect.width - 28f, 28f);
            Widgets.Label(titleRect, title);
            Text.Font = GameFont.Small;

            Rect closeBtnRect = new Rect(inRect.width - 24f, 2f, 22f, 22f);
            if (Widgets.ButtonText(closeBtnRect, "✕"))
            {
                onColorSaved?.Invoke(originalColor);
                Close();
            }

            // Divider 1
            Widgets.DrawLineHorizontal(0f, 32f, inRect.width);

            // 2. Central Wheel Canvas
            Rect wheelRect = new Rect((inRect.width - TexSize) / 2f, 36f, TexSize, TexSize);
            if (wheelTexture != null)
            {
                GUI.DrawTexture(wheelRect, wheelTexture);
            }

            // Mouse handling in GUI coordinates
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

            // Draw Selector Reticles
            DrawHueCursor(wheelRect);
            DrawDiamondCursor(wheelRect);

            // 3. Row 1: Default Presets with Category Tooltips
            float yPresets1 = wheelRect.yMax + 8f;
            float chipSize = 20f;
            float chipGap = 5f;

            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.7f, 0.72f, 0.75f, 1f);
            Widgets.Label(new Rect(0f, yPresets1 + 2f, 62f, 20f), "NHT_YAPEM_PresetsDefault".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            float startX1 = 66f;
            for (int i = 0; i < DefaultPresets.Length; i++)
            {
                var preset = DefaultPresets[i];
                Rect chipRect = new Rect(startX1 + i * (chipSize + chipGap), yPresets1, chipSize, chipSize);

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

            // 4. Row 2: User Custom Presets with RMB FloatMenu & +Save Button
            float yPresets2 = yPresets1 + chipSize + 6f;

            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.7f, 0.72f, 0.75f, 1f);
            Widgets.Label(new Rect(0f, yPresets2 + 2f, 62f, 20f), "NHT_YAPEM_PresetsCustom".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            List<Color> customPresets = YAPEMPatchMod.Settings?.CustomPresets;
            if (customPresets == null)
            {
                customPresets = new List<Color>();
                if (YAPEMPatchMod.Settings != null)
                {
                    YAPEMPatchMod.Settings.CustomPresets = customPresets;
                }
            }

            for (int i = 0; i < CustomSlotCount; i++)
            {
                int slotIndex = i;
                Rect chipRect = new Rect(startX1 + i * (chipSize + chipGap), yPresets2, chipSize, chipSize);

                if (slotIndex < customPresets.Count)
                {
                    Color c = customPresets[slotIndex];
                    Widgets.DrawBoxSolid(chipRect, c);
                    Widgets.DrawBox(chipRect, 1);
                    Widgets.DrawHighlightIfMouseover(chipRect);

                    if (IsColorClose(currentColor, c))
                    {
                        Widgets.DrawBox(chipRect.ExpandedBy(2f), 2);
                    }

                    string tip = $"#{ColorUtility.ToHtmlStringRGB(c)}\n" + "NHT_YAPEM_CustomSlotTip".Translate();
                    TooltipHandler.TipRegion(chipRect, tip);

                    // Check for Left Click (Select) or Right Click / ContextClick (Menu)
                    if (Widgets.ButtonInvisible(chipRect, true))
                    {
                        if (Event.current.button == 1) // RMB
                        {
                            OpenCustomSlotMenu(slotIndex, c);
                        }
                        else // LMB
                        {
                            SelectColor(c);
                        }
                    }
                    else if (Event.current.type == EventType.ContextClick && Mouse.IsOver(chipRect))
                    {
                        OpenCustomSlotMenu(slotIndex, c);
                        Event.current.Use();
                    }
                }
                else
                {
                    // Empty slot: clickable to save currentColor
                    Widgets.DrawBoxSolid(chipRect, new Color(0.12f, 0.14f, 0.17f, 0.8f));
                    Widgets.DrawBox(chipRect, 1);
                    Widgets.DrawHighlightIfMouseover(chipRect);

                    Text.Font = GameFont.Tiny;
                    GUI.color = new Color(0.55f, 0.58f, 0.62f, 1f);
                    Widgets.Label(new Rect(chipRect.x + 5f, chipRect.y + 1f, chipRect.width, chipRect.height), "+");
                    GUI.color = Color.white;
                    Text.Font = GameFont.Small;

                    TooltipHandler.TipRegion(chipRect, "NHT_YAPEM_CustomSlotEmptyTip".Translate());

                    if (Widgets.ButtonInvisible(chipRect, true))
                    {
                        customPresets.Add(currentColor);
                        YAPEMPatchMod.Settings?.Write();
                    }
                    else if (Event.current.type == EventType.ContextClick && Mouse.IsOver(chipRect))
                    {
                        customPresets.Add(currentColor);
                        YAPEMPatchMod.Settings?.Write();
                        Event.current.Use();
                    }
                }
            }

            // Quick "+Save" button next to custom slots
            float saveBtnX = startX1 + CustomSlotCount * (chipSize + chipGap) + 4f;
            Rect saveBtnRect = new Rect(saveBtnX, yPresets2, 60f, chipSize);
            TooltipHandler.TipRegion(saveBtnRect, "NHT_YAPEM_SaveCustomTip".Translate());
            if (Widgets.ButtonText(saveBtnRect, "NHT_YAPEM_SaveBtn".Translate()))
            {
                SaveCurrentColorToPresets();
            }

            // Divider 2
            float yDiv2 = yPresets2 + chipSize + 8f;
            Widgets.DrawLineHorizontal(0f, yDiv2, inRect.width);

            // 5. Comparison & Hex Inspector Card
            float yCard = yDiv2 + 8f;
            Rect cardRect = new Rect(0f, yCard, inRect.width, 46f);
            Widgets.DrawBoxSolid(cardRect, new Color(0.12f, 0.14f, 0.17f, 0.6f));
            Widgets.DrawBox(cardRect, 1);

            // Split Swatch: Left = Previous, Right = Current
            float swX = cardRect.x + 8f;
            float swY = cardRect.y + 7f;
            float swW = 56f;
            float swH = 32f;
            Rect splitRect = new Rect(swX, swY, swW, swH);
            Rect leftHalf = new Rect(swX, swY, swW / 2f, swH);
            Rect rightHalf = new Rect(swX + swW / 2f, swY, swW / 2f, swH);

            Widgets.DrawBoxSolid(leftHalf, originalColor);
            Widgets.DrawBoxSolid(rightHalf, currentColor);
            Widgets.DrawBox(splitRect, 1);
            Widgets.DrawLineVertical(swX + swW / 2f, swY, swH);
            Widgets.DrawHighlightIfMouseover(splitRect);
            TooltipHandler.TipRegion(leftHalf, $"#{ColorUtility.ToHtmlStringRGB(originalColor)} (" + "NHT_YAPEM_ColorPrevious".Translate() + ")");
            TooltipHandler.TipRegion(rightHalf, $"#{ColorUtility.ToHtmlStringRGB(currentColor)} (" + "NHT_YAPEM_ColorCurrent".Translate() + ")");

            if (Widgets.ButtonInvisible(leftHalf))
            {
                SelectColor(originalColor);
            }

            // Hex Input Field
            float hexX = splitRect.xMax + 14f;
            Rect hexHashRect = new Rect(hexX, cardRect.y + 11f, 14f, 24f);
            Widgets.Label(hexHashRect, "#");

            Rect hexFieldRect = new Rect(hexHashRect.xMax + 2f, cardRect.y + 9f, 84f, 28f);
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

            // RGB Numeric Readout
            int r255 = Mathf.RoundToInt(currentColor.r * 255f);
            int g255 = Mathf.RoundToInt(currentColor.g * 255f);
            int b255 = Mathf.RoundToInt(currentColor.b * 255f);

            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.72f, 0.72f, 0.72f, 1f);
            Rect rgbRect1 = new Rect(hexFieldRect.xMax + 12f, cardRect.y + 7f, 96f, 16f);
            Rect rgbRect2 = new Rect(hexFieldRect.xMax + 12f, cardRect.y + 23f, 96f, 16f);
            Widgets.Label(rgbRect1, $"R: {r255}");
            Widgets.Label(rgbRect2, $"G: {g255}  B: {b255}");
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            // 6. Action Buttons Bar
            float yBtns = inRect.height - 32f;
            float btnW = 104f;
            float btnH = 30f;

            // Reset Button
            Rect resetRect = new Rect(0f, yBtns, btnW, btnH);
            if (Widgets.ButtonText(resetRect, "NHT_YAPEM_ResetBtn".Translate()))
            {
                SelectColor(defaultColor);
            }

            // Cancel Button
            Rect cancelRect = new Rect((inRect.width - btnW) / 2f, yBtns, btnW, btnH);
            if (Widgets.ButtonText(cancelRect, "Cancel".Translate()))
            {
                onColorSaved?.Invoke(originalColor);
                Close();
            }

            // Accept / OK Button
            Rect okRect = new Rect(inRect.width - btnW, yBtns, btnW, btnH);
            if (Widgets.ButtonText(okRect, "OK".Translate()))
            {
                accepted = true;
                onColorSaved?.Invoke(currentColor);
                Close();
            }
        }

        private void OpenCustomSlotMenu(int index, Color slotColor)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            string slotHex = ColorUtility.ToHtmlStringRGB(slotColor);
            string curHex = ColorUtility.ToHtmlStringRGB(currentColor);

            // 1. Select / Apply this slot color
            options.Add(new FloatMenuOption("NHT_YAPEM_ApplySlot".Translate(slotHex), () =>
            {
                SelectColor(slotColor);
            }));

            // 2. Overwrite this slot with current color
            options.Add(new FloatMenuOption("NHT_YAPEM_OverwriteSlot".Translate(curHex), () =>
            {
                var custom = YAPEMPatchMod.Settings?.CustomPresets;
                if (custom != null && index >= 0 && index < custom.Count)
                {
                    custom[index] = currentColor;
                    YAPEMPatchMod.Settings?.Write();
                }
            }));

            // 3. Delete this slot
            options.Add(new FloatMenuOption("NHT_YAPEM_DeleteSlot".Translate(), () =>
            {
                var custom = YAPEMPatchMod.Settings?.CustomPresets;
                if (custom != null && index >= 0 && index < custom.Count)
                {
                    custom.RemoveAt(index);
                    YAPEMPatchMod.Settings?.Write();
                }
            }));

            // 4. Clear all custom presets
            options.Add(new FloatMenuOption("NHT_YAPEM_ClearAllSlots".Translate(), () =>
            {
                var custom = YAPEMPatchMod.Settings?.CustomPresets;
                if (custom != null)
                {
                    custom.Clear();
                    YAPEMPatchMod.Settings?.Write();
                }
            }));

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void SaveCurrentColorToPresets()
        {
            var customPresets = YAPEMPatchMod.Settings?.CustomPresets;
            if (customPresets == null) return;

            if (customPresets.Count < CustomSlotCount)
            {
                customPresets.Add(currentColor);
                YAPEMPatchMod.Settings?.Write();
            }
            else
            {
                // If all slots are filled, open a choice menu to let user pick which slot to overwrite
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                string curHex = ColorUtility.ToHtmlStringRGB(currentColor);
                for (int s = 0; s < customPresets.Count; s++)
                {
                    int slotIdx = s;
                    string hex = ColorUtility.ToHtmlStringRGB(customPresets[s]);
                    options.Add(new FloatMenuOption($"Слот {s + 1} (#{hex}) -> #{curHex}", () =>
                    {
                        customPresets[slotIdx] = currentColor;
                        YAPEMPatchMod.Settings?.Write();
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        private static bool IsColorClose(Color a, Color b)
        {
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
            if (!accepted && Event.current?.keyCode == KeyCode.Escape)
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
