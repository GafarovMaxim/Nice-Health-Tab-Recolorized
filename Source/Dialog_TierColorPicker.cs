using System;
using UnityEngine;
using Verse;

namespace NiceHealthTab_YAPEM_Patch
{
    public class Dialog_TierColorPicker : Window
    {
        private const int TexSize = 220;
        private const float Center = TexSize / 2f;
        private const float RadiusOuter = 100f;
        private const float RadiusInner = 74f;
        private const float RadiusDiamond = 64f;

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

        public override Vector2 InitialSize => new Vector2(320f, 340f);

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
                // In Unity Texture2D, y=0 is bottom, y=TexSize-1 is top.
                // GUI coordinates have y=0 at top.
                // To align texture with GUI, dy_gui = Center - y.
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

                        // Antialiased edges
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

                    if (manhattan <= RadiusDiamond + 0.5f)
                    {
                        // In GUI coords: Top (dy_gui = -RadiusDiamond) -> White (l = 1.0)
                        // Bottom (dy_gui = +RadiusDiamond) -> Black (l = 0.0)
                        float l = Mathf.Clamp01(0.5f - dy_gui / (2f * RadiusDiamond));
                        float span = RadiusDiamond - Mathf.Abs(dy_gui);
                        float s = span > 0.001f ? Mathf.Clamp01((dx_gui + span) / (2f * span)) : 0f;

                        Color col = HslToRgb(currentH, s, l);

                        // Antialias boundary
                        float edgeDist = RadiusDiamond - manhattan;
                        if (edgeDist < 0f)
                            col.a = Mathf.Clamp01(1f + edgeDist);
                        else
                            col.a = 1f;

                        wheelTexture.SetPixel(x, y, col);
                    }
                    else
                    {
                        // Clear gap between diamond and hue ring
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
            // Title & Close Button
            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(0f, 0f, inRect.width - 28f, 28f);
            Widgets.Label(titleRect, title);
            Text.Font = GameFont.Small;

            Rect closeBtnRect = new Rect(inRect.width - 24f, 2f, 22f, 22f);
            if (Widgets.ButtonText(closeBtnRect, "✕"))
            {
                accepted = true;
                Close();
            }

            // Wheel Canvas
            Rect wheelRect = new Rect((inRect.width - TexSize) / 2f, 34f, TexSize, TexSize);
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

            // Bottom bar: [Preview Swatch] [#HEX] [Reset] [OK >]
            Rect bottomRect = new Rect(wheelRect.x, wheelRect.yMax + 12f, wheelRect.width, 30f);

            // 1. Swatch
            Rect swatchRect = new Rect(bottomRect.x, bottomRect.y, 32f, 28f);
            Widgets.DrawBoxSolid(swatchRect, currentColor);
            Widgets.DrawBox(swatchRect, 1);

            // 2. Hex input
            Rect hexHashRect = new Rect(swatchRect.xMax + 6f, bottomRect.y + 4f, 12f, 24f);
            Widgets.Label(hexHashRect, "#");

            Rect hexFieldRect = new Rect(hexHashRect.xMax + 2f, bottomRect.y + 2f, 70f, 26f);
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

            // 3. Reset Button
            Rect resetRect = new Rect(hexFieldRect.xMax + 6f, bottomRect.y + 2f, 48f, 26f);
            if (Widgets.ButtonText(resetRect, "NHT_YAPEM_ResetBtn".Translate()))
            {
                currentColor = defaultColor;
                hexBuffer = ColorUtility.ToHtmlStringRGB(currentColor);
                RgbToHsl(currentColor, out currentH, out currentS, out currentL);
                RedrawDiamond();
                onColorSaved?.Invoke(currentColor);
            }

            // 4. Accept button '>'
            Rect okBtnRect = new Rect(bottomRect.xMax - 36f, bottomRect.y + 2f, 36f, 26f);
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
            // Top (dy = -RadiusDiamond) -> White (l = 1.0)
            // Bottom (dy = +RadiusDiamond) -> Black (l = 0.0)
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
            Widgets.DrawBoxSolid(outer, new Color(0f, 0f, 0f, 0.9f));
            Rect inner = outer.ContractedBy(1.5f);
            Widgets.DrawBoxSolid(inner, Color.white);
            Widgets.DrawBoxSolid(inner.ContractedBy(1.5f), currentColor);
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
