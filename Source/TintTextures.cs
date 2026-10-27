using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Verse;
using NiceHealthTab;

namespace NiceHealthTab_YAPEM_Patch
{
    // This mod's prosthesis textures are pre-coloured (e.g. a teal Ultratech arm). Tinting them multiplies the colors
    // (yellow x teal = green), so they are redrawn from a grey copy: the fill becomes white and the shading is kept.
    public static class TintTextures
    {
        private static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

        public static Texture2D Neutral(DollBodyPartDef partDef, Texture2D original, Rect uv)
        {
            if (original == null || string.IsNullOrEmpty(partDef.imagePath) || uv.width < 0.999f || uv.height < 0.999f) return original;
            ModContentPack mod = YAPEMPatchMod.ContentPack;
            if (mod == null || partDef.modContentPack != mod) return original;

            if (!cache.TryGetValue(partDef.imagePath, out Texture2D grey))
            {
                grey = Build(mod, partDef.imagePath);
                cache[partDef.imagePath] = grey;
            }
            return grey != null && grey.width == original.width && grey.height == original.height ? grey : original;
        }

        private static Texture2D Build(ModContentPack mod, string imagePath)
        {
            try
            {
                string file = Path.Combine(mod.RootDir, "Textures", imagePath + ".png");
                if (!File.Exists(file)) return null;
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!tex.LoadImage(File.ReadAllBytes(file))) return null;

                Color32[] px = tex.GetPixels32();
                float sum = 0f;
                int n = 0;
                for (int i = 0; i < px.Length; i++)
                {
                    float l = Lum(px[i]);
                    if (px[i].a > 200 && l > 0.25f)
                    {
                        sum += l;
                        n++;
                    }
                }
                if (n == 0) return null;
                float scale = n / sum; // mean fill luminance -> 1.0

                for (int i = 0; i < px.Length; i++)
                {
                    byte v = (byte)Mathf.RoundToInt(Mathf.Clamp01(Lum(px[i]) * scale) * 255f);
                    px[i] = new Color32(v, v, v, px[i].a);
                }
                tex.SetPixels32(px);
                tex.Apply(false, true);
                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Clamp;
                return tex;
            }
            catch (System.Exception ex)
            {
                Log.Warning("[NiceHealthTab: Recolorized] Could not neutralize " + imagePath + ": " + ex.Message);
                return null;
            }
        }

        private static float Lum(Color32 c) => (0.299f * c.r + 0.587f * c.g + 0.114f * c.b) / 255f;
    }
}
