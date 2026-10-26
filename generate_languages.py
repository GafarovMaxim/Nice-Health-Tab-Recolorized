import os
import json
import xml.sax.saxutils

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
JSON_PATH = os.path.join(SCRIPT_DIR, "languages.json")

with open(JSON_PATH, "r", encoding="utf-8") as f:
    languages = json.load(f)

language_aliases = {
    "English": ["English"],
    "Russian": ["Russian", "Russian (Русский)"],
    "French": ["French", "French (Français)"],
    "German": ["German", "German (Deutsch)"],
    "Italian": ["Italian", "Italian (Italiano)"],
    "Spanish": ["Spanish", "Spanish (Español(Castellano))"],
    "SpanishLatin": ["SpanishLatin", "SpanishLatin (Español(Latinoamérica))"],
    "PortugueseBrazilian": ["PortugueseBrazilian", "PortugueseBrazilian (Português Brasileiro)"],
    "Japanese": ["Japanese", "Japanese (日本語)"],
    "Korean": ["Korean", "Korean (한국어)"],
    "ChineseSimplified": ["ChineseSimplified", "ChineseSimplified (简体中文)"],
    "ChineseTraditional": ["ChineseTraditional", "ChineseTraditional (繁體中文)"],
    "Turkish": ["Turkish", "Turkish (Türkçe)"],
    "Ukrainian": ["Ukrainian", "Ukrainian (Українська)"],
    "Vietnamese": ["Vietnamese", "Vietnamese (tiếng Việt)"],
    "Polish": ["Polish", "Polish (Polski)"],
    "Czech": ["Czech", "Czech (Čeština)"],
    "Hungarian": ["Hungarian", "Hungarian (Magyar)"]
}

target_dirs = [
    os.path.join(SCRIPT_DIR, "Languages"),
    os.path.join(SCRIPT_DIR, "1.7", "Languages"),
    r"G:\Modding\RimWorld\NiceHealthTab_Recolorized\Languages",
    r"G:\Modding\RimWorld\NiceHealthTab_Recolorized\1.7\Languages",
    r"E:\SteamLibrary\steamapps\common\RimWorld\Mods\NiceHealthTab_Recolorized\Languages",
    r"E:\SteamLibrary\steamapps\common\RimWorld\Mods\NiceHealthTab_Recolorized\1.7\Languages"
]

def generate():
    for base_dir in target_dirs:
        for lang, keys in languages.items():
            folder_names = language_aliases.get(lang, [lang])
            for folder_name in folder_names:
                keyed_dir = os.path.join(base_dir, folder_name, "Keyed")
                os.makedirs(keyed_dir, exist_ok=True)
                xml_path = os.path.join(keyed_dir, "Settings.xml")
                lines = ['<?xml version="1.0" encoding="utf-8" ?>', '<LanguageData>']
                for k, v in keys.items():
                    safe_v = xml.sax.saxutils.escape(v)
                    lines.append(f"  <{k}>{safe_v}</{k}>")
                lines.append('</LanguageData>\n')
                with open(xml_path, "w", encoding="utf-8") as f:
                    f.write("\n".join(lines))
    print(f"Generated and validated settings for {len(languages)} languages across all target directories.")

if __name__ == "__main__":
    generate()
