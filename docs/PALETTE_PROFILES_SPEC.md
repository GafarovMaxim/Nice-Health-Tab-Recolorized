# Palette dialog and profile management: UI designer spec (not implemented yet)

Source: UI-designer agent review of `Dialog_TierColorPicker.cs`, `PaletteProfile.cs`, screenshot of the dialog. Sizes were not checked against real
`Text.CalcSize` in the game; Russian labels may be longer than English.

## Palette dialog, window 440x560, padding 12 (content 416), all sizes multiples of 4
- Header (y0, h32, Medium): swatch 28 + category name + close; line under it: "#RRGGBB" Small grey. Header must say which category is being colored.
- Wheel 200x200 centered (y62..262). Then one row: "Was" / "Now" swatches (label under each, no truncation) + "#" hex field + copy (h32).
- One Small line: R G B and H S V (or only hex + HSV, RGB in tooltip).
- Presets: label "Presets", 11 (now 6) chips 28x28, gap 6. My colors: label "My colors", 8 slots 28x28; empty slot = grey 1px border + plus, number only in tooltip.
- Remove: "HSV Diamond" badge, "N presets" counter, "LMB/RMB" hint (move to tooltip, one Small line under slots).
- Buttons: Reset (left, tooltip "back to default for this category"), Cancel, Apply (accent color when the color changed).
- Chip states in one helper `DrawChip(rect, color, selected)`: hover +10% and white 60% 1px border; selected 2px accent border, 2px offset.
- Remove the profile block from the dialog.

## Profiles
- One row above the tabs in the settings window: `Profile: [ Custom 1 v ] *   [Save]  [...]`.
- Dropdown (FloatMenu): built-in marked, active checked. `[...]` menu: New, Duplicate, Rename, Delete, Export, Import, Revert to saved.
- "Modified" indicator (*) and Save enabled only when current colors differ from a snapshot of the active profile.
- Built-ins (Default, Cyberpunk, Pastel): cannot be deleted/overwritten; Save offers "Save as copy".
- Confirm only: Delete, Import over an existing name (Replace / Save as copy), Revert (loses edits).
- Export/Import: clipboard + Messages.Message feedback ("copied", "no NHT profile in clipboard").
- Profile content: 5 tiers, DLC colors, mod colors + auto-detected tier colors (NEW), 8 custom slots. Old profiles: new fields from defaults.
  When applying a profile with no color for an installed mod, keep the current color.
- JSON: add "version" and "modTiers"; replace the regex parser in `PaletteProfile.ImportJson` (fragile for dictionaries).

## Order of work
1. Dialog layout + DrawChip. 2. Profile row, snapshot/modified indicator, FloatMenu operations. 3. Mod/dynamic-tier colors in profiles (changes save format, needs migration) last.

## Visual rules
Three text sizes (Medium/Small/Tiny>=12px); spacing grid 4 (window 12, blocks 12, chips 6); one accent color shared with the settings tabs
(0.37, 0.66, 1.0); heights 28 or 36 only; one state set (default, hover, selected, disabled, empty).
