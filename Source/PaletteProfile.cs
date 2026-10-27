using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace NiceHealthTab_YAPEM_Patch
{
    public class PaletteProfile : IExposable
    {
        // Export format: 1 = tiers/DLC/presets only (no "version" key), 2 = + modTiers and presetNames.
        public const int JsonVersion = 2;

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

        // Custom Presets (CustomSlotCount slots) and their optional names ("" = unnamed)
        public List<Color> CustomPresets = new List<Color>();
        public List<string> CustomPresetNames = new List<string>();

        // "#RRGGBB" per mod packageId and per auto-detected tier key; absent = Auto. Old profiles have none.
        public Dictionary<string, string> ModColors = new Dictionary<string, string>();
        public Dictionary<string, string> DynamicTierColors = new Dictionary<string, string>();

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
            if (CustomPresets == null) CustomPresets = new List<Color>();
            while (CustomPresets.Count < Dialog_TierColorPicker.CustomSlotCount) CustomPresets.Add(Color.clear);

            if (CustomPresetNames == null) CustomPresetNames = new List<string>();
            while (CustomPresetNames.Count < Dialog_TierColorPicker.CustomSlotCount) CustomPresetNames.Add("");
            for (int i = 0; i < CustomPresetNames.Count; i++)
            {
                if (CustomPresetNames[i] == null) CustomPresetNames[i] = "";
            }

            if (ModColors == null) ModColors = new Dictionary<string, string>();
            if (DynamicTierColors == null) DynamicTierColors = new Dictionary<string, string>();
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
            Scribe_Collections.Look(ref CustomPresetNames, "CustomPresetNames", LookMode.Value);
            Scribe_Collections.Look(ref ModColors, "ModColors", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref DynamicTierColors, "DynamicTierColors", LookMode.Value, LookMode.Value);
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

        // Every key of `profile` must match in `current`; with strict also every key of `current` must exist in `profile`.
        private static bool DictsMatch(Dictionary<string, string> current, Dictionary<string, string> profile, bool strict)
        {
            foreach (var kv in profile)
            {
                if (!current.TryGetValue(kv.Key, out string v) || !string.Equals(v, kv.Value, StringComparison.OrdinalIgnoreCase)) return false;
            }
            return !strict || current.Keys.All(profile.ContainsKey);
        }

        // `this` = current colors, `saved` = the profile. Colors are compared as 8-bit hex: saved floats lose precision.
        // Built-in profiles hold no mod colors, so colors set for mods are not a difference there (applying keeps them).
        public bool SameAs(PaletteProfile saved)
        {
            if (saved == null) return false;
            if (!AllColors().Select(ColorUtility.ToHtmlStringRGBA).SequenceEqual(saved.AllColors().Select(ColorUtility.ToHtmlStringRGBA))) return false;
            if (!CustomPresetNames.SequenceEqual(saved.CustomPresetNames)) return false;
            bool strict = !saved.IsBuiltIn;
            return DictsMatch(ModColors, saved.ModColors, strict) && DictsMatch(DynamicTierColors, saved.DynamicTierColors, strict);
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

            p.CustomPresets = new List<Color>(this.CustomPresets ?? new List<Color>());
            p.CustomPresetNames = new List<string>(this.CustomPresetNames ?? new List<string>());
            p.ModColors = new Dictionary<string, string>(this.ModColors ?? new Dictionary<string, string>());
            p.DynamicTierColors = new Dictionary<string, string>(this.DynamicTierColors ?? new Dictionary<string, string>());
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

        private static string Hex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);

        private static void AppendMap(StringBuilder sb, Dictionary<string, string> map)
        {
            sb.Append("{");
            bool first = true;
            foreach (var kv in map.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                sb.Append(first ? " " : ", ").Append('"').Append(EscapeJson(kv.Key)).Append("\": \"").Append(EscapeJson(kv.Value)).Append('"');
                first = false;
            }
            sb.Append(first ? "}" : " }");
        }

        public string ExportJson()
        {
            EnsureSlotCount();
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"version\": {JsonVersion},");
            sb.AppendLine($"  \"name\": \"{EscapeJson(Name)}\",");
            sb.AppendLine("  \"colors\": {");
            sb.AppendLine($"    \"AdvancedBionic\": \"{Hex(ColorAdvancedBionic)}\",");
            sb.AppendLine($"    \"Ultratech\": \"{Hex(ColorUltratech)}\",");
            sb.AppendLine($"    \"Archotech\": \"{Hex(ColorArchotech)}\",");
            sb.AppendLine($"    \"Bionic\": \"{Hex(ColorBionic)}\",");
            sb.AppendLine($"    \"Prosthetic\": \"{Hex(ColorProsthetic)}\",");
            sb.AppendLine($"    \"Royalty\": \"{Hex(ColorRoyalty)}\",");
            sb.AppendLine($"    \"Biotech\": \"{Hex(ColorBiotech)}\",");
            sb.AppendLine($"    \"Anomaly\": \"{Hex(ColorAnomaly)}\",");
            sb.AppendLine($"    \"Odyssey\": \"{Hex(ColorOdyssey)}\",");
            sb.AppendLine($"    \"IntegratedImplants\": \"{Hex(ColorIntegratedImplants)}\"");
            sb.AppendLine("  },");
            sb.Append("  \"modTiers\": { \"mods\": ");
            AppendMap(sb, ModColors);
            sb.Append(", \"tiers\": ");
            AppendMap(sb, DynamicTierColors);
            sb.AppendLine(" },");
            // positional: an empty string is an empty slot
            sb.AppendLine("  \"customPresets\": [" + string.Join(", ", CustomPresets.Select(c => c.a > 0.01f ? $"\"{Hex(c)}\"" : "\"\"").ToArray()) + "],");
            sb.AppendLine("  \"presetNames\": [" + string.Join(", ", CustomPresetNames.Select(n => $"\"{EscapeJson(n)}\"").ToArray()) + "]");
            sb.AppendLine("}");
            return sb.ToString();
        }

        // Reads versions 1 and 2. Returns null when the text is not an NHT profile.
        public static PaletteProfile ImportJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                var root = MiniJson.Parse(json) as Dictionary<string, object>;
                var colors = root != null && root.TryGetValue("colors", out object c) ? c as Dictionary<string, object> : null;
                if (colors == null) return null;

                string name = root.TryGetValue("name", out object n) && n is string ns && !string.IsNullOrWhiteSpace(ns) ? ns.Trim() : "Imported Profile";
                var prof = new PaletteProfile(name, false);

                prof.ColorAdvancedBionic = ReadColor(colors, "AdvancedBionic", YAPEMPatchSettings.DefaultColorAdvancedBionic);
                prof.ColorUltratech      = ReadColor(colors, "Ultratech", YAPEMPatchSettings.DefaultColorUltratech);
                prof.ColorArchotech      = ReadColor(colors, "Archotech", YAPEMPatchSettings.DefaultColorArchotech);
                prof.ColorBionic         = ReadColor(colors, "Bionic", YAPEMPatchSettings.DefaultColorBionic);
                prof.ColorProsthetic     = ReadColor(colors, "Prosthetic", YAPEMPatchSettings.DefaultColorProsthetic);

                prof.ColorRoyalty        = ReadColor(colors, "Royalty", YAPEMPatchSettings.DefaultColorRoyalty);
                prof.ColorBiotech        = ReadColor(colors, "Biotech", YAPEMPatchSettings.DefaultColorBiotech);
                prof.ColorAnomaly        = ReadColor(colors, "Anomaly", YAPEMPatchSettings.DefaultColorAnomaly);
                prof.ColorOdyssey        = ReadColor(colors, "Odyssey", YAPEMPatchSettings.DefaultColorOdyssey);
                prof.ColorIntegratedImplants = ReadColor(colors, "IntegratedImplants", YAPEMPatchSettings.DefaultColorIntegratedImplants);

                prof.CustomPresets = new List<Color>();
                if (root.TryGetValue("customPresets", out object presets) && presets is List<object> presetList)
                {
                    foreach (object item in presetList)
                    {
                        // version 1 listed only filled slots, version 2 keeps positions with "" for empty ones
                        string s = item as string;
                        if (s != null && ColorUtility.TryParseHtmlString(s.StartsWith("#") ? s : "#" + s, out Color col)) prof.CustomPresets.Add(col);
                        else if (root.ContainsKey("version")) prof.CustomPresets.Add(Color.clear);
                    }
                }

                prof.CustomPresetNames = new List<string>();
                if (root.TryGetValue("presetNames", out object names) && names is List<object> nameList)
                {
                    foreach (object item in nameList) prof.CustomPresetNames.Add(item as string ?? "");
                }

                if (root.TryGetValue("modTiers", out object modTiers) && modTiers is Dictionary<string, object> mt)
                {
                    ReadMap(mt, "mods", prof.ModColors);
                    ReadMap(mt, "tiers", prof.DynamicTierColors);
                }

                prof.EnsureSlotCount();
                return prof;
            }
            catch (Exception ex)
            {
                Log.Warning("[NiceHealthTab: Recolorized] Failed to parse profile JSON: " + ex.Message);
                return null;
            }
        }

        private static Color ReadColor(Dictionary<string, object> map, string key, Color fallback)
        {
            if (map.TryGetValue(key, out object v) && v is string s && ColorUtility.TryParseHtmlString(s.StartsWith("#") ? s : "#" + s, out Color c))
            {
                return c;
            }
            return fallback;
        }

        private static void ReadMap(Dictionary<string, object> parent, string key, Dictionary<string, string> into)
        {
            if (!parent.TryGetValue(key, out object v) || !(v is Dictionary<string, object> map)) return;
            foreach (var kv in map)
            {
                if (kv.Value is string s && ColorUtility.TryParseHtmlString(s.StartsWith("#") ? s : "#" + s, out Color c))
                {
                    into[kv.Key] = Hex(c);
                }
            }
        }

        private static string EscapeJson(string str)
        {
            if (str == null) return "";
            return str.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }
    }
}
