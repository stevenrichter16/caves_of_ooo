# Independent Cindercaller source-art review

2026-10-24. Read-only inspection of the actual baseline, imagegen concept, editable cell source, native PNG, comparison render, manifest, README and importer. Only this review file was written. No Unity operation, runtime change or new-art gameplay acceptance was performed.

**Source decision: suitable for the next integration/play gate; no significant source-art defect found.** Exact scope is one static living `MarlbackCindercaller` body, with no guaranteed held weapon, fire supply, flame, casting or motion cue.

## Baseline and design comparison

The actual 1280×800 `cindercaller-baseline-world.png` still shows the red `g`. Its paired observation identifies visible Cindercaller 4983 at (78,7), role `ranged`, HP12. This establishes a real remaining fallback target, not proof of the new sprite in play.

The source comparison gives Cindercaller a narrower, smooth slate crown between the approved broad Scrabbler dome and tall stepped Gleaner ridge. It shares their tawny lower body and separated asymmetric limbs. Occupied tops are row8 for Scrabbler, row5 for Cindercaller and row3 for Gleaner; all finish at row22. Thus family membership remains clear while the new outline is more than a recolor. The restrained rust/charcoal rim reads as a scorched mantle, without implying a permanently burning enemy or an inventory object still held after it is spent.

At native size, cap/body outline and leg gaps carry identification; the eye and charred seam are secondary details. The contact makes the form readable at equal scale. Whether that distinction survives actual ordinary zoom, terrain contrast, selection and neighboring actors remains the in-game gate. This review does not promote an enlarged contact to gameplay acceptance.

The concept’s broad direction was applied selectively: narrow crown, scorched rim, restrained family palette. Its inconsistent displayed grid, soft shading, oversized scene placement and duplicate-looking legs were correctly rejected. Its labelled “in-game placement” is a generated study and must not be presented as a Unity capture. The shipped native source is flat hard-edged integer geometry, without gradients or permanent equipment/effects.

## Independent integrity checks

- Native **16×24**, transparent background, occupied bounds **(2,5)–(15,23)** with exclusive right/bottom. Exactly **119 opaque pixels**, **7 opaque colors**, alpha only **0/255** and empty bottom row23.
- All **384 source cells** compared to the native PNG: **0 mismatches**. Direct 8× Cindercaller comparison blocks: **0 pixel mismatches**.
- Scrabbler and Gleaner contact blocks compared against their approved cast frames at the same 8× scale: **0 mismatches** for either. Both alpha silhouettes differ from Cindercaller.
- All **65** saved approved-art/source hashes remain unchanged.
- Source PNG is byte-identical to `Assets/Resources/Sprites/Live16x24/cindercaller.png`; SHA256 **`ab7fb7010cd20d3b9edab00cd48747aade51c124f8916eff6df0a5534ef72394`** matches the manifest.
- Palette stays within the named family colors: ink `#342E25`, stone dark `#3C5148`, stone `#526657`, stone light `#7D8E71`, tawny `#A97542`, claw `#BCA576`, rust `#9B442B`.
- Resource metadata has a valid 32-hex GUID and Point/Clamp/16PPU/centered pivot, no mipmaps, NPOT scaling or lossy compression. Runtime tile binding still needs its independent tests.

The saved `.blend`, cell JSON and generator provide editable reproduction sources. This reviewer compared their declared raster-cell output without rerunning Blender or the verifier that rewrites its receipt. No claim is made here about current-owner/fog/custody rejection, actual examine portraits, inherited Scrabbler-motion suppression, combat readability, save/load, performance, packaged activation, physical Deck or whole-region completeness. Root’s new-art Play check remains required.
