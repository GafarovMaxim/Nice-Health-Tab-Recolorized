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
        public const int CustomSlotCount = 24;
        private const int ChipsPerRow = 12;
        private const float ChipSize = 28f;
        private const float ChipGap = 6f;
        private static readonly Color Accent = new Color(0.37f, 0.66f, 1f);
        private static readonly Color Grey = new Color(0.68f, 0.72f, 0.78f);

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

        public override Vector2 InitialSize => new Vector2(440f, 584f);

        protected override float Margin => 12f;

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

            float w = inRect.width;
            bool changed = !IsColorClose(currentColor, originalColor);

            // Header: swatch, category name, close; hex line under it
            Rect swatchRect = new Rect(0f, 2f, 28f, 28f);
            Widgets.DrawBoxSolid(swatchRect, currentColor);
            Widgets.DrawBox(swatchRect, 1);

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(36f, 0f, w - 36f - 32f, 32f), title);
            Text.Font = GameFont.Small;

            if (Widgets.ButtonText(new Rect(w - 28f, 2f, 28f, 28f), "✕"))
            {
                Close();
            }

            GUI.color = Grey;
            Widgets.Label(new Rect(36f, 32f, w - 36f, 20f), "#" + ColorUtility.ToHtmlStringRGB(currentColor));
            GUI.color = Color.white;

            // Wheel: ring diameter 200 sits inside the 220 texture, so the ring spans y 62..262
            Rect wheelRect = new Rect((w - TexSize) / 2f, 52f, TexSize, TexSize);
            if (wheelTexture != null)
            {
                GUI.DrawTexture(wheelRect, wheelTexture);
            }

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

            DrawHueCursor(wheelRect);
            DrawDiamondCursor(wheelRect);

            // Was / Now swatches (label under each), hex field, copy
            float rowY = 272f;
            Rect wasRect = new Rect(0f, rowY, 56f, 28f);
            Rect nowRect = new Rect(60f, rowY, 56f, 28f);
            Widgets.DrawBoxSolid(wasRect, originalColor);
            Widgets.DrawBox(wasRect, 1);
            Widgets.DrawBoxSolid(nowRect, currentColor);
            Widgets.DrawBox(nowRect, 1);
            Widgets.DrawHighlightIfMouseover(wasRect);
            TooltipHandler.TipRegion(wasRect, $"#{ColorUtility.ToHtmlStringRGB(originalColor)}");
            TooltipHandler.TipRegion(nowRect, $"#{ColorUtility.ToHtmlStringRGB(currentColor)}");
            if (Widgets.ButtonInvisible(wasRect))
            {
                SelectColor(originalColor);
            }

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperCenter;
            GUI.color = Grey;
            Widgets.Label(new Rect(wasRect.x, rowY + 30f, wasRect.width, 16f), "NHT_YAPEM_ColorBefore".Translate());
            Widgets.Label(new Rect(nowRect.x, rowY + 30f, nowRect.width, 16f), "NHT_YAPEM_ColorAfter".Translate());
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(132f, rowY, 12f, 28f), "#");
            Text.Anchor = TextAnchor.UpperLeft;

            string newHex = Widgets.TextField(new Rect(146f, rowY, 96f, 28f), hexBuffer);
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

            Rect copyRect = new Rect(246f, rowY, 28f, 28f);
            TooltipHandler.TipRegion(copyRect, "NHT_YAPEM_CopyHex".Translate());
            if (Widgets.ButtonText(copyRect, "📋"))
            {
                string curHex = ColorUtility.ToHtmlStringRGB(currentColor);
                GUIUtility.systemCopyBuffer = "#" + curHex;
                Messages.Message(string.Format("NHT_YAPEM_HexCopied".Translate().RawText, curHex), MessageTypeDefOf.TaskCompletion, false);
            }

            // One line: RGB and HSL
            int r255 = Mathf.RoundToInt(currentColor.r * 255f);
            int g255 = Mathf.RoundToInt(currentColor.g * 255f);
            int b255 = Mathf.RoundToInt(currentColor.b * 255f);
            int hDeg = Mathf.RoundToInt(currentH * 360f);
            int sPct = Mathf.RoundToInt(currentS * 100f);
            int lPct = Mathf.RoundToInt(currentL * 100f);
            GUI.color = Grey;
            Widgets.Label(new Rect(0f, 320f, w, 20f), $"R {r255}  G {g255}  B {b255}    H {hDeg}°  S {sPct}%  L {lPct}%");
            GUI.color = Color.white;

            // Default presets (x starts at 4 so the selection ring is not clipped)
            Text.Font = GameFont.Tiny;
            GUI.color = Grey;
            Widgets.Label(new Rect(0f, 348f, w, 16f), "NHT_YAPEM_PresetsDefault".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            for (int i = 0; i < DefaultPresets.Length; i++)
            {
                var preset = DefaultPresets[i];
                Rect chipRect = new Rect(4f + i * (ChipSize + ChipGap), 368f, ChipSize, ChipSize);
                DrawChip(chipRect, preset.Color, IsColorClose(currentColor, preset.Color));
                TooltipHandler.TipRegion(chipRect, preset.Tooltip);
                if (Widgets.ButtonInvisible(chipRect))
                {
                    SelectColor(preset.Color);
                }
            }

            // Custom slots
            Text.Font = GameFont.Tiny;
            GUI.color = Grey;
            Widgets.Label(new Rect(0f, 408f, w, 16f), "NHT_YAPEM_PresetsCustom".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            List<Color> customPresets = YAPEMPatchMod.Settings?.CustomPresets;
            for (int i = 0; i < CustomSlotCount; i++)
            {
                int slotIndex = i;
                int slotNumber = i + 1;
                Rect chipRect = new Rect(4f + (i % ChipsPerRow) * (ChipSize + ChipGap), 428f + (i / ChipsPerRow) * (ChipSize + ChipGap), ChipSize, ChipSize);
                bool isFilled = customPresets != null && slotIndex < customPresets.Count && customPresets[slotIndex].a > 0.01f;

                if (isFilled)
                {
                    Color c = customPresets[slotIndex];
                    DrawChip(chipRect, c, IsColorClose(currentColor, c));
                    TooltipHandler.TipRegion(chipRect, string.Format("NHT_YAPEM_SlotTitle".Translate().RawText, slotNumber) + $" (#{ColorUtility.ToHtmlStringRGB(c)})\n" + "NHT_YAPEM_CustomSlotTip".Translate().RawText);

                    if (Widgets.ButtonInvisible(chipRect, true))
                    {
                        if (Event.current.button == 1)
                        {
                            OpenCustomSlotMenu(slotIndex, slotNumber, c);
                        }
                        else
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
                    DrawChip(chipRect, Color.clear, false, true);
                    TooltipHandler.TipRegion(chipRect, string.Format("NHT_YAPEM_SlotTitle".Translate().RawText, slotNumber) + ": " + "NHT_YAPEM_CustomSlotEmptyTip".Translate().RawText);

                    if (customPresets != null && (Widgets.ButtonInvisible(chipRect, true)
                        || (Event.current.type == EventType.ContextClick && Mouse.IsOver(chipRect))))
                    {
                        customPresets[slotIndex] = currentColor;
                        YAPEMPatchMod.Settings?.Write();
                        if (Event.current.type == EventType.ContextClick) Event.current.Use();
                    }
                }
            }

            // Buttons: Reset | Cancel, Apply (accent when the color changed)
            float yBtns = inRect.height - 36f;
            Rect resetRect = new Rect(0f, yBtns, 108f, 36f);
            TooltipHandler.TipRegion(resetRect, "#" + ColorUtility.ToHtmlStringRGB(defaultColor));
            if (Widgets.ButtonText(resetRect, "NHT_YAPEM_ResetBtn".Translate()))
            {
                SelectColor(defaultColor);
            }

            if (Widgets.ButtonText(new Rect(w - 216f, yBtns, 104f, 36f), "Cancel".Translate()))
            {
                Close();
            }

            if (changed) GUI.color = Accent;
            bool apply = Widgets.ButtonText(new Rect(w - 104f, yBtns, 104f, 36f), "NHT_YAPEM_ApplyBtn".Translate());
            GUI.color = Color.white;
            if (apply)
            {
                accepted = true;
                onColorSaved?.Invoke(currentColor);
                YAPEMPatchMod.Settings?.Write();
                Close();
            }
        }

        // One look for every swatch: hover = +10% brightness and a light border, selected = accent ring offset by 2px
        private static void DrawChip(Rect rect, Color color, bool selected, bool empty = false)
        {
            bool hover = Mouse.IsOver(rect);
            if (empty)
            {
                GUI.color = new Color(0.5f, 0.5f, 0.5f, hover ? 1f : 0.7f);
                Widgets.DrawBox(rect, 1);
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, "+");
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
                return;
            }

            Widgets.DrawBoxSolid(rect, hover ? Color.Lerp(color, Color.white, 0.1f) : color);
            GUI.color = hover ? new Color(1f, 1f, 1f, 0.6f) : new Color(0f, 0f, 0f, 0.6f);
            Widgets.DrawBox(rect, 1);
            if (selected)
            {
                GUI.color = Accent;
                Widgets.DrawBox(rect.ExpandedBy(4f), 2);
            }
            GUI.color = Color.white;
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
