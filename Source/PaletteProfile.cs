using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Verse;

namespace NiceHealthTab_YAPEM_Patch
{
    public class PaletteProfile : IExposable
    {
        public string Name;
        public bool IsBuiltIn;

        // Base Tiers
        public Color ColorAdvancedBionic = YAPEMPatchSettings.DefaultColorAdvancedBionic;
        public Color ColorUltratech      = YAPEMPatchSettings.DefaultColorUltratech;
        public Color ColorArchotech      = YAPEMPatchSettings.DefaultColorArchotech;
        public Color ColorBionic         = YAPEMPatchSettings.DefaultColorBionic;
        public Color ColorProsthetic     = YAPEMPatchSettings.DefaultColorProsthetic;

        // DLC & Mod Tiers
        public Color ColorRoyalty            = YAPEMPatchSettings.DefaultColorRoyalty;
        public Color ColorBiotech            = YAPEMPatchSettings.DefaultColorBiotech;
        public Color ColorAnomaly            = YAPEMPatchSettings.DefaultColorAnomaly;
        public Color ColorOdyssey            = YAPEMPatchSettings.DefaultColorOdyssey;
        public Color ColorIntegratedImplants = YAPEMPatchSettings.DefaultColorIntegratedImplants;

        // Custom Presets (8 slots)
        public List<Color> CustomPresets = new List<Color>();

        public PaletteProfile()
        {
            EnsureSlotCount();
        }

        public PaletteProfile(string name, bool isBuiltIn = false)
        {
            Name = name;
            IsBuiltIn = isBuiltIn;
            EnsureSlotCount();
        }

        public string Label
        {
            get
            {
                if (string.IsNullOrEmpty(Name)) return "Profile";
                if (Name == "NHT_YAPEM_ProfileDefault" || Name == "Default")
                    return "NHT_YAPEM_ProfileDefault".Translate().RawText;
                if (Name == "NHT_YAPEM_ProfileCyberpunk" || Name == "Cyberpunk")
                    return "NHT_YAPEM_ProfileCyberpunk".Translate().RawText;
                if (Name == "NHT_YAPEM_ProfilePastel" || Name == "Pastel")
                    return "NHT_YAPEM_ProfilePastel".Translate().RawText;
                return Name;
            }
        }

        public void EnsureSlotCount()
        {
            if (CustomPresets == null)
            {
                CustomPresets = new List<Color>();
            }
            while (CustomPresets.Count < Dialog_TierColorPicker.CustomSlotCount)
            {
                CustomPresets.Add(Color.clear);
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref Name, "Name", "Default");
            Scribe_Values.Look(ref IsBuiltIn, "IsBuiltIn", false);

            Scribe_Values.Look(ref ColorAdvancedBionic, "ColorAdvancedBionic", YAPEMPatchSettings.DefaultColorAdvancedBionic);
            Scribe_Values.Look(ref ColorUltratech, "ColorUltratech", YAPEMPatchSettings.DefaultColorUltratech);
            Scribe_Values.Look(ref ColorArchotech, "ColorArchotech", YAPEMPatchSettings.DefaultColorArchotech);
            Scribe_Values.Look(ref ColorBionic, "ColorBionic", YAPEMPatchSettings.DefaultColorBionic);
            Scribe_Values.Look(ref ColorProsthetic, "ColorProsthetic", YAPEMPatchSettings.DefaultColorProsthetic);

            Scribe_Values.Look(ref ColorRoyalty, "ColorRoyalty", YAPEMPatchSettings.DefaultColorRoyalty);
            Scribe_Values.Look(ref ColorBiotech, "ColorBiotech", YAPEMPatchSettings.DefaultColorBiotech);
            Scribe_Values.Look(ref ColorAnomaly, "ColorAnomaly", YAPEMPatchSettings.DefaultColorAnomaly);
            Scribe_Values.Look(ref ColorOdyssey, "ColorOdyssey", YAPEMPatchSettings.DefaultColorOdyssey);
            Scribe_Values.Look(ref ColorIntegratedImplants, "ColorIntegratedImplants", YAPEMPatchSettings.DefaultColorIntegratedImplants);

            Scribe_Collections.Look(ref CustomPresets, "CustomPresets", LookMode.Value);
            EnsureSlotCount();
        }

        private IEnumerable<Color> AllColors()
        {
            yield return ColorAdvancedBionic; yield return ColorUltratech; yield return ColorArchotech;
            yield return ColorBionic; yield return ColorProsthetic;
            yield return ColorRoyalty; yield return ColorBiotech; yield return ColorAnomaly;
            yield return ColorOdyssey; yield return ColorIntegratedImplants;
            foreach (var c in CustomPresets) yield return c;
        }

        // Compared as 8-bit hex: saved floats lose precision, so exact float equality would flag false changes.
        public bool SameAs(PaletteProfile other)
        {
            return other != null && AllColors().Select(ColorUtility.ToHtmlStringRGBA).SequenceEqual(other.AllColors().Select(ColorUtility.ToHtmlStringRGBA));
        }

        public PaletteProfile Clone(string newName)
        {
            var p = new PaletteProfile(newName, false);
            p.ColorAdvancedBionic = this.ColorAdvancedBionic;
            p.ColorUltratech = this.ColorUltratech;
            p.ColorArchotech = this.ColorArchotech;
            p.ColorBionic = this.ColorBionic;
            p.ColorProsthetic = this.ColorProsthetic;

            p.ColorRoyalty = this.ColorRoyalty;
            p.ColorBiotech = this.ColorBiotech;
            p.ColorAnomaly = this.ColorAnomaly;
            p.ColorOdyssey = this.ColorOdyssey;
            p.ColorIntegratedImplants = this.ColorIntegratedImplants;

            p.CustomPresets = new List<Color>();
            if (this.CustomPresets != null)
            {
                for (int i = 0; i < this.CustomPresets.Count; i++)
                {
                    p.CustomPresets.Add(this.CustomPresets[i]);
                }
            }
            p.EnsureSlotCount();
            return p;
        }

        public static PaletteProfile CreateDefault()
        {
            var p = new PaletteProfile("NHT_YAPEM_ProfileDefault", true);
            p.ColorAdvancedBionic = YAPEMPatchSettings.DefaultColorAdvancedBionic;
            p.ColorUltratech      = YAPEMPatchSettings.DefaultColorUltratech;
            p.ColorArchotech      = YAPEMPatchSettings.DefaultColorArchotech;
            p.ColorBionic         = YAPEMPatchSettings.DefaultColorBionic;
            p.ColorProsthetic     = YAPEMPatchSettings.DefaultColorProsthetic;
            p.ColorRoyalty        = YAPEMPatchSettings.DefaultColorRoyalty;
            p.ColorBiotech        = YAPEMPatchSettings.DefaultColorBiotech;
            p.ColorAnomaly        = YAPEMPatchSettings.DefaultColorAnomaly;
            p.ColorOdyssey        = YAPEMPatchSettings.DefaultColorOdyssey;
            p.ColorIntegratedImplants = YAPEMPatchSettings.DefaultColorIntegratedImplants;
            return p;
        }

        public static PaletteProfile CreateCyberpunk()
        {
            var p = new PaletteProfile("NHT_YAPEM_ProfileCyberpunk", true);
            p.ColorAdvancedBionic = new Color(0f, 229f / 255f, 1f);          // #00E5FF Electric Cyan
            p.ColorUltratech      = new Color(57f / 255f, 1f, 20f / 255f);   // #39FF14 Neon Lime
            p.ColorArchotech      = new Color(1f, 0f, 127f / 255f);          // #FF007F Neon Rose
            p.ColorBionic         = new Color(1f, 1f, 1f);                   // #FFFFFF Pure White
            p.ColorProsthetic     = new Color(127f / 255f, 127f / 255f, 127f / 255f); // #7F7F7F Graphite
            p.ColorRoyalty        = new Color(1f, 215f / 255f, 0f);          // #FFD700 Solar Gold
            p.ColorBiotech        = new Color(0f, 1f, 204f / 255f);          // #00FFCC Aqua Neon
            p.ColorAnomaly        = new Color(191f / 255f, 0f, 1f);          // #BF00FF Electric Purple
            p.ColorOdyssey        = new Color(1f, 69f / 255f, 0f);           // #FF4500 Neon Orange
            p.ColorIntegratedImplants = new Color(121f / 255f, 40f / 255f, 202f / 255f); // #7928CA Cyber Violet
            return p;
        }

        public static PaletteProfile CreatePastel()
        {
            var p = new PaletteProfile("NHT_YAPEM_ProfilePastel", true);
            p.ColorAdvancedBionic = new Color(190f / 255f, 225f / 255f, 230f / 255f); // #BEE1E6 Pastel Sky
            p.ColorUltratech      = new Color(202f / 255f, 210f / 255f, 197f / 255f); // #CAD2C5 Mint Cream
            p.ColorArchotech      = new Color(226f / 255f, 236f / 255f, 233f / 255f); // #E2ECE9 Soft Sage
            p.ColorBionic         = new Color(240f / 255f, 239f / 255f, 235f / 255f); // #F0EFEB Ivory White
            p.ColorProsthetic     = new Color(223f / 255f, 227f / 255f, 230f / 255f); // #DFE3E6 Muted Ash
            p.ColorRoyalty        = new Color(253f / 255f, 226f / 255f, 228f / 255f); // #FDE2E4 Pastel Rose
            p.ColorBiotech        = new Color(197f / 255f, 225f / 255f, 165f / 255f); // #C5E1A5 Soft Lime
            p.ColorAnomaly        = new Color(225f / 255f, 190f / 255f, 231f / 255f); // #E1BEE7 Pastel Violet
            p.ColorOdyssey        = new Color(179f / 255f, 229f / 255f, 252f / 255f); // #B3E5FC Sky Mist
            p.ColorIntegratedImplants = new Color(209f / 255f, 196f / 255f, 233f / 255f); // #D1C4E9 Lavender
            return p;
        }

        public string ExportJson()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"name\": \"{EscapeJson(Name)}\",");
            sb.AppendLine("  \"colors\": {");
            sb.AppendLine($"    \"AdvancedBionic\": \"#{ColorUtility.ToHtmlStringRGB(ColorAdvancedBionic)}\",");
            sb.AppendLine($"    \"Ultratech\": \"#{ColorUtility.ToHtmlStringRGB(ColorUltratech)}\",");
            sb.AppendLine($"    \"Archotech\": \"#{ColorUtility.ToHtmlStringRGB(ColorArchotech)}\",");
            sb.AppendLine($"    \"Bionic\": \"#{ColorUtility.ToHtmlStringRGB(ColorBionic)}\",");
            sb.AppendLine($"    \"Prosthetic\": \"#{ColorUtility.ToHtmlStringRGB(ColorProsthetic)}\",");
            sb.AppendLine($"    \"Royalty\": \"#{ColorUtility.ToHtmlStringRGB(ColorRoyalty)}\",");
            sb.AppendLine($"    \"Biotech\": \"#{ColorUtility.ToHtmlStringRGB(ColorBiotech)}\",");
            sb.AppendLine($"    \"Anomaly\": \"#{ColorUtility.ToHtmlStringRGB(ColorAnomaly)}\",");
            sb.AppendLine($"    \"Odyssey\": \"#{ColorUtility.ToHtmlStringRGB(ColorOdyssey)}\",");
            sb.AppendLine($"    \"IntegratedImplants\": \"#{ColorUtility.ToHtmlStringRGB(ColorIntegratedImplants)}\"");
            sb.AppendLine("  },");
            sb.Append("  \"customPresets\": [");
            if (CustomPresets != null)
            {
                bool first = true;
                for (int i = 0; i < CustomPresets.Count; i++)
                {
                    if (CustomPresets[i].a > 0.01f)
                    {
                        if (!first) sb.Append(", ");
                        sb.Append($"\"#{ColorUtility.ToHtmlStringRGB(CustomPresets[i])}\"");
                        first = false;
                    }
                }
            }
            sb.AppendLine("]");
            sb.AppendLine("}");
            return sb.ToString();
        }

        public static PaletteProfile ImportJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                string name = "Imported Profile";
                var matchName = Regex.Match(json, "\"name\"\\s*:\\s*\"([^\"]+)\"");
                if (matchName.Success) name = matchName.Groups[1].Value;

                var prof = new PaletteProfile(name, false);

                prof.ColorAdvancedBionic = ParseColorInJson(json, "AdvancedBionic", YAPEMPatchSettings.DefaultColorAdvancedBionic);
                prof.ColorUltratech      = ParseColorInJson(json, "Ultratech", YAPEMPatchSettings.DefaultColorUltratech);
                prof.ColorArchotech      = ParseColorInJson(json, "Archotech", YAPEMPatchSettings.DefaultColorArchotech);
                prof.ColorBionic         = ParseColorInJson(json, "Bionic", YAPEMPatchSettings.DefaultColorBionic);
                prof.ColorProsthetic     = ParseColorInJson(json, "Prosthetic", YAPEMPatchSettings.DefaultColorProsthetic);

                prof.ColorRoyalty        = ParseColorInJson(json, "Royalty", YAPEMPatchSettings.DefaultColorRoyalty);
                prof.ColorBiotech        = ParseColorInJson(json, "Biotech", YAPEMPatchSettings.DefaultColorBiotech);
                prof.ColorAnomaly        = ParseColorInJson(json, "Anomaly", YAPEMPatchSettings.DefaultColorAnomaly);
                prof.ColorOdyssey        = ParseColorInJson(json, "Odyssey", YAPEMPatchSettings.DefaultColorOdyssey);
                prof.ColorIntegratedImplants = ParseColorInJson(json, "IntegratedImplants", YAPEMPatchSettings.DefaultColorIntegratedImplants);

                prof.CustomPresets = new List<Color>();
                var matchPresets = Regex.Match(json, "\"customPresets\"\\s*:\\s*\\[([^\\]]*)\\]");
                if (matchPresets.Success)
                {
                    var matches = Regex.Matches(matchPresets.Groups[1].Value, "\"#?([A-Fa-f0-9]{6})\"");
                    foreach (Match m in matches)
                    {
                        if (ColorUtility.TryParseHtmlString("#" + m.Groups[1].Value, out Color c))
                        {
                            prof.CustomPresets.Add(c);
                        }
                    }
                }
                prof.EnsureSlotCount();
                return prof;
            }
            catch (Exception ex)
            {
                Log.Warning("[NiceHealthTab: Recolorized] Failed to parse profile JSON: " + ex);
                return null;
            }
        }

        private static Color ParseColorInJson(string json, string key, Color fallback)
        {
            var m = Regex.Match(json, $"\"{key}\"\\s*:\\s*\"#?([A-Fa-f0-9]{{6}})\"");
            if (m.Success && ColorUtility.TryParseHtmlString("#" + m.Groups[1].Value, out Color c))
            {
                return c;
            }
            return fallback;
        }

        private static string EscapeJson(string str)
        {
            if (str == null) return "";
            return str.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
