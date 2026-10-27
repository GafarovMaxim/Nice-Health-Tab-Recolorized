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

## Done (stage 2a, window)
- Tabs: General / Tier colors / DLC / Mods / Fine-tune (`DrawTabBar`, own buttons: TabDrawer signatures differ between RimWorld versions).
- Mods tab (`DrawModsTab`, `DrawModColorEntry`): third-party groups, Auto or custom color, Reset returns to Auto.
- Tier colors tab lists auto-detected tiers (`DrawDetectedTiers`); Fine-tune dropdown includes them.
- 11 new NHT_YAPEM_* keys in all 18 languages (machine-written, not reviewed by native speakers).

## Todo (stage 2b)
1. Preview via NHT DollRenderer (see above). Not started.
2. Profiles (`PaletteProfile`) do not include ModColors / DynamicTierColors yet.
3. Default colors: see audit notes (Bionic #E6E6E6 is nearly white, dE 5.2; AdvBionic vs Odyssey dE 10.8; textures are dark, mean luminance ~45%, so on-screen color = texture x tint, darker than the swatch).

## Done (stage 2b)
- Ultratech/Advanced parts of YAPEM were classified as Archotech: they copy the `ImplantEmpireRoyal` tag, which now only gives Archotech if no
  Ultra/Advanced name or category matched. Non-implant hediffs are no longer tinted at all.
- Fine-tune: mod picker is a FloatMenu; buttons "Reset all overrides" and per-mod reset.
- Reset buttons per tab (General = everything).
- Integrated Implants is a normal mod now (Mods tab); DLC default colors = default Bionic color.
- Detected tiers show their source mod/DLC.

## Next
- `docs/PALETTE_PROFILES_SPEC.md` (palette dialog + profiles), then the NHT-doll preview.
