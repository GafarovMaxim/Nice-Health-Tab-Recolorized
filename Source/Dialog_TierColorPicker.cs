using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace NiceHealthTab_YAPEM_Patch
{
    public class Dialog_TierColorPicker : Window
    {
        private readonly string title;
        private readonly Action<Color> onColorSaved;
        private readonly Color defaultColor;
        private readonly Color originalColor;
        private Color currentColor;

        private float currentH;
        private float currentS;
        private float currentL;

        private Texture2D wheelTexture;

        private const int TexSize = 240;
        private const float Center = 120f;
        private const float RadiusOuter = 106f;
        private const float RadiusInner = 82f;
        private const float RadiusDiamond = 72f;

        private string hexBuffer;
        private bool isDraggingHue;
        private bool isDraggingDiamond;
        private bool accepted;

        public override Vector2 InitialSize => new Vector2(290f, 360f);

        public Dialog_TierColorPicker(string title, Color initialColor, Color defaultColor, Action<Color> onColorSaved)
        {
            this.title = title;
            this.originalColor = initialColor;
            this.defaultColor = defaultColor;
            this.currentColor = initialColor;
            this.onColorSaved = onColorSaved;

            this.closeOnClickedOutside = true;
            this.absorbInputAroundWindow = true;
            this.doCloseX = true;

            RgbToHsl(initialColor, out currentH, out currentS, out currentL);
            hexBuffer = ColorUtility.ToHtmlStringRGB(initialColor);

            InitWheelTexture();
        }

        private void InitWheelTexture()
        {
            if (wheelTexture != null)
            {
                UnityEngine.Object.Destroy(wheelTexture);
            }

            wheelTexture = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            // 1. Draw background and static Hue ring
            for (int y = 0; y < TexSize; y++)
            {
                float dy = y - Center;
                for (int x = 0; x < TexSize; x++)
                {
                    float dx = x - Center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist >= RadiusInner && dist <= RadiusOuter)
                    {
                        // Hue angle
                        float angle = Mathf.Atan2(dy, dx);
                        float h = (angle / (2f * Mathf.PI) + 1f) % 1f;
                        Color hueColor = HslToRgb(h, 1f, 0.5f);

                        // Antialiasing edges
                        float alpha = 1f;
                        if (dist < RadiusInner + 1f)
                            alpha = dist - RadiusInner;
                        else if (dist > RadiusOuter - 1f)
                            alpha = RadiusOuter - dist;

                        hueColor.a = Mathf.Clamp01(alpha);
                        wheelTexture.SetPixel(x, y, hueColor);
                    }
                    else
                    {
                        wheelTexture.SetPixel(x, y, Color.clear);
                    }
                }
            }

            // 2. Draw diamond
            RedrawDiamond();
        }

        private void RedrawDiamond()
        {
            if (wheelTexture == null) return;

            int minX = Mathf.FloorToInt(Center - RadiusDiamond);
            int maxX = Mathf.CeilToInt(Center + RadiusDiamond);
            int minY = Mathf.FloorToInt(Center - RadiusDiamond);
            int maxY = Mathf.CeilToInt(Center + RadiusDiamond);

            for (int y = minY; y <= maxY; y++)
            {
                float dy = y - Center;
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = x - Center;
                    float manhattan = Mathf.Abs(dx) + Mathf.Abs(dy);

                    if (manhattan <= RadiusDiamond)
                    {
                        // In screen coords, top is -dy, bottom is +dy
                        // Lightness: Top = 1.0 (White), Bottom = 0.0 (Black)
                        float l = Mathf.Clamp01(0.5f - dy / (2f * RadiusDiamond));
                        float span = RadiusDiamond - Mathf.Abs(dy);
                        float s = span > 0.001f ? Mathf.Clamp01((dx + span) / (2f * span)) : 0f;

                        Color col = HslToRgb(currentH, s, l);

                        // Antialias edge
                        float edgeDist = RadiusDiamond - manhattan;
                        if (edgeDist < 1f)
                        {
                            col.a = Mathf.Clamp01(edgeDist);
                        }
                        else
                        {
                            col.a = 1f;
                        }

                        wheelTexture.SetPixel(x, y, col);
                    }
                    else
                    {
                        // Clear gap between diamond and hue ring
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
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
            // Title
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width - 30f, 28f), title);
            Text.Font = GameFont.Small;

            // Wheel area
            Rect wheelRect = new Rect((inRect.width - TexSize) / 2f, 32f, TexSize, TexSize);
            if (wheelTexture != null)
            {
                GUI.DrawTexture(wheelRect, wheelTexture);
            }

            // Mouse handling
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

            // Draw Selector Rings
            DrawHueCursor(wheelRect);
            DrawDiamondCursor(wheelRect);

            // Bottom bar: [Preview Swatch] [#HEX] [>]
            Rect bottomRect = new Rect(wheelRect.x, wheelRect.yMax + 12f, wheelRect.width, 30f);

            // 1. Swatch
            Rect swatchRect = new Rect(bottomRect.x, bottomRect.y, 32f, 28f);
            Widgets.DrawBoxSolid(swatchRect, currentColor);
            Widgets.DrawBox(swatchRect, 1);

            // 2. Hex input
            Rect hexHashRect = new Rect(swatchRect.xMax + 10f, bottomRect.y + 4f, 14f, 24f);
            Widgets.Label(hexHashRect, "#");

            Rect hexFieldRect = new Rect(hexHashRect.xMax + 2f, bottomRect.y + 2f, 96f, 26f);
            string newHex = Widgets.TextField(hexFieldRect, hexBuffer);
            if (newHex != hexBuffer)
            {
                hexBuffer = newHex.Trim().TrimStart('#').ToUpperInvariant();
                if (ColorUtility.TryParseHtmlString("#" + hexBuffer, out Color parsed))
                {
                    currentColor = parsed;
                    RgbToHsl(currentColor, out currentH, out currentS, out currentL);
                    RedrawDiamond();
                    onColorSaved?.Invoke(currentColor);
                }
            }

            // 3. Accept button '>'
            Rect okBtnRect = new Rect(bottomRect.xMax - 40f, bottomRect.y + 2f, 40f, 26f);
            if (Widgets.ButtonText(okBtnRect, ">"))
            {
                accepted = true;
                onColorSaved?.Invoke(currentColor);
                Close();
            }
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
            Rect outer = new Rect(center.x - 6f, center.y - 6f, 12f, 12f);
            Widgets.DrawBoxSolid(outer, new Color(0f, 0f, 0f, 0.85f));
            Rect inner = outer.ContractedBy(1.5f);
            Widgets.DrawBoxSolid(inner, Color.white);
            Widgets.DrawBoxSolid(inner.ContractedBy(1.5f), new Color(0f, 0f, 0f, 0.85f));
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
            if (s <= 0.0001f) return new Color(l, l, l);
            float q = l < 0.5f ? l * (1f + s) : l + s - l * s;
            float p = 2f * l - q;
            float r = HueToRgb(p, q, h + 1f / 3f);
            float g = HueToRgb(p, q, h);
            float b = HueToRgb(p, q, h - 1f / 3f);
            return new Color(r, g, b, 1f);
        }

        private static float HueToRgb(float p, float q, float t)
        {
            if (t < 0f) t += 1f;
            if (t > 1f) t -= 1f;
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
