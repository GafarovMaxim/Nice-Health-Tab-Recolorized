# Settings redesign: status and plan

## Decisions (agreed with Maxim)
- Color priority: single implant (CustomHediffTiers) > mod color (ModColors) > tier color.
- Tiers added by other mods are detected automatically: a ThingCategoryDef directly under `BodyParts` whose name is not a known tier
  (YAPEM declares Ultratech and Advanced Bionics this way). Tier key = `Cat:<defName>`.
- Settings window gets tabs: General / Tier colors / DLC / Mods / Fine-tune. Mockup: `docs/settings_mockup.html` (clickable).
- Preview: NOT a hand-drawn mannequin (an earlier one rendered wrongly and was removed). Must use Nice Health Tab's own doll,
  same as in the health tab. Entry point: `NiceHealthTab.DollRenderer.DrawDoll(DollRenderContext ctx, Doll doll, Rect rect, bool selectable, bool drawOutline)`.
  Needs a pawn with prosthetics (temporary generated pawn) and a filled DollRenderContext (Pawn, Remap, Hediffs ...). Not investigated yet.

## Done (stage 1, logic, no UI yet)
- `YAPEMPatchSettings`: `ModColors`, `DynamicTierColors` (hex strings, saved), reset in ResetAllToDefault.
- `LimbTierUtility`: auto tier cached per def with mod key; mod color applied over tier color; dynamic tiers
  (`FindDynamicTierKey`, `GetDynamicTiers()`, `GetDynamicTierColor`, `IsDynamicTier`); `GetModKey(def)`; `ClearCaches()`.

## Todo (stage 2, UI)
1. Tabs UI in `YAPEMPatchSettings.DoSettingsWindowContents` (now one long scroll, ~1000 lines).
2. Mods tab: groups from `EnsureImplantGroupsCached` except core/DLC; row = mod name, count, Auto/custom color; key = `LimbTierUtility.GetModKey`.
3. Tier colors tab: add rows for `LimbTierUtility.GetDynamicTiers()` (color in `DynamicTierColors`).
4. Fine-tune dropdown (`OpenTierSelectionMenu`): add dynamic tiers to the list.
5. Preview via NHT DollRenderer (see above).
6. New Keyed strings in `languages.json` for all 18 languages, then run `generate_languages.py`.
7. Profiles (`PaletteProfile`) do not include ModColors / DynamicTierColors yet.
