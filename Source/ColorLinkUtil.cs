using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace NiceHealthTab_YAPEM_Patch
{
    // Color inheritance: a DLC, detected tier or mod can take its color from a tier, a DLC or another mod.
    // Keys: tier / DLC name as in LimbTierUtility ("Anomaly", "Cat:..."), mods as "m:<lowercase packageId>".
    public static class ColorLinkUtil
    {
        public const string ModPrefix = "m:";
        private const int MaxChain = 8;

        public static readonly string[] BaseTiers = { "Archotech", "Ultratech", "AdvancedBionic", "Bionic", "Prosthetic" };
        public static readonly string[] DlcTiers = { "Royalty", "Biotech", "Anomaly", "Odyssey" };

        public static bool HasLink(string key) =>
            key != null && YAPEMPatchMod.Settings?.ColorLinks != null && YAPEMPatchMod.Settings.ColorLinks.ContainsKey(key);

        // Color of `key` through its inheritance chain. False when `key` is not linked or the chain is broken (cycle, Auto mod).
        public static bool TryResolve(string key, out Color color)
        {
            color = Color.white;
            Dictionary<string, string> links = YAPEMPatchMod.Settings?.ColorLinks;
            if (links == null || links.Count == 0 || key == null || !links.TryGetValue(key, out string next)) return false;
            for (int i = 0; i < MaxChain; i++)
            {
                if (links.TryGetValue(next, out string further))
                {
                    next = further;
                    continue;
                }
                return TryRoot(next, out color);
            }
            return false;
        }

        // Own color of an unlinked key.
        public static bool TryRoot(string key, out Color color)
        {
            if (key.StartsWith(ModPrefix))
            {
                color = Color.white;
                return YAPEMPatchMod.Settings.ModColors.TryGetValue(key.Substring(ModPrefix.Length), out string hex)
                    && YAPEMPatchSettings.TryParseHex(hex, out color);
            }
            return LimbTierUtility.TryGetOwnTierColor(key, out color);
        }

        // Would linking `self` to `target` create a loop?
        private static bool Loops(string self, string target)
        {
            Dictionary<string, string> links = YAPEMPatchMod.Settings.ColorLinks;
            string cur = target;
            for (int i = 0; i < MaxChain && cur != null; i++)
            {
                if (cur == self) return true;
                links.TryGetValue(cur, out cur);
            }
            return false;
        }

        public static string Label(string key)
        {
            if (key.StartsWith(ModPrefix))
            {
                foreach (KeyValuePair<string, string> m in YAPEMPatchMod.Settings.GetModLinkTargets())
                {
                    if (m.Key == key) return m.Value;
                }
                return key.Substring(ModPrefix.Length);
            }
            return LimbTierUtility.GetTierLabel(key);
        }

        public static void OpenMenu(string self, Action<string> setLink)
        {
            var root = new List<FloatMenuOption>();
            if (HasLink(self))
            {
                root.Add(new FloatMenuOption("NHT_YAPEM_InheritNone".Translate(), () => setLink(null)));
            }
            root.Add(new FloatMenuOption("NHT_YAPEM_InheritTier".Translate() + " ▸", () => OpenSources(self, setLink, Tiers())));
            root.Add(new FloatMenuOption("NHT_YAPEM_InheritDlc".Translate() + " ▸", () => OpenSources(self, setLink, Dlcs())));
            root.Add(new FloatMenuOption("NHT_YAPEM_InheritMod".Translate() + " ▸", () => OpenSources(self, setLink, Mods())));
            Find.WindowStack.Add(new FloatMenu(root));
        }

        private static List<KeyValuePair<string, string>> Tiers()
        {
            var list = new List<KeyValuePair<string, string>>();
            foreach (string t in BaseTiers) list.Add(new KeyValuePair<string, string>(t, LimbTierUtility.GetTierLabel(t)));
            list.AddRange(LimbTierUtility.GetDynamicTiers());
            return list;
        }

        private static List<KeyValuePair<string, string>> Dlcs()
        {
            var list = new List<KeyValuePair<string, string>>();
            foreach (string t in DlcTiers) list.Add(new KeyValuePair<string, string>(t, LimbTierUtility.GetTierLabel(t)));
            return list;
        }

        private static List<KeyValuePair<string, string>> Mods() => YAPEMPatchMod.Settings.GetModLinkTargets();

        private static void OpenSources(string self, Action<string> setLink, List<KeyValuePair<string, string>> sources)
        {
            var options = new List<FloatMenuOption>();
            foreach (KeyValuePair<string, string> s in sources)
            {
                string target = s.Key;
                if (target == self || Loops(self, target)) continue;
                // Auto mods have no color of their own, so there is nothing to inherit from them.
                if (!TryRoot(target, out _) && !TryResolve(target, out _)) continue;
                Color shown = TryResolve(target, out Color linked) ? linked : (TryRoot(target, out Color own) ? own : Color.white);
                options.Add(new FloatMenuOption(s.Value, () => setLink(target), BaseContent.WhiteTex, shown));
            }
            if (options.Count == 0) options.Add(new FloatMenuOption("NHT_YAPEM_InheritNothing".Translate(), null));
            Find.WindowStack.Add(new FloatMenu(options));
        }
    }
}
